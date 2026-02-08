using MyApi.Data;
using MyApi.Modules.Auth.DTOs;
using MyApi.Modules.Auth.Models;
using MyApi.Modules.Users.Models;
using MyApi.Modules.Users.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace MyApi.Modules.Auth.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto> LoginAsync(LoginRequestDto loginDto);
        Task<AuthResponseDto> UserLoginAsync(LoginRequestDto loginDto);
        Task<AuthResponseDto> SignupAsync(SignupRequestDto signupDto);
        Task<AuthResponseDto> RefreshTokenAsync(string refreshToken);
        Task<UserDto?> GetUserByIdAsync(int userId);
        Task<UserDto?> GetUserByEmailAsync(string email);
        Task<IEnumerable<UserDto>> GetAllAdminUsersAsync();
        Task<AuthResponseDto> OAuthLoginAsync(string email);
        Task<AuthResponseDto> UpdateUserAsync(int userId, UpdateUserRequestDto updateDto);
        Task<AuthResponseDto> ChangePasswordAsync(int userId, ChangePasswordRequestDto changePasswordDto);
        Task<bool> LogoutAsync(int userId);
        Task<bool> AdminExistsAsync();
        Task<AdminExistsResultDto> GetAdminExistsWithPreferencesAsync();
    }

    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        public AuthService(ApplicationDbContext context, IConfiguration configuration, ILogger<AuthService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<AuthResponseDto> LoginAsync(LoginRequestDto loginDto)
        {
            try
            {
                // First, try to find user in MainAdminUsers table
                var adminUser = await _context.MainAdminUsers
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == loginDto.Email.ToLower() && u.IsActive);

                if (adminUser != null && VerifyPassword(loginDto.Password, adminUser.PasswordHash))
                {
                    var (accessToken, refreshToken, expiresAt) = GenerateTokensAsync(adminUser);

                    // Update admin user login info
                    adminUser.LastLoginAt = DateTime.UtcNow;
                    adminUser.LastLoginDate = DateTime.UtcNow;
                    adminUser.AccessToken = accessToken;
                    adminUser.RefreshToken = refreshToken;
                    adminUser.TokenExpiresAt = expiresAt;
                    adminUser.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();

                    return new AuthResponseDto
                    {
                        Success = true,
                        Message = "Login successful",
                        AccessToken = accessToken,
                        RefreshToken = refreshToken,
                        ExpiresAt = expiresAt,
                        User = MapToUserDto(adminUser)
                    };
                }

                // If not found in MainAdminUsers, try Users table
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == loginDto.Email.ToLower() && u.IsActive && !u.IsDeleted);

                if (user != null && VerifyPassword(loginDto.Password, user.PasswordHash))
                {
                    var (accessToken, refreshToken, expiresAt) = GenerateUserTokensAsync(user);

                    // Update regular user login info
                    user.LastLoginAt = DateTime.UtcNow;
                    user.AccessToken = accessToken;
                    user.RefreshToken = refreshToken;
                    user.TokenExpiresAt = expiresAt;
                    user.ModifiedDate = DateTime.UtcNow;
                    user.ModifiedBy = user.Email;

                    await _context.SaveChangesAsync();

                    return new AuthResponseDto
                    {
                        Success = true,
                        Message = "Login successful",
                        AccessToken = accessToken,
                        RefreshToken = refreshToken,
                        ExpiresAt = expiresAt,
                        User = MapUserToUserDto(user)
                    };
                }

                // No valid user found in either table
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Invalid email or password"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during unified login for email: {Email}", loginDto.Email);
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "An error occurred during login"
                };
            }
        }

        public async Task<AuthResponseDto> UserLoginAsync(LoginRequestDto loginDto)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == loginDto.Email.ToLower() && u.IsActive && !u.IsDeleted);

                if (user == null || !VerifyPassword(loginDto.Password, user.PasswordHash))
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Invalid email or password"
                    };
                }

                // Check if user has at least one active role assigned
                var hasActiveRole = await _context.UserRoles
                    .AnyAsync(ur => ur.UserId == user.Id && ur.IsActive);

                if (!hasActiveRole)
                {
                    _logger.LogWarning("User {Email} attempted to login without any assigned roles", user.Email);
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Access denied. No role has been assigned to your account. Please contact your administrator."
                    };
                }

                // Get the user's primary role name for the token
                var userRole = await _context.UserRoles
                    .Where(ur => ur.UserId == user.Id && ur.IsActive)
                    .Include(ur => ur.Role)
                    .Select(ur => ur.Role!.Name)
                    .FirstOrDefaultAsync();

                // Update user's Role field with the actual role name
                if (!string.IsNullOrEmpty(userRole))
                {
                    user.Role = userRole;
                }

                var (accessToken, refreshToken, expiresAt) = GenerateUserTokensAsync(user);

                // Update user login info
                user.LastLoginAt = DateTime.UtcNow;
                user.AccessToken = accessToken;
                user.RefreshToken = refreshToken;
                user.TokenExpiresAt = expiresAt;
                user.ModifiedDate = DateTime.UtcNow;
                user.ModifiedBy = user.Email;

                await _context.SaveChangesAsync();

                return new AuthResponseDto
                {
                    Success = true,
                    Message = "Login successful",
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = expiresAt,
                    User = MapUserToUserDto(user)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user login for email: {Email}", loginDto.Email);
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "An error occurred during login"
                };
            }
        }

        public async Task<bool> AdminExistsAsync()
        {
            try
            {
                return await _context.MainAdminUsers.AnyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if admin exists");
                return false;
            }
        }

        public async Task<AdminExistsResultDto> GetAdminExistsWithPreferencesAsync()
        {
            try
            {
                var admin = await _context.MainAdminUsers.FirstOrDefaultAsync();
                
                if (admin == null)
                {
                    return new AdminExistsResultDto
                    {
                        AdminExists = false,
                        SignupAllowed = true,
                        Message = "No administrator account found. Please create one."
                    };
                }

                // Extract theme, language, primaryColor from PreferencesJson
                AdminPreferencesDto? adminPreferences = null;
                if (!string.IsNullOrEmpty(admin.PreferencesJson))
                {
                    try
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(admin.PreferencesJson);
                        var root = doc.RootElement;
                        adminPreferences = new AdminPreferencesDto
                        {
                            Theme = root.TryGetProperty("theme", out var t) ? t.GetString() : null,
                            Language = root.TryGetProperty("language", out var l) ? l.GetString() : null,
                            PrimaryColor = root.TryGetProperty("primaryColor", out var c) ? c.GetString() : null
                        };
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse admin PreferencesJson");
                    }
                }

                return new AdminExistsResultDto
                {
                    AdminExists = true,
                    SignupAllowed = false,
                    Message = "An administrator account exists. Please login.",
                    AdminPreferences = adminPreferences,
                    CompanyLogoUrl = admin.CompanyLogoUrl
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if admin exists with preferences");
                return new AdminExistsResultDto
                {
                    AdminExists = false,
                    SignupAllowed = true,
                    Message = "Error checking admin status"
                };
            }
        }

        public async Task<AuthResponseDto> SignupAsync(SignupRequestDto signupDto)
        {
            try
            {
                _logger.LogInformation("Starting signup process for email: {Email}", signupDto.Email);
                
                // Check if any admin user already exists - block signup if so
                var adminExists = await _context.MainAdminUsers.AnyAsync();
                if (adminExists)
                {
                    _logger.LogWarning("Signup blocked: An admin user already exists. Email attempted: {Email}", signupDto.Email);
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Signup is disabled. An administrator account already exists. Please login instead."
                    };
                }
                
                // Check if email exists in MainAdminUsers table
                _logger.LogInformation("Checking if admin user exists for email: {Email}", signupDto.Email);
                var existingAdminUser = await _context.MainAdminUsers
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == signupDto.Email.ToLower());

                if (existingAdminUser != null)
                {
                    _logger.LogWarning("Admin user already exists with email: {Email}", signupDto.Email);
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "A user with this email already exists"
                    };
                }

                // Note: Users table check removed - admin signup only uses MainAdminUsers table
                // Regular user management is handled separately

                // Create new admin user
                _logger.LogInformation("Hashing password for admin user: {Email}", signupDto.Email);
                string hashedPassword;
                try
                {
                    hashedPassword = signupDto.Password == "nopassword" ? "nopassword" : HashPassword(signupDto.Password);
                }
                catch (Exception hashEx)
                {
                    _logger.LogError(hashEx, "Password hashing failed for email: {Email}", signupDto.Email);
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = $"Password hashing failed: {hashEx.Message}"
                    };
                }
                
                _logger.LogInformation("Creating new admin user object for email: {Email}", signupDto.Email);
                var newUser = new MainAdminUser
                {
                    Email = signupDto.Email.ToLower(),
                    Username = signupDto.Email.ToLower(), // Use email as username
                    PasswordHash = hashedPassword,
                    FirstName = signupDto.FirstName,
                    LastName = signupDto.LastName,
                    PhoneNumber = signupDto.PhoneNumber,
                    Country = signupDto.Country,
                    Industry = signupDto.Industry ?? "",
                    CompanyName = signupDto.CompanyName,
                    CompanyWebsite = signupDto.CompanyWebsite,
                    PreferencesJson = signupDto.Preferences,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    OnboardingCompleted = false
                };

                _logger.LogInformation("Adding admin user to database context for email: {Email}", signupDto.Email);
                _context.MainAdminUsers.Add(newUser);
                
                _logger.LogInformation("Saving changes to database for email: {Email}", signupDto.Email);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (Exception dbEx)
                {
                    _logger.LogError(dbEx, "Database save failed for email: {Email}", signupDto.Email);
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = $"Database save failed: {dbEx.Message}"
                    };
                }

                _logger.LogInformation("Generating tokens for new admin user: {Email}", signupDto.Email);
                (string accessToken, string refreshToken, DateTime expiresAt) tokens;
                try
                {
                    tokens = GenerateTokensAsync(newUser);
                }
                catch (Exception tokenEx)
                {
                    _logger.LogError(tokenEx, "Token generation failed for email: {Email}", signupDto.Email);
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = $"Token generation failed: {tokenEx.Message}"
                    };
                }

                // Update user with tokens
                _logger.LogInformation("Updating admin user with tokens for email: {Email}", signupDto.Email);
                newUser.AccessToken = tokens.accessToken;
                newUser.RefreshToken = tokens.refreshToken;
                newUser.TokenExpiresAt = tokens.expiresAt;
                newUser.LastLoginAt = DateTime.UtcNow;
                newUser.LastLoginDate = DateTime.UtcNow;

                _logger.LogInformation("Saving token updates to database for email: {Email}", signupDto.Email);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Admin user registration completed successfully for email: {Email}", signupDto.Email);
                return new AuthResponseDto
                {
                    Success = true,
                    Message = "Account created successfully",
                    AccessToken = tokens.accessToken,
                    RefreshToken = tokens.refreshToken,
                    ExpiresAt = tokens.expiresAt,
                    User = MapToUserDto(newUser)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during signup for email: {Email}. Exception: {Exception}", signupDto.Email, ex.ToString());
                return new AuthResponseDto
                {
                    Success = false,
                    Message = $"Signup error: {ex.GetType().Name} - {ex.Message}"
                };
            }
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                var user = await _context.MainAdminUsers
                    .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken && u.IsActive);

                if (user == null || user.TokenExpiresAt < DateTime.UtcNow)
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Invalid or expired refresh token"
                    };
                }

                var (newAccessToken, newRefreshToken, expiresAt) = GenerateTokensAsync(user);

                user.AccessToken = newAccessToken;
                user.RefreshToken = newRefreshToken;
                user.TokenExpiresAt = expiresAt;
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return new AuthResponseDto
                {
                    Success = true,
                    Message = "Token refreshed successfully",
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshToken,
                    ExpiresAt = expiresAt,
                    User = MapToUserDto(user)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh");
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "An error occurred during token refresh"
                };
            }
        }

        public async Task<UserDto?> GetUserByIdAsync(int userId)
        {
            try
            {
                var user = await _context.MainAdminUsers
                    .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

                return user != null ? MapToUserDto(user) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by ID: {UserId}", userId);
                return null;
            }
        }

        public async Task<IEnumerable<UserDto>> GetAllAdminUsersAsync()
        {
            try
            {
                var adminUsers = await _context.MainAdminUsers
                    .Where(u => u.IsActive)
                    .OrderBy(u => u.FirstName)
                    .ThenBy(u => u.LastName)
                    .ToListAsync();

                return adminUsers.Select(MapToUserDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all admin users");
                return Enumerable.Empty<UserDto>();
            }
        }


        public async Task<AuthResponseDto> UpdateUserAsync(int userId, UpdateUserRequestDto updateDto)
        {
            try
            {
                var user = await _context.MainAdminUsers
                    .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

                if (user == null)
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "User not found"
                    };
                }

                // Update user properties
                if (!string.IsNullOrEmpty(updateDto.FirstName))
                    user.FirstName = updateDto.FirstName;
                if (!string.IsNullOrEmpty(updateDto.LastName))
                    user.LastName = updateDto.LastName;
                if (!string.IsNullOrEmpty(updateDto.PhoneNumber))
                    user.PhoneNumber = updateDto.PhoneNumber;
                if (!string.IsNullOrEmpty(updateDto.Country))
                    user.Country = updateDto.Country;
                if (!string.IsNullOrEmpty(updateDto.Industry))
                    user.Industry = updateDto.Industry;
                if (!string.IsNullOrEmpty(updateDto.CompanyName))
                    user.CompanyName = updateDto.CompanyName;
                if (!string.IsNullOrEmpty(updateDto.CompanyWebsite))
                    user.CompanyWebsite = updateDto.CompanyWebsite;
                // CompanyLogoUrl: update if provided (empty string = remove, URL = set)
                if (updateDto.CompanyLogoUrl != null)
                    user.CompanyLogoUrl = string.IsNullOrEmpty(updateDto.CompanyLogoUrl) ? null : updateDto.CompanyLogoUrl;
                if (!string.IsNullOrEmpty(updateDto.Preferences))
                    user.PreferencesJson = updateDto.Preferences;
                if (updateDto.OnboardingCompleted.HasValue)
                    user.OnboardingCompleted = updateDto.OnboardingCompleted.Value;

                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return new AuthResponseDto
                {
                    Success = true,
                    Message = "User updated successfully",
                    User = MapToUserDto(user)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user: {UserId}", userId);
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "An error occurred during user update"
                };
            }
        }

        public async Task<AuthResponseDto> ChangePasswordAsync(int userId, ChangePasswordRequestDto changePasswordDto)
        {
            try
            {
                var user = await _context.MainAdminUsers
                    .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

                if (user == null)
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "User not found"
                    };
                }

                // Verify current password
                if (!VerifyPassword(changePasswordDto.CurrentPassword, user.PasswordHash))
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Current password is incorrect"
                    };
                }

                // Hash new password
                user.PasswordHash = HashPassword(changePasswordDto.NewPassword);
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return new AuthResponseDto
                {
                    Success = true,
                    Message = "Password changed successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user: {UserId}", userId);
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "An error occurred while changing password"
                };
            }
        }

        public async Task<bool> LogoutAsync(int userId)
        {
            try
            {
                var user = await _context.MainAdminUsers
                    .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

                if (user != null)
                {
                    user.AccessToken = null;
                    user.RefreshToken = null;
                    user.TokenExpiresAt = null;
                    user.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout for user: {UserId}", userId);
                return false;
            }
        }

        // Generate tokens for MainAdminUser (Id=1 always)
        private (string accessToken, string refreshToken, DateTime expiresAt) GenerateTokensAsync(MainAdminUser user)
        {
            var jwtKey = _configuration["Jwt:Key"] ?? "YourSuperSecretKeyHere12345";
            var jwtIssuer = _configuration["Jwt:Issuer"] ?? "FlowServiceBackend";
            var jwtAudience = _configuration["Jwt:Audience"] ?? "FlowServiceFrontend";

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
                new Claim("UserId", user.Id.ToString()),
                new Claim("FirstName", user.FirstName),
                new Claim("LastName", user.LastName),
                new Claim("Industry", user.Industry ?? ""),
                new Claim("UserType", "MainAdminUser")
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expiresAt = DateTime.UtcNow.AddHours(24); // 24 hours expiry

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials
            );

            var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
            var refreshToken = GenerateRefreshToken();

            return (accessToken, refreshToken, expiresAt);
        }

        private string GenerateRefreshToken()
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        private string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12));
        }

        private bool VerifyPassword(string password, string hashedPassword)
        {
            // Handle OAuth users with default password
            if (hashedPassword == "nopassword" && password == "nopassword")
            {
                return true;
            }
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }

        public async Task<UserDto?> GetUserByEmailAsync(string email)
        {
            try
            {
                var user = await _context.MainAdminUsers
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower() && u.IsActive);

                return user != null ? MapToUserDto(user) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by email: {Email}", email);
                return null;
            }
        }

        public async Task<AuthResponseDto> OAuthLoginAsync(string email)
        {
            try
            {
                var user = await _context.MainAdminUsers
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower() && u.IsActive);

                if (user == null)
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "User not found. Please complete signup."
                    };
                }

                var (accessToken, refreshToken, expiresAt) = GenerateTokensAsync(user);

                // Update user login info
                user.LastLoginAt = DateTime.UtcNow;
                user.LastLoginDate = DateTime.UtcNow;
                user.AccessToken = accessToken;
                user.RefreshToken = refreshToken;
                user.TokenExpiresAt = expiresAt;
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return new AuthResponseDto
                {
                    Success = true,
                    Message = "OAuth login successful",
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = expiresAt,
                    User = MapToUserDto(user)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during OAuth login for email: {Email}", email);
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "An error occurred during OAuth login"
                };
            }
        }

        private UserDto MapToUserDto(MainAdminUser user)
        {
            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                Country = user.Country ?? "US",
                Industry = user.Industry ?? "",
                CompanyName = user.CompanyName,
                CompanyWebsite = user.CompanyWebsite,
                CompanyLogoUrl = user.CompanyLogoUrl,
                Preferences = user.PreferencesJson,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt ?? user.LastLoginDate,
                OnboardingCompleted = user.OnboardingCompleted
            };
        }

        private UserDto MapUserToUserDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.Phone ?? user.PhoneNumber,
                Country = user.Country ?? "US",
                Industry = user.Role ?? "User",
                CompanyName = "",
                CompanyWebsite = "",
                Preferences = "",
                CreatedAt = user.CreatedDate,
                LastLoginAt = user.LastLoginAt,
                OnboardingCompleted = true
            };
        }

        // Generate tokens for regular Users (Id >= 2)
        private (string accessToken, string refreshToken, DateTime expiresAt) GenerateUserTokensAsync(User user)
        {
            var jwtKey = _configuration["Jwt:Key"] ?? "YourSuperSecretKeyHere12345";
            var jwtIssuer = _configuration["Jwt:Issuer"] ?? "FlowServiceBackend";
            var jwtAudience = _configuration["Jwt:Audience"] ?? "FlowServiceFrontend";

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
                new Claim("UserId", user.Id.ToString()),
                new Claim("FirstName", user.FirstName),
                new Claim("LastName", user.LastName),
                new Claim("Role", user.Role ?? "User"),
                new Claim("UserType", "RegularUser")
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expiresAt = DateTime.UtcNow.AddHours(24);

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials
            );

            var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
            var refreshToken = GenerateRefreshToken();

            return (accessToken, refreshToken, expiresAt);
        }
    }
}
