using Microsoft.EntityFrameworkCore;
using MyApi.Data;
using MyApi.Modules.Purchases.DTOs;
using MyApi.Modules.Purchases.Models;

namespace MyApi.Modules.Purchases.Services
{
    public class SupplierInvoiceService : ISupplierInvoiceService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SupplierInvoiceService> _logger;
        private readonly MyApi.Modules.Numbering.Services.INumberingService? _numberingService;

        public SupplierInvoiceService(ApplicationDbContext context, ILogger<SupplierInvoiceService> logger,
            MyApi.Modules.Numbering.Services.INumberingService? numberingService = null)
        {
            _context = context;
            _logger = logger;
            _numberingService = numberingService;
        }

        public async Task<PaginatedSupplierInvoiceResponse> GetInvoicesAsync(
            string? status, string? supplierId, bool? rsApplicable,
            DateTime? dateFrom, DateTime? dateTo, string? search,
            int page, int limit, string sortBy, string sortOrder)
        {
            var query = _context.SupplierInvoices.AsNoTracking().Where(i => !i.IsDeleted).AsQueryable();
            if (!string.IsNullOrEmpty(status)) query = query.Where(i => i.Status == status);
            if (!string.IsNullOrEmpty(supplierId) && int.TryParse(supplierId, out int sid))
                query = query.Where(i => i.SupplierId == sid);
            if (rsApplicable.HasValue) query = query.Where(i => i.RsApplicable == rsApplicable.Value);
            if (dateFrom.HasValue) query = query.Where(i => i.InvoiceDate >= dateFrom.Value);
            if (dateTo.HasValue) query = query.Where(i => i.InvoiceDate <= dateTo.Value);
            if (!string.IsNullOrEmpty(search))
            {
                var s = search.ToLower();
                query = query.Where(i => (i.InvoiceNumber != null && i.InvoiceNumber.ToLower().Contains(s)) ||
                    (i.SupplierName != null && i.SupplierName.ToLower().Contains(s)));
            }
            var total = await query.CountAsync();
            query = sortOrder == "asc" ? query.OrderBy(i => i.CreatedDate) : query.OrderByDescending(i => i.CreatedDate);
            var invoices = await query.Skip((page - 1) * limit).Take(limit).Include(i => i.Items).ToListAsync();

            var poIds = invoices.Where(i => i.PurchaseOrderId.HasValue).Select(i => i.PurchaseOrderId!.Value).Distinct().ToList();
            var poNumbers = await _context.PurchaseOrders.AsNoTracking().Where(p => poIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.OrderNumber);

            return new PaginatedSupplierInvoiceResponse
            {
                Invoices = invoices.Select(i => MapToDto(i, i.PurchaseOrderId.HasValue ? poNumbers.GetValueOrDefault(i.PurchaseOrderId.Value) : null)).ToList(),
                Pagination = new PurchasePaginationInfo { Page = page, Limit = limit, Total = total, TotalPages = (int)Math.Ceiling((double)total / limit) }
            };
        }

