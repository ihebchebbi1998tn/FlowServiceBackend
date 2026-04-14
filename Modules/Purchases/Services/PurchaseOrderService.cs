using Microsoft.EntityFrameworkCore;
using MyApi.Data;
using MyApi.Modules.Purchases.DTOs;
using MyApi.Modules.Purchases.Models;

namespace MyApi.Modules.Purchases.Services
{
    public class PurchaseOrderService : IPurchaseOrderService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PurchaseOrderService> _logger;
        private readonly MyApi.Modules.Numbering.Services.INumberingService? _numberingService;

        public PurchaseOrderService(ApplicationDbContext context, ILogger<PurchaseOrderService> logger,
            MyApi.Modules.Numbering.Services.INumberingService? numberingService = null)
        {
            _context = context;
            _logger = logger;
            _numberingService = numberingService;
        }

        public async Task<PaginatedPurchaseOrderResponse> GetOrdersAsync(
            string? status, string? supplierId, string? paymentStatus,
            DateTime? dateFrom, DateTime? dateTo, string? search,
            int page, int limit, string sortBy, string sortOrder)
        {
            var query = _context.PurchaseOrders.AsNoTracking().Where(o => !o.IsDeleted).AsQueryable();

            if (!string.IsNullOrEmpty(status)) query = query.Where(o => o.Status == status);
            if (!string.IsNullOrEmpty(supplierId) && int.TryParse(supplierId, out int sid))
                query = query.Where(o => o.SupplierId == sid);
            if (!string.IsNullOrEmpty(paymentStatus)) query = query.Where(o => o.PaymentStatus == paymentStatus);
            if (dateFrom.HasValue) query = query.Where(o => o.OrderDate >= dateFrom.Value);
            if (dateTo.HasValue) query = query.Where(o => o.OrderDate <= dateTo.Value);
            if (!string.IsNullOrEmpty(search))
            {
                var s = search.ToLower();
                query = query.Where(o =>
                    (o.OrderNumber != null && o.OrderNumber.ToLower().Contains(s)) ||
                    (o.Title != null && o.Title.ToLower().Contains(s)) ||
                    (o.SupplierName != null && o.SupplierName.ToLower().Contains(s)));
            }

            var total = await query.CountAsync();
            query = sortBy switch
            {
                "order_date" => sortOrder == "asc" ? query.OrderBy(o => o.OrderDate) : query.OrderByDescending(o => o.OrderDate),
                "grand_total" => sortOrder == "asc" ? query.OrderBy(o => o.GrandTotal) : query.OrderByDescending(o => o.GrandTotal),
                _ => sortOrder == "asc" ? query.OrderBy(o => o.CreatedDate) : query.OrderByDescending(o => o.CreatedDate)
            };

            var orders = await query.Skip((page - 1) * limit).Take(limit).Include(o => o.Items).ToListAsync();

            return new PaginatedPurchaseOrderResponse
            {
                Orders = orders.Select(MapToDto).ToList(),
                Pagination = new PurchasePaginationInfo { Page = page, Limit = limit, Total = total, TotalPages = (int)Math.Ceiling((double)total / limit) }
            };
        }

