using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApi.DTOs;
using MyApi.Services;

namespace MyApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ArticlesController : ControllerBase
    {
        private readonly IArticleService _articleService;
        private readonly ILogger<ArticlesController> _logger;

        public ArticlesController(IArticleService articleService, ILogger<ArticlesController> logger)
        {
            _articleService = articleService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<ArticleListResponseDto>> GetAll([FromQuery] ArticleSearchRequestDto? searchRequest = null)
        {
            try
            {
                var result = await _articleService.GetAllArticlesAsync(searchRequest);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting articles");
                return StatusCode(500, "An error occurred while retrieving articles");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ArticleResponseDto>> GetById(Guid id)
        {
            try
            {
                var item = await _articleService.GetArticleByIdAsync(id);
                if (item == null) return NotFound();
                return Ok(item);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting article {ArticleId}", id);
                return StatusCode(500, "An error occurred while retrieving the article");
            }
        }

        [HttpPost]
        public async Task<ActionResult<ArticleResponseDto>> Create([FromBody] CreateArticleRequestDto createDto)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);
                var created = await _articleService.CreateArticleAsync(createDto);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Validation error while creating article");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating article");
                return StatusCode(500, "An error occurred while creating the article");
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ArticleResponseDto>> Update(Guid id, [FromBody] UpdateArticleRequestDto updateDto)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);
                var updated = await _articleService.UpdateArticleAsync(id, updateDto);
                if (updated == null) return NotFound();
                return Ok(updated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating article {ArticleId}", id);
                return StatusCode(500, "An error occurred while updating the article");
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(Guid id)
        {
            try
            {
                var ok = await _articleService.DeleteArticleAsync(id);
                if (!ok) return NotFound();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting article {ArticleId}", id);
                return StatusCode(500, "An error occurred while deleting the article");
            }
        }
    }
}