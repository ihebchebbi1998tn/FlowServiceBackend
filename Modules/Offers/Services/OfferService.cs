using Microsoft.EntityFrameworkCore;
using MyApi.Data;
using MyApi.Modules.Offers.DTOs;
using MyApi.Modules.Offers.Models;
using MyApi.Modules.Contacts.Models;

namespace MyApi.Modules.Offers.Services
{
    public class OfferService : IOfferService
    {
        private readonly ApplicationDbContext _context;

        public OfferService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedOfferResponse> GetOffersAsync(
            string? status = null,
            string? category = null,
            string? source = null,
            string? contactId = null,
            DateTime? dateFrom = null,
            DateTime? dateTo = null,
            string? search = null,
            int page = 1,
            int limit = 20,
            string sortBy = "updated_at",
            string sortOrder = "desc"
        )
        {
            var query = _context.Offers.AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(status))
                query = query.Where(o => o.Status == status);

            if (!string.IsNullOrEmpty(category))
                query = query.Where(o => o.Category == category);

            if (!string.IsNullOrEmpty(source))
                query = query.Where(o => o.Source == source);

            if (!string.IsNullOrEmpty(contactId))
                query = query.Where(o => o.ContactId == contactId);

            if (dateFrom.HasValue)
                query = query.Where(o => o.CreatedAt >= dateFrom.Value);

            if (dateTo.HasValue)
                query = query.Where(o => o.CreatedAt <= dateTo.Value);

            if (!string.IsNullOrEmpty(search))
            {
                var searchLower = search.ToLower();
                query = query.Where(o =>
                    o.Title.ToLower().Contains(searchLower) ||
                    (o.Description != null && o.Description.ToLower().Contains(searchLower))
                );
            }

            // Count total
            var total = await query.CountAsync();

            // Apply sorting
            query = sortBy.ToLower() switch
            {
                "created_at" => sortOrder.ToLower() == "asc" ? query.OrderBy(o => o.CreatedAt) : query.OrderByDescending(o => o.CreatedAt),
                "title" => sortOrder.ToLower() == "asc" ? query.OrderBy(o => o.Title) : query.OrderByDescending(o => o.Title),
                "amount" => sortOrder.ToLower() == "asc" ? query.OrderBy(o => o.Amount) : query.OrderByDescending(o => o.Amount),
                _ => sortOrder.ToLower() == "asc" ? query.OrderBy(o => o.UpdatedAt) : query.OrderByDescending(o => o.UpdatedAt)
            };

            // Apply pagination
            var offers = await query
                .Skip((page - 1) * limit)
                .Take(limit)
                .Include(o => o.Items)
                .ToListAsync();
                
            // Get contact IDs
            var contactIds = offers.Select(o => o.ContactId).Distinct().ToList();
            var contacts = await _context.Contacts
                .Where(c => contactIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id);

            var offerDtos = offers.Select(o => MapToDto(o, contacts.GetValueOrDefault(o.ContactId))).ToList();

            return new PaginatedOfferResponse
            {
                Offers = offerDtos,
                Pagination = new PaginationInfo
                {
                    Page = page,
                    Limit = limit,
                    Total = total,
                    TotalPages = (int)Math.Ceiling((double)total / limit)
                }
            };
        }

        public async Task<OfferDto?> GetOfferByIdAsync(string id)
        {
            var offer = await _context.Offers
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (offer == null) return null;
            
            var contact = await _context.Contacts.FindAsync(offer.ContactId);
            return MapToDto(offer, contact);
        }

