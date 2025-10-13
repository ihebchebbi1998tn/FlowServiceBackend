using MyApi.Modules.Articles.DTOs;

namespace MyApi.Modules.Articles.Services
{
    public interface IArticleService
    {
        Task<ArticleListResponseDto> GetAllArticlesAsync(ArticleSearchRequestDto? searchRequest = null);
        Task<ArticleResponseDto?> GetArticleByIdAsync(Guid id);
        Task<ArticleResponseDto> CreateArticleAsync(CreateArticleRequestDto createDto);
        Task<ArticleResponseDto?> UpdateArticleAsync(Guid id, UpdateArticleRequestDto updateDto);
        Task<bool> DeleteArticleAsync(Guid id);
    }
}