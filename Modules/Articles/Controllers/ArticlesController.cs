using System.Threading.Tasks;
using System.Security.Claims;
using MyApi.Modules.Articles.DTOs;
using MyApi.Modules.Articles.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace MyApi.Modules.Articles.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/v1/articles")]
    public class ArticlesController : ControllerBase
    {
        private readonly IArticleService _articleService;
        private readonly ILogger<ArticlesController> _logger;

        public ArticlesController(IArticleService articleService, ILogger<ArticlesController> logger)
        {
            _articleService = articleService;
            _logger = logger;
        }

        private string GetCurrentUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
        }

        // =====================================================
        // Article Endpoints
        // =====================================================

        /// <summary>
        /// Get all articles with optional filtering and pagination
        /// </summary>
        /// <param name="type">Filter by type (material/service)</param>
        /// <param name="category">Filter by category</param>
        /// <param name="status">Filter by status</param>
        /// <param name="location">Filter by location</param>
        /// <param name="search">Search term for name, SKU, or category</param>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="limit">Items per page (default: 20)</param>
        /// <param name="sortBy">Sort field</param>
        /// <param name="sortOrder">Sort order (asc/desc)</param>
        [HttpGet]
        public async Task<ActionResult<ArticleListDto>> GetAllArticles(
            [FromQuery] string? type = null,
            [FromQuery] string? category = null,
            [FromQuery] string? status = null,
            [FromQuery] string? location = null,
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 20,
            [FromQuery] string? sortBy = null,
            [FromQuery] string? sortOrder = "asc")
        {
            var result = await _articleService.GetAllArticlesAsync(
                type, category, status, location, search, page, limit, sortBy, sortOrder);
            
            return Ok(result);
        }

        /// <summary>
        /// Get article by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ArticleDto>> GetArticleById(string id)
        {
            var article = await _articleService.GetArticleByIdAsync(id);
            
            if (article == null)
                return NotFound(new { message = "Article not found" });
            
            return Ok(article);
        }

        /// <summary>
        /// Create a new article
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ArticleDto>> CreateArticle([FromBody] CreateArticleDto dto)
        {
            try
            {
                var userId = GetCurrentUserId();
                var article = await _articleService.CreateArticleAsync(dto, userId);
                
                return CreatedAtAction(nameof(GetArticleById), new { id = article.Id }, new
                {
                    success = true,
                    data = article
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating article");
                return StatusCode(500, new
                {
                    success = false,
                    error = new
                    {
                        code = "INTERNAL_ERROR",
                        message = "An error occurred while creating the article"
                    }
                });
            }
        }

        /// <summary>
        /// Update an existing article
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<ArticleDto>> UpdateArticle(string id, [FromBody] UpdateArticleDto dto)
        {
            try
            {
                var userId = GetCurrentUserId();
                var article = await _articleService.UpdateArticleAsync(id, dto, userId);
                
                if (article == null)
                    return NotFound(new
                    {
                        success = false,
                        error = new
                        {
                            code = "ARTICLE_NOT_FOUND",
                            message = "Article not found"
                        }
                    });
                
                return Ok(new
                {
                    success = true,
                    data = article
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating article {ArticleId}", id);
                return StatusCode(500, new
                {
                    success = false,
                    error = new
                    {
                        code = "INTERNAL_ERROR",
                        message = "An error occurred while updating the article"
                    }
                });
            }
        }

        /// <summary>
        /// Delete an article
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteArticle(string id)
        {
            var deleted = await _articleService.DeleteArticleAsync(id);
            
            if (!deleted)
                return NotFound(new { message = "Article not found" });
            
            return Ok(new { success = true, message = "Article deleted successfully", id });
        }

        // =====================================================
        // Inventory Transaction Endpoints
        // =====================================================

        /// <summary>
        /// Create a new inventory transaction
        /// </summary>
        [HttpPost("transactions")]
        public async Task<ActionResult<InventoryTransactionDto>> CreateTransaction([FromBody] CreateInventoryTransactionDto dto)
        {
            try
            {
                var userId = GetCurrentUserId();
                var transaction = await _articleService.CreateTransactionAsync(dto, userId);
                
                return CreatedAtAction(nameof(GetArticleTransactions), new { articleId = dto.ArticleId }, new
                {
                    success = true,
                    data = transaction
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating inventory transaction");
                return StatusCode(500, new
                {
                    success = false,
                    error = new
                    {
                        code = "INTERNAL_ERROR",
                        message = "An error occurred while creating the transaction"
                    }
                });
            }
        }

        /// <summary>
        /// Get all transactions for a specific article
        /// </summary>
        [HttpGet("{articleId}/transactions")]
        public async Task<ActionResult> GetArticleTransactions(string articleId)
        {
            var transactions = await _articleService.GetArticleTransactionsAsync(articleId);
            
            return Ok(transactions);
        }

        // =====================================================
        // Batch Operations
        // =====================================================

        /// <summary>
        /// Batch update stock levels
        /// </summary>
        [HttpPost("batch")]
        public async Task<ActionResult<BatchOperationResultDto>> BatchUpdateStock([FromBody] BatchUpdateStockDto dto)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _articleService.BatchUpdateStockAsync(dto, userId);
                
                return Ok(new
                {
                    success = true,
                    data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error batch updating stock");
                return StatusCode(500, new
                {
                    success = false,
                    error = new
                    {
                        code = "INTERNAL_ERROR",
                        message = "An error occurred while batch updating stock"
                    }
                });
            }
        }

        // =====================================================
        // Category Endpoints
        // =====================================================

        /// <summary>
        /// Get all categories
        /// </summary>
        [HttpGet("categories")]
        public async Task<ActionResult> GetAllCategories()
        {
            var categories = await _articleService.GetAllCategoriesAsync();
            
            return Ok(categories);
        }

        /// <summary>
        /// Create a new category
        /// </summary>
        [HttpPost("categories")]
        public async Task<ActionResult<ArticleCategoryDto>> CreateCategory([FromBody] CreateArticleCategoryDto dto)
        {
            var category = await _articleService.CreateCategoryAsync(dto);
            
            return CreatedAtAction(nameof(GetAllCategories), new { id = category.Id }, category);
        }

        // =====================================================
        // Location Endpoints
        // =====================================================

        /// <summary>
        /// Get all locations
        /// </summary>
        [HttpGet("locations")]
        public async Task<ActionResult> GetAllLocations()
        {
            var locations = await _articleService.GetAllLocationsAsync();
            
            return Ok(locations);
        }

        /// <summary>
        /// Create a new location
        /// </summary>
        [HttpPost("locations")]
        public async Task<ActionResult<LocationDto>> CreateLocation([FromBody] CreateLocationDto dto)
        {
            var location = await _articleService.CreateLocationAsync(dto);
            
            return CreatedAtAction(nameof(GetAllLocations), new { id = location.Id }, location);
        }
    }
}