        public async Task<PurchaseOrderDto?> GetOrderByIdAsync(int id)
        {
            var order = await _context.PurchaseOrders.AsNoTracking().Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);
            return order == null ? null : MapToDto(order);
        }

        public async Task<PurchaseOrderDto> CreateOrderAsync(CreatePurchaseOrderDto dto, string userId)
        {
            var supplier = await _context.Contacts.FindAsync(dto.SupplierId)
                ?? throw new KeyNotFoundException($"Supplier with ID {dto.SupplierId} not found");

            string orderNumber;
            try { orderNumber = _numberingService != null ? await _numberingService.GetNextAsync("PurchaseOrder") : $"PO-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..5].ToUpper()}"; }
            catch { orderNumber = $"PO-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..5].ToUpper()}"; }

            var order = new PurchaseOrder
            {
                OrderNumber = orderNumber,
                Title = dto.Title,
                Description = dto.Description,
                SupplierId = dto.SupplierId,
                SupplierName = supplier.Name ?? string.Empty,
                SupplierEmail = supplier.Email,
                SupplierPhone = supplier.Phone,
                SupplierAddress = supplier.Address,
                Status = "draft",
                OrderDate = DateTime.UtcNow,
                ExpectedDelivery = dto.ExpectedDelivery,
                Currency = dto.Currency,
                Discount = dto.Discount,
                DiscountType = dto.DiscountType,
                FiscalStamp = dto.FiscalStamp,
                PaymentTerms = dto.PaymentTerms,
                Notes = dto.Notes,
                Tags = dto.Tags,
                BillingAddress = dto.BillingAddress,
                DeliveryAddress = dto.DeliveryAddress,
                ServiceOrderId = dto.ServiceOrderId,
                SaleId = dto.SaleId,
                CreatedBy = userId,
                CreatedDate = DateTime.UtcNow
            };

            _context.PurchaseOrders.Add(order);
            await _context.SaveChangesAsync();

            if (dto.Items?.Any() == true)
            {
                var items = dto.Items.Select((item, idx) => new PurchaseOrderItem
                {
                    PurchaseOrderId = order.Id,
                    ArticleId = item.ArticleId,
                    ArticleName = item.ArticleName,
                    ArticleNumber = item.ArticleNumber,
                    SupplierRef = item.SupplierRef,
                    Description = item.Description,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TaxRate = item.TaxRate,
                    Discount = item.Discount,
                    DiscountType = item.DiscountType,
                    Unit = item.Unit,
                    DisplayOrder = idx,
                    LineTotal = CalculateLineTotal(item.Quantity, item.UnitPrice, item.Discount, item.DiscountType, item.TaxRate)
                }).ToList();
                _context.PurchaseOrderItems.AddRange(items);
                await _context.SaveChangesAsync();
                RecalculateTotals(order, items);
                await _context.SaveChangesAsync();
            }

            LogActivity("purchase_order", order.Id, "created", $"Purchase order {orderNumber} created", userId);
            await _context.SaveChangesAsync();

            return (await GetOrderByIdAsync(order.Id))!;
        }

        public async Task<PurchaseOrderDto> UpdateOrderAsync(int id, UpdatePurchaseOrderDto dto, string userId)
        {
            var order = await _context.PurchaseOrders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted)
                ?? throw new KeyNotFoundException($"PurchaseOrder {id} not found");

            var oldStatus = order.Status;
            if (dto.Title != null) order.Title = dto.Title;
            if (dto.Description != null) order.Description = dto.Description;
            if (dto.Status != null) order.Status = dto.Status;
            if (dto.ExpectedDelivery.HasValue) order.ExpectedDelivery = dto.ExpectedDelivery;
            if (dto.Discount.HasValue) order.Discount = dto.Discount.Value;
            if (dto.DiscountType != null) order.DiscountType = dto.DiscountType;
            if (dto.FiscalStamp.HasValue) order.FiscalStamp = dto.FiscalStamp.Value;
            if (dto.PaymentTerms != null) order.PaymentTerms = dto.PaymentTerms;
            if (dto.PaymentStatus != null) order.PaymentStatus = dto.PaymentStatus;
            if (dto.Notes != null) order.Notes = dto.Notes;
            if (dto.Tags != null) order.Tags = dto.Tags;
            if (dto.BillingAddress != null) order.BillingAddress = dto.BillingAddress;
            if (dto.DeliveryAddress != null) order.DeliveryAddress = dto.DeliveryAddress;
            order.ModifiedDate = DateTime.UtcNow;
            order.ModifiedBy = userId;

            if (order.Items != null) RecalculateTotals(order, order.Items.ToList());

            if (dto.Status != null && dto.Status != oldStatus)
                LogActivity("purchase_order", id, "status_changed", $"Status changed from {oldStatus} to {dto.Status}", userId, oldStatus, dto.Status);

            await _context.SaveChangesAsync();
            return (await GetOrderByIdAsync(id))!;
        }

        public async Task<bool> DeleteOrderAsync(int id, string userId)
        {
            var order = await _context.PurchaseOrders.FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);
            if (order == null) return false;
            order.IsDeleted = true; order.DeletedAt = DateTime.UtcNow; order.DeletedBy = userId;
            LogActivity("purchase_order", id, "deleted", $"Purchase order {order.OrderNumber} deleted", userId);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<PurchaseOrderStatsDto> GetStatsAsync(DateTime? dateFrom, DateTime? dateTo)
        {
            var query = _context.PurchaseOrders.AsNoTracking().Where(o => !o.IsDeleted);
            if (dateFrom.HasValue) query = query.Where(o => o.OrderDate >= dateFrom.Value);
            if (dateTo.HasValue) query = query.Where(o => o.OrderDate <= dateTo.Value);

            var orders = await query.ToListAsync();
            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            return new PurchaseOrderStatsDto
            {
                TotalOrders = orders.Count,
                DraftOrders = orders.Count(o => o.Status == "draft"),
                OrderedOrders = orders.Count(o => o.Status == "ordered"),
                ReceivedOrders = orders.Count(o => o.Status == "received"),
                CancelledOrders = orders.Count(o => o.Status == "cancelled"),
                TotalSpend = orders.Where(o => o.Status != "cancelled" && o.Status != "draft").Sum(o => o.GrandTotal),
                MonthlySpend = orders.Where(o => o.OrderDate >= monthStart && o.Status != "cancelled").Sum(o => o.GrandTotal),
                AvgLeadTime = orders.Where(o => o.ActualDelivery.HasValue).Select(o => (o.ActualDelivery!.Value - o.OrderDate).TotalDays).DefaultIfEmpty(0).Average(),
                PendingReceipts = orders.Count(o => o.Status == "ordered" || o.Status == "partially_received"),
                OverdueInvoices = await _context.SupplierInvoices.CountAsync(i => !i.IsDeleted && i.DueDate < now && i.Status != "paid" && i.Status != "cancelled")
            };
        }

        public async Task<PurchaseOrderItemDto> AddItemAsync(int orderId, CreatePurchaseOrderItemDto dto)
        {
            var order = await _context.PurchaseOrders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == orderId && !o.IsDeleted)
                ?? throw new KeyNotFoundException($"PurchaseOrder {orderId} not found");
            var item = new PurchaseOrderItem
            {
                PurchaseOrderId = orderId, ArticleId = dto.ArticleId, ArticleName = dto.ArticleName,
                ArticleNumber = dto.ArticleNumber, SupplierRef = dto.SupplierRef, Description = dto.Description,
                Quantity = dto.Quantity, UnitPrice = dto.UnitPrice, TaxRate = dto.TaxRate,
                Discount = dto.Discount, DiscountType = dto.DiscountType, Unit = dto.Unit,
                DisplayOrder = (order.Items?.Count ?? 0),
                LineTotal = CalculateLineTotal(dto.Quantity, dto.UnitPrice, dto.Discount, dto.DiscountType, dto.TaxRate)
            };
            _context.PurchaseOrderItems.Add(item);
            await _context.SaveChangesAsync();
            RecalculateTotals(order, order.Items!.ToList());
            await _context.SaveChangesAsync();
            return MapItemToDto(item);
        }

        public async Task<PurchaseOrderItemDto> UpdateItemAsync(int orderId, int itemId, CreatePurchaseOrderItemDto dto)
        {
            var order = await _context.PurchaseOrders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == orderId && !o.IsDeleted)
                ?? throw new KeyNotFoundException($"PurchaseOrder {orderId} not found");
            var item = order.Items?.FirstOrDefault(i => i.Id == itemId)
                ?? throw new KeyNotFoundException($"Item {itemId} not found");
            item.ArticleId = dto.ArticleId; item.ArticleName = dto.ArticleName; item.ArticleNumber = dto.ArticleNumber;
            item.SupplierRef = dto.SupplierRef; item.Description = dto.Description; item.Quantity = dto.Quantity;
            item.UnitPrice = dto.UnitPrice; item.TaxRate = dto.TaxRate; item.Discount = dto.Discount;
            item.DiscountType = dto.DiscountType; item.Unit = dto.Unit;
            item.LineTotal = CalculateLineTotal(dto.Quantity, dto.UnitPrice, dto.Discount, dto.DiscountType, dto.TaxRate);
            RecalculateTotals(order, order.Items!.ToList());
            await _context.SaveChangesAsync();
            return MapItemToDto(item);
        }

        public async Task<bool> DeleteItemAsync(int orderId, int itemId)
        {
            var order = await _context.PurchaseOrders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == orderId && !o.IsDeleted);
            var item = order?.Items?.FirstOrDefault(i => i.Id == itemId);
            if (item == null) return false;
            _context.PurchaseOrderItems.Remove(item);
            await _context.SaveChangesAsync();
            RecalculateTotals(order!, order!.Items!.ToList());
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<PurchaseActivityDto>> GetActivitiesAsync(int orderId, int page, int limit)
        {
            return await _context.PurchaseActivities.AsNoTracking()
                .Where(a => a.EntityType == "purchase_order" && a.EntityId == orderId)
                .OrderByDescending(a => a.PerformedAt)
                .Skip((page - 1) * limit).Take(limit)
                .Select(a => new PurchaseActivityDto
                {
                    Id = a.Id, EntityType = a.EntityType, EntityId = a.EntityId,
                    ActivityType = a.ActivityType, Description = a.Description,
                    OldValue = a.OldValue, NewValue = a.NewValue,
                    PerformedBy = a.PerformedBy, PerformedByName = a.PerformedByName,
                    PerformedAt = a.PerformedAt
                }).ToListAsync();
        }

        // ── Helpers ──

        private static decimal CalculateLineTotal(decimal qty, decimal price, decimal discount, string discountType, decimal taxRate)
        {
            var subtotal = qty * price;
            var discountAmount = discountType == "percentage" ? subtotal * discount / 100 : discount;
            var afterDiscount = subtotal - discountAmount;
            return afterDiscount + (afterDiscount * taxRate / 100);
        }

        private static void RecalculateTotals(PurchaseOrder order, List<PurchaseOrderItem> items)
        {
            order.SubTotal = items.Sum(i => i.Quantity * i.UnitPrice);
            var discAmt = order.DiscountType == "percentage" ? order.SubTotal * order.Discount / 100 : order.Discount;
            var afterDiscount = order.SubTotal - discAmt;
            order.TaxAmount = items.Sum(i =>
            {
                var sub = i.Quantity * i.UnitPrice;
                var d = i.DiscountType == "percentage" ? sub * i.Discount / 100 : i.Discount;
                return (sub - d) * i.TaxRate / 100;
            });
            order.GrandTotal = afterDiscount + order.TaxAmount + order.FiscalStamp;
        }

        private void LogActivity(string entityType, int entityId, string activityType, string desc, string userId, string? oldVal = null, string? newVal = null)
        {
            _context.PurchaseActivities.Add(new PurchaseActivity
            {
                EntityType = entityType, EntityId = entityId, ActivityType = activityType,
                Description = desc, OldValue = oldVal, NewValue = newVal,
                PerformedBy = userId, PerformedAt = DateTime.UtcNow
            });
        }

        private static PurchaseOrderDto MapToDto(PurchaseOrder o) => new()
        {
            Id = o.Id, OrderNumber = o.OrderNumber, Title = o.Title, Description = o.Description,
            SupplierId = o.SupplierId, SupplierName = o.SupplierName, SupplierEmail = o.SupplierEmail,
            SupplierPhone = o.SupplierPhone, SupplierAddress = o.SupplierAddress,
            SupplierMatriculeFiscale = o.SupplierMatriculeFiscale,
            Status = o.Status, OrderDate = o.OrderDate, ExpectedDelivery = o.ExpectedDelivery,
            ActualDelivery = o.ActualDelivery, Currency = o.Currency, SubTotal = o.SubTotal,
            Discount = o.Discount, DiscountType = o.DiscountType, TaxAmount = o.TaxAmount,
            FiscalStamp = o.FiscalStamp, GrandTotal = o.GrandTotal, PaymentTerms = o.PaymentTerms,
            PaymentStatus = o.PaymentStatus, Notes = o.Notes, Tags = o.Tags,
            BillingAddress = o.BillingAddress, DeliveryAddress = o.DeliveryAddress,
            ServiceOrderId = o.ServiceOrderId, SaleId = o.SaleId, ApprovedBy = o.ApprovedBy,
            ApprovalDate = o.ApprovalDate, SentToSupplierAt = o.SentToSupplierAt,
            Items = o.Items?.Select(MapItemToDto).ToList(),
            CreatedDate = o.CreatedDate, CreatedBy = o.CreatedBy, CreatedByName = o.CreatedByName,
            ModifiedDate = o.ModifiedDate, ModifiedBy = o.ModifiedBy
        };

        private static PurchaseOrderItemDto MapItemToDto(PurchaseOrderItem i) => new()
        {
            Id = i.Id, PurchaseOrderId = i.PurchaseOrderId, ArticleId = i.ArticleId,
            ArticleName = i.ArticleName, ArticleNumber = i.ArticleNumber, SupplierRef = i.SupplierRef,
            Description = i.Description, Quantity = i.Quantity, ReceivedQty = i.ReceivedQty,
            UnitPrice = i.UnitPrice, TaxRate = i.TaxRate, Discount = i.Discount,
            DiscountType = i.DiscountType, LineTotal = i.LineTotal, Unit = i.Unit, DisplayOrder = i.DisplayOrder
        };
    }
}
