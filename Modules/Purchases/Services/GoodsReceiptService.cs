using Microsoft.EntityFrameworkCore;
using MyApi.Data;
using MyApi.Modules.Purchases.DTOs;
using MyApi.Modules.Purchases.Models;

namespace MyApi.Modules.Purchases.Services
{
    public class GoodsReceiptService : IGoodsReceiptService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<GoodsReceiptService> _logger;
        private readonly MyApi.Modules.Numbering.Services.INumberingService? _numberingService;

        public GoodsReceiptService(ApplicationDbContext context, ILogger<GoodsReceiptService> logger,
            MyApi.Modules.Numbering.Services.INumberingService? numberingService = null)
        {
            _context = context;
            _logger = logger;
            _numberingService = numberingService;
        }

        public async Task<PaginatedGoodsReceiptResponse> GetReceiptsAsync(
            int? purchaseOrderId, string? supplierId, string? status,
            DateTime? dateFrom, DateTime? dateTo, string? search,
            int page, int limit, string sortBy, string sortOrder)
        {
            var query = _context.GoodsReceipts.AsNoTracking().AsQueryable();
            if (purchaseOrderId.HasValue) query = query.Where(r => r.PurchaseOrderId == purchaseOrderId.Value);
            if (!string.IsNullOrEmpty(supplierId) && int.TryParse(supplierId, out int sid))
                query = query.Where(r => r.SupplierId == sid);
            if (!string.IsNullOrEmpty(status)) query = query.Where(r => r.Status == status);
            if (dateFrom.HasValue) query = query.Where(r => r.ReceiptDate >= dateFrom.Value);
            if (dateTo.HasValue) query = query.Where(r => r.ReceiptDate <= dateTo.Value);
            if (!string.IsNullOrEmpty(search))
            {
                var s = search.ToLower();
                query = query.Where(r => (r.ReceiptNumber != null && r.ReceiptNumber.ToLower().Contains(s)) ||
                    (r.SupplierName != null && r.SupplierName.ToLower().Contains(s)));
            }
            var total = await query.CountAsync();
            query = sortOrder == "asc" ? query.OrderBy(r => r.CreatedDate) : query.OrderByDescending(r => r.CreatedDate);
            var receipts = await query.Skip((page - 1) * limit).Take(limit).Include(r => r.Items).ToListAsync();

            // Get PO numbers
            var poIds = receipts.Select(r => r.PurchaseOrderId).Distinct().ToList();
            var poNumbers = await _context.PurchaseOrders.AsNoTracking().Where(p => poIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.OrderNumber);

            return new PaginatedGoodsReceiptResponse
            {
                Receipts = receipts.Select(r => MapToDto(r, poNumbers.GetValueOrDefault(r.PurchaseOrderId))).ToList(),
                Pagination = new PurchasePaginationInfo { Page = page, Limit = limit, Total = total, TotalPages = (int)Math.Ceiling((double)total / limit) }
            };
        }

        public async Task<GoodsReceiptDto?> GetReceiptByIdAsync(int id)
        {
            var receipt = await _context.GoodsReceipts.AsNoTracking().Include(r => r.Items).FirstOrDefaultAsync(r => r.Id == id);
            if (receipt == null) return null;
            var poNumber = await _context.PurchaseOrders.AsNoTracking().Where(p => p.Id == receipt.PurchaseOrderId).Select(p => p.OrderNumber).FirstOrDefaultAsync();
            return MapToDto(receipt, poNumber);
        }

