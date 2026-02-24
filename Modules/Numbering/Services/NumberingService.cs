using MyApi.Data;
using MyApi.Modules.Numbering.DTOs;
using MyApi.Modules.Numbering.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace MyApi.Modules.Numbering.Services
{
    public class NumberingService : INumberingService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<NumberingService> _logger;

        // Valid entities
        private static readonly HashSet<string> ValidEntities = new(StringComparer.OrdinalIgnoreCase)
        {
            "Offer", "Sale", "ServiceOrder", "Dispatch"
        };

        // Entity prefixes for legacy fallback
        private static readonly Dictionary<string, string> EntityPrefixes = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Offer", "OFR" },
            { "Sale", "SALE" },
            { "ServiceOrder", "SO" },
            { "Dispatch", "DISP" }
        };

        // Token regex: matches {TOKEN} or {TOKEN:param}
        private static readonly Regex TokenRegex = new(@"\{([A-Z_]+)(?::([^}]+))?\}", RegexOptions.Compiled);

        // Supported tokens
        private static readonly HashSet<string> SupportedTokens = new()
        {
            "YYYY", "YEAR", "YY", "DATE", "SEQ", "GUID", "TS", "ENTITY", "ID"
        };

        public NumberingService(ApplicationDbContext context, ILogger<NumberingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ──────────────────────────────────────────────────────────
        // GetNextAsync — atomic number generation
        // ──────────────────────────────────────────────────────────
        public async Task<string> GetNextAsync(string entity)
        {
            if (!ValidEntities.Contains(entity))
                throw new ArgumentException($"Unknown entity: {entity}");

            var settings = await _context.Set<NumberingSettings>()
                .FirstOrDefaultAsync(s => s.EntityName == entity);

            // Fallback to legacy if not configured or disabled
            if (settings == null || !settings.IsEnabled)
                return GenerateLegacy(entity);

            var now = DateTime.UtcNow;
            string result;
            int retries = 0;
            const int maxRetries = 3;

            while (true)
            {
                try
                {
                    result = await RenderTemplateWithSequence(settings, now, consume: true);
                    break;
                }
                catch (DbUpdateConcurrencyException) when (retries < maxRetries)
                {
                    retries++;
                    _logger.LogWarning("Numbering concurrency conflict for {Entity}, retry {Retry}/{Max}", entity, retries, maxRetries);
                    await Task.Delay(retries * 50); // brief backoff
                }
            }

            return result;
        }

        // ──────────────────────────────────────────────────────────
        // PreviewAsync — preview using stored settings
        // ──────────────────────────────────────────────────────────
        public async Task<List<string>> PreviewAsync(string entity, int count = 5)
        {
            var settings = await _context.Set<NumberingSettings>()
                .FirstOrDefaultAsync(s => s.EntityName == entity);

            if (settings == null || !settings.IsEnabled)
            {
                return Enumerable.Range(1, count).Select(i => GenerateLegacy(entity)).ToList();
            }

            return PreviewFromSettings(settings, count);
        }

        // ──────────────────────────────────────────────────────────
        // PreviewFromTemplate — ad-hoc preview for admin UI
        // ──────────────────────────────────────────────────────────
        public List<string> PreviewFromTemplate(NumberingPreviewRequest request)
        {
            var fakeSettings = new NumberingSettings
            {
                EntityName = request.Entity,
                Template = request.Template,
                Strategy = request.Strategy,
                ResetFrequency = request.ResetFrequency,
                StartValue = request.StartValue,
                Padding = request.Padding,
                IsEnabled = true,
            };

            return PreviewFromSettings(fakeSettings, Math.Min(request.Count, 10));
        }

        // ──────────────────────────────────────────────────────────
        // ValidateTemplate
        // ──────────────────────────────────────────────────────────
        public (bool IsValid, List<string> Errors, List<string> Warnings) ValidateTemplate(string template, string strategy)
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            if (string.IsNullOrWhiteSpace(template))
            {
                errors.Add("Template cannot be empty.");
                return (false, errors, warnings);
            }

            if (template.Length > 200)
                errors.Add("Template must be 200 characters or less.");

            var matches = TokenRegex.Matches(template);
            bool hasSeq = false;
            bool hasUnique = false;

            foreach (Match m in matches)
            {
                var token = m.Groups[1].Value;
                if (!SupportedTokens.Contains(token))
                {
                    errors.Add($"Unknown token: {{{token}}}");
                }
                if (token == "SEQ") hasSeq = true;
                if (token is "SEQ" or "GUID" or "TS" or "ID") hasUnique = true;
            }

            if (!hasUnique)
                warnings.Add("Template has no unique token ({SEQ}, {GUID}, {TS}, or {ID}). This may cause collisions.");

            if (hasSeq && strategy is "timestamp_random" or "guid")
                warnings.Add("{SEQ} token is used but strategy does not generate sequences. The {SEQ} value will default to 0.");

            if (!hasSeq && strategy is "db_sequence" or "atomic_counter")
                warnings.Add("Strategy uses sequences but template has no {SEQ} token.");

            return (errors.Count == 0, errors, warnings);
        }

        // ──────────────────────────────────────────────────────────
        // Settings CRUD
        // ──────────────────────────────────────────────────────────
        public async Task<List<NumberingSettingsDto>> GetAllSettingsAsync()
        {
            var all = await _context.Set<NumberingSettings>().ToListAsync();

            // Auto-seed if table is empty (first run before migration seed or EF seed runs)
            if (all.Count == 0)
            {
                await EnsureDefaultSettingsAsync();
                all = await _context.Set<NumberingSettings>().ToListAsync();
            }

            // Ensure all 4 entities are represented
            var result = new List<NumberingSettingsDto>();
            foreach (var entity in ValidEntities)
            {
                var existing = all.FirstOrDefault(s => s.EntityName == entity);
                result.Add(existing != null ? MapToDto(existing) : GetDefaultDto(entity));
            }
            return result;
        }

        /// <summary>
        /// Ensures default NumberingSettings rows exist for all entities.
        /// Called on first access if the table is empty — safe for concurrent calls via ON CONFLICT.
        /// </summary>
        private async Task EnsureDefaultSettingsAsync()
        {
            foreach (var entity in ValidEntities)
            {
                var exists = await _context.Set<NumberingSettings>()
                    .AnyAsync(s => s.EntityName == entity);

                if (!exists)
                {
                    var defaults = GetDefaultDto(entity);
                    _context.Set<NumberingSettings>().Add(new NumberingSettings
                    {
                        EntityName = entity,
                        IsEnabled = false,
                        Template = defaults.Template,
                        Strategy = defaults.Strategy,
                        ResetFrequency = defaults.ResetFrequency,
                        StartValue = defaults.StartValue,
                        Padding = defaults.Padding,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                    });
                }
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Race condition — another instance seeded first. Safe to ignore.
                _logger.LogInformation("NumberingSettings already seeded by another instance");
            }
        }

        public async Task<NumberingSettingsDto?> GetSettingsAsync(string entity)
        {
            var s = await _context.Set<NumberingSettings>()
                .FirstOrDefaultAsync(x => x.EntityName == entity);
            return s != null ? MapToDto(s) : GetDefaultDto(entity);
        }

        public async Task<NumberingSettingsDto> SaveSettingsAsync(string entity, UpdateNumberingSettingsRequest request)
        {
            if (!ValidEntities.Contains(entity))
                throw new ArgumentException($"Unknown entity: {entity}");

            var (isValid, errors, _) = ValidateTemplate(request.Template, request.Strategy);
            if (!isValid)
                throw new InvalidOperationException($"Invalid template: {string.Join("; ", errors)}");

            var settings = await _context.Set<NumberingSettings>()
                .FirstOrDefaultAsync(s => s.EntityName == entity);

            if (settings == null)
            {
                settings = new NumberingSettings
                {
                    EntityName = entity,
                    CreatedAt = DateTime.UtcNow,
                };
                _context.Set<NumberingSettings>().Add(settings);
            }

            settings.IsEnabled = request.IsEnabled;
            settings.Template = request.Template;
            settings.Strategy = request.Strategy;
            settings.ResetFrequency = request.ResetFrequency;
            settings.StartValue = request.StartValue;
            settings.Padding = request.Padding;
            settings.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return MapToDto(settings);
        }

        // ══════════════════════════════════════════════════════════
        //  PRIVATE HELPERS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Render a template, optionally consuming a sequence counter atomically.
        /// </summary>
        private async Task<string> RenderTemplateWithSequence(NumberingSettings settings, DateTime now, bool consume)
        {
            long seqValue = settings.StartValue;

            if (consume && settings.Strategy is "atomic_counter" or "db_sequence" &&
                settings.Template.Contains("{SEQ", StringComparison.OrdinalIgnoreCase))
            {
                seqValue = await GetNextSequenceValueAsync(settings.EntityName, settings.ResetFrequency, settings.StartValue, now);
            }

            return RenderTemplate(settings, now, seqValue);
        }

        /// <summary>
        /// Atomically increment and return the next sequence value.
        /// Uses UPDATE ... SET last_value = last_value + 1 RETURNING last_value via raw SQL for atomicity.
        /// </summary>
        private async Task<long> GetNextSequenceValueAsync(string entity, string resetFrequency, int startValue, DateTime now)
        {
            var periodKey = GetPeriodKey(resetFrequency, now);

            // Try atomic UPDATE + RETURNING first (PostgreSQL)
            var sql = @"
                INSERT INTO ""NumberSequences"" (entity_name, period_key, last_value, created_at, updated_at)
                VALUES ({0}, {1}, {2}, NOW(), NOW())
                ON CONFLICT (entity_name, period_key)
                DO UPDATE SET last_value = ""NumberSequences"".last_value + 1, updated_at = NOW()
                RETURNING last_value;";

            var result = await _context.Database
                .SqlQueryRaw<long>(sql, entity, periodKey, startValue)
                .ToListAsync();

            return result.FirstOrDefault();
        }

        private static string GetPeriodKey(string resetFrequency, DateTime now)
        {
            return resetFrequency switch
            {
                "yearly" => now.Year.ToString(),
                "monthly" => $"{now.Year}-{now.Month:D2}",
                _ => "all"
            };
        }

        /// <summary>
        /// Render template tokens into a final string.
        /// </summary>
        private string RenderTemplate(NumberingSettings settings, DateTime now, long seqValue)
        {
            var result = TokenRegex.Replace(settings.Template, match =>
            {
                var token = match.Groups[1].Value;
                var param = match.Groups[2].Success ? match.Groups[2].Value : null;

                return token switch
                {
                    "YYYY" or "YEAR" => now.Year.ToString(),
                    "YY" => now.ToString("yy"),
                    "DATE" => now.ToString(param ?? "yyyyMMdd"),
                    "SEQ" =>
                        seqValue.ToString().PadLeft(
                            param != null && int.TryParse(param, out var p) ? p : settings.Padding, '0'),
                    "GUID" =>
                        Guid.NewGuid().ToString("N")[..(param != null && int.TryParse(param, out var g) ? Math.Min(g, 32) : 8)].ToUpper(),
                    "TS" => now.ToString(param ?? "yyyyMMddHHmmss"),
                    "ENTITY" => EntityPrefixes.GetValueOrDefault(settings.EntityName, settings.EntityName),
                    "ID" => "0", // placeholder — actual DB id not known at generation time
                    _ => match.Value // leave unknown tokens as-is
                };
            });

            return result;
        }

        /// <summary>
        /// Preview N values using settings without consuming counters.
        /// </summary>
        private List<string> PreviewFromSettings(NumberingSettings settings, int count)
        {
            var now = DateTime.UtcNow;
            var results = new List<string>();

            for (int i = 0; i < count; i++)
            {
                long seqValue = settings.StartValue + i;
                results.Add(RenderTemplate(settings, now, seqValue));
            }

            return results;
        }

        /// <summary>
        /// Legacy number generation (current hardcoded logic).
        /// </summary>
        private string GenerateLegacy(string entity)
        {
            var now = DateTime.UtcNow;
            return entity switch
            {
                "Offer" => $"OFR-{now.Year}-{1:D6}", // caller should still use OfferService's existing logic for real fallback
                "Sale" => $"SALE-{now:yyyyMMdd}-{Guid.NewGuid().ToString()[..5].ToUpper()}",
                "ServiceOrder" => $"SO-{now:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}",
                "Dispatch" => $"DISP-{now:yyyyMMddHHmmss}",
                _ => $"DOC-{now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..4].ToUpper()}"
            };
        }

        private static NumberingSettingsDto MapToDto(NumberingSettings s) => new()
        {
            Id = s.Id,
            EntityName = s.EntityName,
            IsEnabled = s.IsEnabled,
            Template = s.Template,
            Strategy = s.Strategy,
            ResetFrequency = s.ResetFrequency,
            StartValue = s.StartValue,
            Padding = s.Padding,
            UpdatedAt = s.UpdatedAt,
        };

        /// <summary>
        /// Return default (unsaved) DTO matching current hardcoded logic.
        /// </summary>
        private static NumberingSettingsDto GetDefaultDto(string entity) => entity switch
        {
            "Offer" => new() { EntityName = "Offer", Template = "OFR-{YEAR}-{SEQ:6}", Strategy = "atomic_counter", ResetFrequency = "yearly", StartValue = 1, Padding = 6 },
            "Sale" => new() { EntityName = "Sale", Template = "SALE-{DATE:yyyyMMdd}-{GUID:5}", Strategy = "guid", ResetFrequency = "never", StartValue = 1, Padding = 5 },
            "ServiceOrder" => new() { EntityName = "ServiceOrder", Template = "SO-{DATE:yyyyMMdd}-{GUID:6}", Strategy = "guid", ResetFrequency = "never", StartValue = 1, Padding = 6 },
            "Dispatch" => new() { EntityName = "Dispatch", Template = "DISP-{TS:yyyyMMddHHmmss}", Strategy = "timestamp_random", ResetFrequency = "never", StartValue = 1, Padding = 6 },
            _ => new() { EntityName = entity, Template = $"{entity}-{{SEQ:6}}", Strategy = "atomic_counter", ResetFrequency = "yearly", StartValue = 1, Padding = 6 }
        };
    }
}
