using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApi.Modules.Offers.DTOs;
using MyApi.Modules.Offers.Services;
using System.Security.Claims;

namespace MyApi.Modules.Offers.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/v1/offers")]
    public class OffersController : ControllerBase
    {
        private readonly IOfferService _offerService;
        private readonly ILogger<OffersController> _logger;

        public OffersController(IOfferService offerService, ILogger<OffersController> logger)
        {
            _offerService = offerService;
            _logger = logger;
        }

        private string GetCurrentUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
        }

        /// <summary>
        /// Get all offers with optional filters
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetOffers(
            [FromQuery] string? status = null,
            [FromQuery] string? category = null,
            [FromQuery] string? source = null,
            [FromQuery] string? contact_id = null,
            [FromQuery] DateTime? date_from = null,
            [FromQuery] DateTime? date_to = null,
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 20,
            [FromQuery] string sort_by = "updated_at",
            [FromQuery] string sort_order = "desc"
        )
        {
            try
            {
                var result = await _offerService.GetOffersAsync(
                    status, category, source, contact_id,
                    date_from, date_to, search,
                    page, limit, sort_by, sort_order
                );

                return Ok(new
                {
                    success = true,
                    data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching offers");
                return StatusCode(500, new
                {
                    success = false,
                    error = new
                    {
                        code = "INTERNAL_ERROR",
                        message = "An error occurred while fetching offers"
                    }
                });
            }
        }

        /// <summary>
        /// Get offer statistics
        /// </summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats(
            [FromQuery] DateTime? date_from = null,
            [FromQuery] DateTime? date_to = null
        )
        {
            try
            {
                var stats = await _offerService.GetOfferStatsAsync(date_from, date_to);
                return Ok(new
                {
                    success = true,
                    data = stats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching offer statistics");
                return StatusCode(500, new
                {
                    success = false,
                    error = new
                    {
                        code = "INTERNAL_ERROR",
                        message = "An error occurred while fetching statistics"
                    }
                });
            }
        }

        /// <summary>
        /// Get single offer by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOfferById(string id)
        {
            try
            {
                var offer = await _offerService.GetOfferByIdAsync(id);
                if (offer == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        error = new
                        {
                            code = "OFFER_NOT_FOUND",
                            message = "Offer not found"
                        }
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = offer
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching offer {OfferId}", id);
                return StatusCode(500, new
                {
                    success = false,
                    error = new
                    {
                        code = "INTERNAL_ERROR",
                        message = "An error occurred while fetching the offer"
                    }
                });
            }
        }

        /// <summary>
        /// Create a new offer
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateOffer([FromBody] CreateOfferDto createDto)
        {
            try
            {
                var userId = GetCurrentUserId();
                var offer = await _offerService.CreateOfferAsync(createDto, userId);

                return CreatedAtAction(nameof(GetOfferById), new { id = offer.Id }, new
                {
                    success = true,
                    data = offer
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating offer");
                return StatusCode(500, new
                {
                    success = false,
                    error = new
                    {
                        code = "INTERNAL_ERROR",
                        message = "An error occurred while creating the offer"
                    }
                });
            }
        }

        /// <summary>
        /// Update an offer
        /// </summary>
        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateOffer(string id, [FromBody] UpdateOfferDto updateDto)
        {
            try
            {
                var userId = GetCurrentUserId();
                var offer = await _offerService.UpdateOfferAsync(id, updateDto, userId);

                return Ok(new
                {
                    success = true,
                    data = offer
                });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new
                {
                    success = false,
                    error = new
                    {
                        code = "OFFER_NOT_FOUND",
                        message = "Offer not found"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating offer {OfferId}", id);
                return StatusCode(500, new
                {
                    success = false,
                    error = new
                    {
                        code = "INTERNAL_ERROR",
                        message = "An error occurred while updating the offer"
                    }
                });
            }
        }

        /// <summary>
        /// Delete an offer
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOffer(string id)
        {
            try
            {
                var result = await _offerService.DeleteOfferAsync(id);
                if (!result)
                {
                    return NotFound(new
                    {
                        success = false,
                        error = new
                        {
                            code = "OFFER_NOT_FOUND",
                            message = "Offer not found"
                        }
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Offer deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting offer {OfferId}", id);
                return StatusCode(500, new
                {
                    success = false,
                    error = new
                    {
                        code = "INTERNAL_ERROR",
                        message = "An error occurred while deleting the offer"
                    }
                });
            }
        }

        /// <summary>
        /// Renew an offer (create a duplicate)
        /// </summary>
        [HttpPost("{id}/renew")]
        public async Task<IActionResult> RenewOffer(string id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var renewedOffer = await _offerService.RenewOfferAsync(id, userId);

                return CreatedAtAction(nameof(GetOfferById), new { id = renewedOffer.Id }, new
                {
                    success = true,
                    data = renewedOffer
                });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new
                {
                    success = false,
                    error = new
                    {
                        code = "OFFER_NOT_FOUND",
                        message = "Offer not found"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error renewing offer {OfferId}", id);
                return StatusCode(500, new
                {
                    success = false,
                    error = new
                    {
                        code = "INTERNAL_ERROR",
                        message = "An error occurred while renewing the offer"
                    }
                });
            }
        }

        /// <summary>
        /// Convert offer to sale/service order
        /// </summary>
        [HttpPost("{id}/convert")]
        public async Task<IActionResult> ConvertOffer(string id, [FromBody] ConvertOfferDto convertDto)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _offerService.ConvertOfferAsync(id, convertDto, userId);

                return Ok(new
                {
                    success = true,
                    data = result
                });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new
                {
                    success = false,
                    error = new
                    {
                        code = "OFFER_NOT_FOUND",
                        message = "Offer not found"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting offer {OfferId}", id);
                return StatusCode(500, new
                {
                    success = false,
                    error = new
                    {
                        code = "INTERNAL_ERROR",
                        message = "An error occurred while converting the offer"
                    }
                });
            }
        }

        /// <summary>
        /// Get offer activities
        /// </summary>
        [HttpGet("{id}/activities")]
        public async Task<IActionResult> GetOfferActivities(
            string id,
            [FromQuery] string? type = null,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 20
        )
        {
            try
            {
                var activities = await _offerService.GetOfferActivitiesAsync(id, type, page, limit);
                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        activities,
                        pagination = new
                        {
                            page,
                            limit,
                            total = activities.Count
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching activities for offer {OfferId}", id);
                return StatusCode(500, new
                {
                    success = false,
                    error = new
                    {
                        code = "INTERNAL_ERROR",
                        message = "An error occurred while fetching activities"
                    }
                });
            }
        }

        /// <summary>
        /// Add item to offer
        /// </summary>
        [HttpPost("{id}/items")]
        public async Task<IActionResult> AddOfferItem(string id, [FromBody] CreateOfferItemDto itemDto)
        {
            try
            {
                var item = await _offerService.AddOfferItemAsync(id, itemDto);
                return CreatedAtAction(nameof(GetOfferById), new { id }, new
                {
                    success = true,
                    data = item
                });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new
                {
                    success = false,
                    error = new
                    {
                        code = "OFFER_NOT_FOUND",
                        message = "Offer not found"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding item to offer {OfferId}", id);
                return StatusCode(500, new
                {
                    success = false,
                    error = new
                    {
                        code = "INTERNAL_ERROR",
                        message = "An error occurred while adding the item"
                    }
                });
            }
        }

        /// <summary>
        /// Update offer item
        /// </summary>
        [HttpPatch("{id}/items/{itemId}")]
        public async Task<IActionResult> UpdateOfferItem(string id, string itemId, [FromBody] CreateOfferItemDto itemDto)
        {
            try
            {
                var item = await _offerService.UpdateOfferItemAsync(id, itemId, itemDto);
                return Ok(new
                {
                    success = true,
                    data = item
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    success = false,
                    error = new
                    {
                        code = "ITEM_NOT_FOUND",
                        message = ex.Message
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating item {ItemId} in offer {OfferId}", itemId, id);
                return StatusCode(500, new
                {
                    success = false,
                    error = new
                    {
                        code = "INTERNAL_ERROR",
                        message = "An error occurred while updating the item"
                    }
                });
            }
        }

        /// <summary>
        /// Delete offer item
        /// </summary>
        [HttpDelete("{id}/items/{itemId}")]
        public async Task<IActionResult> DeleteOfferItem(string id, string itemId)
        {
            try
            {
                var result = await _offerService.DeleteOfferItemAsync(id, itemId);
                if (!result)
                {
                    return NotFound(new
                    {
                        success = false,
                        error = new
                        {
                            code = "ITEM_NOT_FOUND",
                            message = "Item not found"
                        }
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Item deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting item {ItemId} from offer {OfferId}", itemId, id);
                return StatusCode(500, new
                {
                    success = false,
                    error = new
                    {
                        code = "INTERNAL_ERROR",
                        message = "An error occurred while deleting the item"
                    }
                });
            }
        }
    }
}