        public async Task<OfferDto> CreateOfferAsync(CreateOfferDto createDto, string userId)
        {
            var offerId = $"OFFER-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";

            // Verify contact exists
            var contact = await _context.Contacts.FindAsync(createDto.ContactId);
            if (contact == null)
                throw new KeyNotFoundException($"Contact with ID {createDto.ContactId} not found");

            var offer = new Offer
            {
                Id = offerId,
                Title = createDto.Title,
                Description = createDto.Description,
                ContactId = createDto.ContactId,
                Status = createDto.Status,
                Category = createDto.Category,
                Source = createDto.Source,
                Currency = createDto.Currency,
                ValidUntil = createDto.ValidUntil,
                BillingAddress = createDto.BillingAddress,
                BillingPostalCode = createDto.BillingPostalCode,
                BillingCountry = createDto.BillingCountry,
                DeliveryAddress = createDto.DeliveryAddress,
                DeliveryPostalCode = createDto.DeliveryPostalCode,
                DeliveryCountry = createDto.DeliveryCountry,
                Taxes = createDto.Taxes ?? 0,
                Discount = createDto.Discount ?? 0,
                Amount = 0, // Will be calculated from items
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Tags = new string[] { }
            };

            _context.Offers.Add(offer);

            // Add items if provided
            if (createDto.Items != null && createDto.Items.Any())
            {
                var items = createDto.Items.Select(itemDto => new OfferItem
                {
                    Id = $"ITEM-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}",
                    OfferId = offerId,
                    Type = itemDto.Type,
                    ArticleId = itemDto.ArticleId,
                    ItemName = itemDto.ItemName,
                    ItemCode = itemDto.ItemCode,
                    Description = itemDto.Description,
                    Quantity = itemDto.Quantity,
                    UnitPrice = itemDto.UnitPrice,
                    Discount = itemDto.Discount,
                    DiscountType = itemDto.DiscountType,
                    InstallationId = itemDto.InstallationId,
                    InstallationName = itemDto.InstallationName
                }).ToList();

                _context.OfferItems.AddRange(items);
            }

            await _context.SaveChangesAsync();

            // Reload to get computed values
            var createdOffer = await GetOfferByIdAsync(offerId);
            return createdOffer!;
        }

        public async Task<OfferDto> UpdateOfferAsync(string id, UpdateOfferDto updateDto, string userId)
        {
            var offer = await _context.Offers.FindAsync(id);
            if (offer == null)
                throw new KeyNotFoundException($"Offer with ID {id} not found");

            // Update only provided fields
            if (updateDto.Title != null) offer.Title = updateDto.Title;
            if (updateDto.Description != null) offer.Description = updateDto.Description;
            if (updateDto.Status != null) offer.Status = updateDto.Status;
            if (updateDto.Amount.HasValue) offer.Amount = updateDto.Amount.Value;
            if (updateDto.Taxes.HasValue) offer.Taxes = updateDto.Taxes.Value;
            if (updateDto.Discount.HasValue) offer.Discount = updateDto.Discount.Value;
            if (updateDto.ValidUntil.HasValue) offer.ValidUntil = updateDto.ValidUntil;
            if (updateDto.BillingAddress != null) offer.BillingAddress = updateDto.BillingAddress;
            if (updateDto.BillingPostalCode != null) offer.BillingPostalCode = updateDto.BillingPostalCode;
            if (updateDto.BillingCountry != null) offer.BillingCountry = updateDto.BillingCountry;
            if (updateDto.DeliveryAddress != null) offer.DeliveryAddress = updateDto.DeliveryAddress;
            if (updateDto.DeliveryPostalCode != null) offer.DeliveryPostalCode = updateDto.DeliveryPostalCode;
            if (updateDto.DeliveryCountry != null) offer.DeliveryCountry = updateDto.DeliveryCountry;
            if (updateDto.Tags != null) offer.Tags = updateDto.Tags;

            offer.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var updatedOffer = await GetOfferByIdAsync(id);
            return updatedOffer!;
        }

