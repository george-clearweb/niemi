using Niemi.Models.DTOs;

namespace Niemi.Services;

public interface IOrdrRadService
{
    Task<string> GetOrdrRadStructureAsync();
    Task<IEnumerable<KeywordCategoryDto>> GetKeywordCategoriesAsync();
}