        public async Task<SupplierInvoiceDto?> GetInvoiceByIdAsync(int id)
        {
            var inv = await _context.SupplierInvoices.AsNoTracking().Include(i => i.Items).FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);
            if (inv == null) return null;
            var poNumber = inv.PurchaseOrderId.HasValue
                ? await _context.PurchaseOrders.AsNoTracking().Where(p => p.Id == inv.PurchaseOrderId.Value).Select(p => p.OrderNumber).FirstOrDefaultAsync()
                : null;
            return MapToDto(inv, poNumber);
        }

        public async Task<SupplierInvoiceDto> CreateInvoiceAsync(CreateSupplierInvoiceDto dto, string userId)
        {
            var supplier = await _context.Contacts.FindAsync(dto.SupplierId)
                ?? throw new KeyNotFoundException($"Supplier {dto.SupplierId} not found");

            string invoiceNumber;
            try { invoiceNumber = _numberingService != null ? await _numberingService.GetNextAsync("SupplierInvoice") : $"SI-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..5].ToUpper()}"; }
            catch { invoiceNumber = $"SI-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..5].ToUpper()}"; }

            var invoice = new SupplierInvoice
            {
                InvoiceNumber = invoiceNumber,
                SupplierInvoiceRef = dto.SupplierInvoiceRef,
                SupplierId = dto.SupplierId,
                SupplierName = supplier.Name ?? string.Empty,
                PurchaseOrderId = dto.PurchaseOrderId,
                GoodsReceiptId = dto.GoodsReceiptId,
                InvoiceDate = dto.InvoiceDate,
                DueDate = dto.DueDate,
                Status = "draft",
                Currency = dto.Currency,
                Discount = dto.Discount,
                DiscountType = dto.DiscountType,
                FiscalStamp = dto.FiscalStamp,
                Notes = dto.Notes,
                RsApplicable = dto.RsApplicable,
                RsTypeCode = dto.RsTypeCode,
                CreatedBy = userId,
                CreatedDate = DateTime.UtcNow
            };

            _context.SupplierInvoices.Add(invoice);
            await _context.SaveChangesAsync();

            if (dto.Items?.Any() == true)
            {
                var items = dto.Items.Select((item, idx) => new SupplierInvoiceItem
                {
                    SupplierInvoiceId = invoice.Id,
                    PurchaseOrderItemId = item.PurchaseOrderItemId,
                    ArticleId = item.ArticleId,
                    ArticleName = item.ArticleName,
                    Description = item.Description,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TaxRate = item.TaxRate,
                    LineTotal = item.Quantity * item.UnitPrice * (1 + item.TaxRate / 100),
                    DisplayOrder = idx
                }).ToList();
                _context.SupplierInvoiceItems.AddRange(items);
                await _context.SaveChangesAsync();

                // Recalculate totals
                invoice.SubTotal = items.Sum(i => i.Quantity * i.UnitPrice);
                var discAmt = invoice.DiscountType == "percentage" ? invoice.SubTotal * invoice.Discount / 100 : invoice.Discount;
                invoice.TaxAmount = items.Sum(i => i.Quantity * i.UnitPrice * i.TaxRate / 100);
                invoice.GrandTotal = invoice.SubTotal - discAmt + invoice.TaxAmount + invoice.FiscalStamp;

                // Calculate RS if applicable
                if (invoice.RsApplicable && !string.IsNullOrEmpty(invoice.RsTypeCode))
                {
                    var rsRate = invoice.RsTypeCode switch
                    {
                        "P1" => 1.5m, "P2" => 5m, "P3" => 10m, "P4" => 15m, "P5" => 25m, _ => 0m
                    };
                    invoice.RsAmount = (invoice.SubTotal - discAmt) * rsRate / 100;
                }
                await _context.SaveChangesAsync();
            }

            _context.PurchaseActivities.Add(new PurchaseActivity
            {
                EntityType = "supplier_invoice", EntityId = invoice.Id, ActivityType = "created",
                Description = $"Supplier invoice {invoiceNumber} created", PerformedBy = userId, PerformedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            return (await GetInvoiceByIdAsync(invoice.Id))!;
        }

        public async Task<SupplierInvoiceDto> UpdateInvoiceAsync(int id, UpdateSupplierInvoiceDto dto, string userId)
        {
            var invoice = await _context.SupplierInvoices.FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted)
                ?? throw new KeyNotFoundException($"SupplierInvoice {id} not found");

            var oldStatus = invoice.Status;
            if (dto.SupplierInvoiceRef != null) invoice.SupplierInvoiceRef = dto.SupplierInvoiceRef;
            if (dto.Status != null) invoice.Status = dto.Status;
            if (dto.DueDate.HasValue) invoice.DueDate = dto.DueDate.Value;
            if (dto.Discount.HasValue) invoice.Discount = dto.Discount.Value;
            if (dto.DiscountType != null) invoice.DiscountType = dto.DiscountType;
            if (dto.FiscalStamp.HasValue) invoice.FiscalStamp = dto.FiscalStamp.Value;
            if (dto.PaymentMethod != null) invoice.PaymentMethod = dto.PaymentMethod;
            if (dto.AmountPaid.HasValue)
            {
                invoice.AmountPaid = dto.AmountPaid.Value;
                if (invoice.AmountPaid >= invoice.GrandTotal)
                {
                    invoice.Status = "paid";
                    invoice.PaymentDate = DateTime.UtcNow;
                }
                else if (invoice.AmountPaid > 0)
                {
                    invoice.Status = "partially_paid";
                }
            }
            if (dto.PaymentDate.HasValue) invoice.PaymentDate = dto.PaymentDate;
            if (dto.Notes != null) invoice.Notes = dto.Notes;
            if (dto.RsApplicable.HasValue) invoice.RsApplicable = dto.RsApplicable.Value;
            if (dto.RsTypeCode != null) invoice.RsTypeCode = dto.RsTypeCode;
            // TEJ sync fields
            if (dto.TejSynced.HasValue) invoice.TejSynced = dto.TejSynced.Value;
            if (dto.TejSyncDate.HasValue) invoice.TejSyncDate = dto.TejSyncDate;
            if (dto.TejSyncStatus != null) invoice.TejSyncStatus = dto.TejSyncStatus;
            if (dto.TejErrorMessage != null) invoice.TejErrorMessage = dto.TejErrorMessage;
            // Facture en ligne fields
            if (dto.FactureEnLigneId != null) invoice.FactureEnLigneId = dto.FactureEnLigneId;
            if (dto.FactureEnLigneStatus != null) invoice.FactureEnLigneStatus = dto.FactureEnLigneStatus;
            if (dto.FactureEnLigneSentAt.HasValue) invoice.FactureEnLigneSentAt = dto.FactureEnLigneSentAt;
            invoice.ModifiedDate = DateTime.UtcNow;
            invoice.ModifiedBy = userId;

            if (dto.Status != null && dto.Status != oldStatus)
            {
                _context.PurchaseActivities.Add(new PurchaseActivity
                {
                    EntityType = "supplier_invoice", EntityId = id, ActivityType = "status_changed",
                    Description = $"Status changed from {oldStatus} to {dto.Status}",
                    OldValue = oldStatus, NewValue = dto.Status, PerformedBy = userId, PerformedAt = DateTime.UtcNow
                });
            }
            await _context.SaveChangesAsync();
            return (await GetInvoiceByIdAsync(id))!;
        }

        public async Task<bool> DeleteInvoiceAsync(int id, string userId)
        {
            var invoice = await _context.SupplierInvoices.FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);
            if (invoice == null) return false;
            invoice.IsDeleted = true; invoice.DeletedAt = DateTime.UtcNow; invoice.DeletedBy = userId;
            await _context.SaveChangesAsync();
            return true;
        }

        private static SupplierInvoiceDto MapToDto(SupplierInvoice inv, string? poNumber) => new()
        {
            Id = inv.Id, InvoiceNumber = inv.InvoiceNumber, SupplierInvoiceRef = inv.SupplierInvoiceRef,
            SupplierId = inv.SupplierId, SupplierName = inv.SupplierName,
            SupplierMatriculeFiscale = inv.SupplierMatriculeFiscale,
            PurchaseOrderId = inv.PurchaseOrderId, PurchaseOrderNumber = poNumber,
            GoodsReceiptId = inv.GoodsReceiptId, InvoiceDate = inv.InvoiceDate, DueDate = inv.DueDate,
            Status = inv.Status, Currency = inv.Currency, SubTotal = inv.SubTotal,
            Discount = inv.Discount, DiscountType = inv.DiscountType, TaxAmount = inv.TaxAmount,
            FiscalStamp = inv.FiscalStamp, GrandTotal = inv.GrandTotal, AmountPaid = inv.AmountPaid,
            PaymentMethod = inv.PaymentMethod, PaymentDate = inv.PaymentDate, Notes = inv.Notes,
            RsApplicable = inv.RsApplicable, RsTypeCode = inv.RsTypeCode, RsAmount = inv.RsAmount,
            RsRecordId = inv.RsRecordId,
            FactureEnLigneId = inv.FactureEnLigneId, FactureEnLigneStatus = inv.FactureEnLigneStatus,
            FactureEnLigneSentAt = inv.FactureEnLigneSentAt,
            TejSynced = inv.TejSynced, TejSyncDate = inv.TejSyncDate, TejSyncStatus = inv.TejSyncStatus,
            TejErrorMessage = inv.TejErrorMessage,
            Items = inv.Items?.Select(i => new SupplierInvoiceItemDto
            {
                Id = i.Id, SupplierInvoiceId = i.SupplierInvoiceId, PurchaseOrderItemId = i.PurchaseOrderItemId,
                ArticleId = i.ArticleId, ArticleName = i.ArticleName, Description = i.Description,
                Quantity = i.Quantity, UnitPrice = i.UnitPrice, TaxRate = i.TaxRate,
                LineTotal = i.LineTotal, DisplayOrder = i.DisplayOrder
            }).ToList(),
            CreatedDate = inv.CreatedDate, CreatedBy = inv.CreatedBy, ModifiedDate = inv.ModifiedDate, ModifiedBy = inv.ModifiedBy
        };
    }
}