        public async Task<bool> DeleteOfferAsync(string id)
        {
            var offer = await _context.Offers.FindAsync(id);
            if (offer == null)
                return false;

            _context.Offers.Remove(offer);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<OfferDto> RenewOfferAsync(string id, string userId)
        {
            var originalOffer = await _context.Offers
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (originalOffer == null)
                throw new KeyNotFoundException($"Offer with ID {id} not found");

            var newOfferId = $"OFFER-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";

            var renewedOffer = new Offer
            {
                Id = newOfferId,
                Title = originalOffer.Title,
                Description = originalOffer.Description,
                ContactId = originalOffer.ContactId,
                Status = "draft",
                Category = originalOffer.Category,
                Source = originalOffer.Source,
                Currency = originalOffer.Currency,
                BillingAddress = originalOffer.BillingAddress,
                BillingPostalCode = originalOffer.BillingPostalCode,
                BillingCountry = originalOffer.BillingCountry,
                DeliveryAddress = originalOffer.DeliveryAddress,
                DeliveryPostalCode = originalOffer.DeliveryPostalCode,
                DeliveryCountry = originalOffer.DeliveryCountry,
                Taxes = originalOffer.Taxes,
                Discount = originalOffer.Discount,
                Amount = 0,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Tags = originalOffer.Tags
            };

            _context.Offers.Add(renewedOffer);

            // Copy items
            if (originalOffer.Items != null)
            {
                var renewedItems = originalOffer.Items.Select(item => new OfferItem
                {
                    Id = $"ITEM-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}",
                    OfferId = newOfferId,
                    Type = item.Type,
                    ArticleId = item.ArticleId,
                    ItemName = item.ItemName,
                    ItemCode = item.ItemCode,
                    Description = item.Description,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    Discount = item.Discount,
                    DiscountType = item.DiscountType,
                    InstallationId = item.InstallationId,
                    InstallationName = item.InstallationName
                }).ToList();

                _context.OfferItems.AddRange(renewedItems);
            }

            await _context.SaveChangesAsync();

            var result = await GetOfferByIdAsync(newOfferId);
            return result!;
        }

        public async Task<object> ConvertOfferAsync(string id, ConvertOfferDto convertDto, string userId)
        {
            var offer = await _context.Offers.FindAsync(id);
            if (offer == null)
                throw new KeyNotFoundException($"Offer with ID {id} not found");

            offer.Status = "accepted";
            offer.ConvertedAt = DateTime.UtcNow;
            offer.UpdatedAt = DateTime.UtcNow;

            string? saleId = null;
            string? serviceOrderId = null;

            if (convertDto.ConvertToSale)
            {
                saleId = $"SALE-{DateTime.UtcNow.Ticks.ToString().Substring(10)}";
                offer.ConvertedToSaleId = saleId;
            }

            if (convertDto.ConvertToServiceOrder)
            {
                serviceOrderId = $"SO-{DateTime.UtcNow.Ticks.ToString().Substring(10)}";
                offer.ConvertedToServiceOrderId = serviceOrderId;
            }

            await _context.SaveChangesAsync();

            var updatedOffer = await GetOfferByIdAsync(id);

            return new
            {
                SaleId = saleId,
                ServiceOrderId = serviceOrderId,
                Offer = updatedOffer
            };
        }

        public async Task<OfferStatsDto> GetOfferStatsAsync(DateTime? dateFrom = null, DateTime? dateTo = null)
        {
            var query = _context.Offers.AsQueryable();

            if (dateFrom.HasValue)
                query = query.Where(o => o.CreatedAt >= dateFrom.Value);

            if (dateTo.HasValue)
                query = query.Where(o => o.CreatedAt <= dateTo.Value);

            var offers = await query.ToListAsync();

            var totalOffers = offers.Count;
            var activeOffers = offers.Count(o => o.Status == "draft" || o.Status == "sent");
            var acceptedOffers = offers.Count(o => o.Status == "accepted");
            var declinedOffers = offers.Count(o => o.Status == "declined" || o.Status == "cancelled");
            var totalValue = offers.Sum(o => o.TotalAmount ?? o.Amount);
            var averageValue = totalOffers > 0 ? totalValue / totalOffers : 0;
            var conversionRate = totalOffers > 0 ? (decimal)acceptedOffers / totalOffers * 100 : 0;

            return new OfferStatsDto
            {
                TotalOffers = totalOffers,
                ActiveOffers = activeOffers,
                AcceptedOffers = acceptedOffers,
                DeclinedOffers = declinedOffers,
                TotalValue = totalValue,
                AverageValue = averageValue,
                ConversionRate = Math.Round(conversionRate, 2),
                MonthlyGrowth = 12.8m // Mock value - implement proper calculation
            };
        }

        public async Task<OfferItemDto> AddOfferItemAsync(string offerId, CreateOfferItemDto itemDto)
        {
            var offer = await _context.Offers.FindAsync(offerId);
            if (offer == null)
                throw new KeyNotFoundException($"Offer with ID {offerId} not found");

            var item = new OfferItem
            {
                Id = $"ITEM-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}",
                OfferId = offerId,
                Type = itemDto.Type,
                ArticleId = itemDto.ArticleId,
                ItemName = itemDto.ItemName,
                ItemCode = itemDto.ItemCode,
                Description = itemDto.Description,
                Quantity = itemDto.Quantity,
                UnitPrice = itemDto.UnitPrice,
                Discount = itemDto.Discount,
                DiscountType = itemDto.DiscountType,
                InstallationId = itemDto.InstallationId,
                InstallationName = itemDto.InstallationName
            };

            _context.OfferItems.Add(item);
            await _context.SaveChangesAsync();

            // Reload to get computed total_price
            var addedItem = await _context.OfferItems.FindAsync(item.Id);
            return MapItemToDto(addedItem!);
        }

        public async Task<OfferItemDto> UpdateOfferItemAsync(string offerId, string itemId, CreateOfferItemDto itemDto)
        {
            var item = await _context.OfferItems
                .FirstOrDefaultAsync(i => i.Id == itemId && i.OfferId == offerId);

            if (item == null)
                throw new KeyNotFoundException($"Item with ID {itemId} not found in offer {offerId}");

            item.Type = itemDto.Type;
            item.ArticleId = itemDto.ArticleId;
            item.ItemName = itemDto.ItemName;
            item.ItemCode = itemDto.ItemCode;
            item.Description = itemDto.Description;
            item.Quantity = itemDto.Quantity;
            item.UnitPrice = itemDto.UnitPrice;
            item.Discount = itemDto.Discount;
            item.DiscountType = itemDto.DiscountType;
            item.InstallationId = itemDto.InstallationId;
            item.InstallationName = itemDto.InstallationName;

            await _context.SaveChangesAsync();

            // Reload to get computed values
            var updatedItem = await _context.OfferItems.FindAsync(itemId);
            return MapItemToDto(updatedItem!);
        }

        public async Task<bool> DeleteOfferItemAsync(string offerId, string itemId)
        {
            var item = await _context.OfferItems
                .FirstOrDefaultAsync(i => i.Id == itemId && i.OfferId == offerId);

            if (item == null)
                return false;

            _context.OfferItems.Remove(item);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<OfferActivityDto>> GetOfferActivitiesAsync(string offerId, string? type = null, int page = 1, int limit = 20)
        {
            var query = _context.OfferActivities
                .Where(a => a.OfferId == offerId);

            if (!string.IsNullOrEmpty(type))
                query = query.Where(a => a.Type == type);

            var activities = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync();

            return activities.Select(a => new OfferActivityDto
            {
                Id = a.Id,
                OfferId = a.OfferId,
                Type = a.Type,
                Description = a.Description,
                Details = a.Details,
                OldValue = a.OldValue,
                NewValue = a.NewValue,
                CreatedAt = a.CreatedAt,
                CreatedBy = a.CreatedBy,
                CreatedByName = a.CreatedByName
            }).ToList();
        }

        private OfferDto MapToDto(Offer offer, Contact? contact = null)
        {
            return new OfferDto
            {
                Id = offer.Id,
                Title = offer.Title,
                Description = offer.Description,
                ContactId = offer.ContactId,
                Contact = contact != null ? new ContactSummaryDto
                {
                    Id = contact.Id,
                    Name = contact.Name,
                    Company = contact.Company,
                    Email = contact.Email,
                    Phone = contact.Phone,
                    Address = contact.Address
                } : null,
                Amount = offer.Amount,
                Currency = offer.Currency,
                Taxes = offer.Taxes,
                Discount = offer.Discount,
                TotalAmount = offer.TotalAmount,
                Status = offer.Status,
                Category = offer.Category,
                Source = offer.Source,
                BillingAddress = offer.BillingAddress,
                BillingPostalCode = offer.BillingPostalCode,
                BillingCountry = offer.BillingCountry,
                DeliveryAddress = offer.DeliveryAddress,
                DeliveryPostalCode = offer.DeliveryPostalCode,
                DeliveryCountry = offer.DeliveryCountry,
                ValidUntil = offer.ValidUntil,
                AssignedTo = offer.AssignedTo,
                AssignedToName = offer.AssignedToName,
                Tags = offer.Tags,
                Items = offer.Items?.Select(MapItemToDto).ToList(),
                CreatedAt = offer.CreatedAt,
                UpdatedAt = offer.UpdatedAt,
                CreatedBy = offer.CreatedBy,
                LastActivity = offer.LastActivity,
                ConvertedToSaleId = offer.ConvertedToSaleId,
                ConvertedToServiceOrderId = offer.ConvertedToServiceOrderId,
                ConvertedAt = offer.ConvertedAt
            };
        }

        private OfferItemDto MapItemToDto(OfferItem item)
        {
            return new OfferItemDto
            {
                Id = item.Id,
                OfferId = item.OfferId,
                Type = item.Type,
                ArticleId = item.ArticleId,
                ItemName = item.ItemName,
                ItemCode = item.ItemCode,
                Description = item.Description,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalPrice = item.TotalPrice ?? 0,
                Discount = item.Discount,
                DiscountType = item.DiscountType,
                InstallationId = item.InstallationId,
                InstallationName = item.InstallationName
            };
        }
    }
}