        public async Task<GoodsReceiptDto> CreateReceiptAsync(CreateGoodsReceiptDto dto, string userId)
        {
            var po = await _context.PurchaseOrders.Include(p => p.Items).FirstOrDefaultAsync(p => p.Id == dto.PurchaseOrderId && !p.IsDeleted)
                ?? throw new KeyNotFoundException($"PurchaseOrder {dto.PurchaseOrderId} not found");

            string receiptNumber;
            try { receiptNumber = _numberingService != null ? await _numberingService.GetNextAsync("GoodsReceipt") : $"GR-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..5].ToUpper()}"; }
            catch { receiptNumber = $"GR-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..5].ToUpper()}"; }

            var receipt = new GoodsReceipt
            {
                ReceiptNumber = receiptNumber,
                PurchaseOrderId = po.Id,
                SupplierId = po.SupplierId,
                SupplierName = po.SupplierName,
                ReceiptDate = dto.ReceiptDate ?? DateTime.UtcNow,
                Status = "partial",
                DeliveryNoteRef = dto.DeliveryNoteRef,
                Notes = dto.Notes,
                ReceivedBy = userId,
                CreatedBy = userId,
                CreatedDate = DateTime.UtcNow
            };

            _context.GoodsReceipts.Add(receipt);
            await _context.SaveChangesAsync();

            if (dto.Items?.Any() == true)
            {
                foreach (var itemDto in dto.Items)
                {
                    var poItem = po.Items?.FirstOrDefault(i => i.Id == itemDto.PurchaseOrderItemId);
                    var grItem = new GoodsReceiptItem
                    {
                        GoodsReceiptId = receipt.Id,
                        PurchaseOrderItemId = itemDto.PurchaseOrderItemId,
                        ArticleId = poItem?.ArticleId,
                        ArticleName = poItem?.ArticleName,
                        ArticleNumber = poItem?.ArticleNumber,
                        OrderedQty = poItem?.Quantity ?? 0,
                        QuantityReceived = itemDto.QuantityReceived,
                        QuantityRejected = itemDto.QuantityRejected,
                        RejectionReason = itemDto.RejectionReason,
                        LocationId = itemDto.LocationId,
                        Notes = itemDto.Notes
                    };
                    _context.GoodsReceiptItems.Add(grItem);

                    // Update PO item received qty
                    if (poItem != null)
                        poItem.ReceivedQty += itemDto.QuantityReceived;
                }
                await _context.SaveChangesAsync();
            }

            // Update PO status
            var allFullyReceived = po.Items?.All(i => i.ReceivedQty >= i.Quantity) ?? false;
            var anyReceived = po.Items?.Any(i => i.ReceivedQty > 0) ?? false;
            if (allFullyReceived)
            {
                po.Status = "received";
                po.ActualDelivery = DateTime.UtcNow;
                receipt.Status = "complete";
            }
            else if (anyReceived)
            {
                po.Status = "partially_received";
            }
            await _context.SaveChangesAsync();

            // Log activity
            _context.PurchaseActivities.Add(new PurchaseActivity
            {
                EntityType = "goods_receipt", EntityId = receipt.Id, ActivityType = "created",
                Description = $"Goods receipt {receiptNumber} created for PO {po.OrderNumber}",
                PerformedBy = userId, PerformedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            return (await GetReceiptByIdAsync(receipt.Id))!;
        }

        public async Task<bool> DeleteReceiptAsync(int id, string userId)
        {
            var receipt = await _context.GoodsReceipts.Include(r => r.Items).FirstOrDefaultAsync(r => r.Id == id);
            if (receipt == null) return false;

            // Reverse ReceivedQty on PO items before deleting the receipt
            var po = await _context.PurchaseOrders.Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.Id == receipt.PurchaseOrderId && !p.IsDeleted);

            if (po != null && receipt.Items != null)
            {
                foreach (var grItem in receipt.Items)
                {
                    var poItem = po.Items?.FirstOrDefault(i => i.Id == grItem.PurchaseOrderItemId);
                    if (poItem != null)
                    {
                        poItem.ReceivedQty = Math.Max(0, poItem.ReceivedQty - grItem.QuantityReceived);
                    }
                }

                // Recalculate PO status based on remaining received quantities
                var allFullyReceived = po.Items?.All(i => i.ReceivedQty >= i.Quantity) ?? false;
                var anyReceived = po.Items?.Any(i => i.ReceivedQty > 0) ?? false;
                if (allFullyReceived)
                {
                    po.Status = "received";
                }
                else if (anyReceived)
                {
                    po.Status = "partially_received";
                }
                else
                {
                    po.Status = "ordered";
                    po.ActualDelivery = null;
                }
            }

            _context.GoodsReceiptItems.RemoveRange(receipt.Items ?? Enumerable.Empty<GoodsReceiptItem>());
            _context.GoodsReceipts.Remove(receipt);

            // Log activity
            _context.PurchaseActivities.Add(new PurchaseActivity
            {
                EntityType = "goods_receipt", EntityId = id, ActivityType = "deleted",
                Description = $"Goods receipt {receipt.ReceiptNumber} deleted, received quantities reversed",
                PerformedBy = userId, PerformedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return true;
        }

        private static GoodsReceiptDto MapToDto(GoodsReceipt r, string? poNumber) => new()
        {
            Id = r.Id, ReceiptNumber = r.ReceiptNumber, PurchaseOrderId = r.PurchaseOrderId,
            PurchaseOrderNumber = poNumber, SupplierId = r.SupplierId, SupplierName = r.SupplierName,
            ReceiptDate = r.ReceiptDate, Status = r.Status, DeliveryNoteRef = r.DeliveryNoteRef,
            Notes = r.Notes, ReceivedBy = r.ReceivedBy, ReceivedByName = r.ReceivedByName,
            Items = r.Items?.Select(i => new GoodsReceiptItemDto
            {
                Id = i.Id, GoodsReceiptId = i.GoodsReceiptId, PurchaseOrderItemId = i.PurchaseOrderItemId,
                ArticleId = i.ArticleId, ArticleName = i.ArticleName, ArticleNumber = i.ArticleNumber,
                OrderedQty = i.OrderedQty, QuantityReceived = i.QuantityReceived, QuantityRejected = i.QuantityRejected,
                RejectionReason = i.RejectionReason, LocationId = i.LocationId, Notes = i.Notes
            }).ToList(),
            CreatedDate = r.CreatedDate, CreatedBy = r.CreatedBy, ModifiedDate = r.ModifiedDate, ModifiedBy = r.ModifiedBy
        };
    }
}
