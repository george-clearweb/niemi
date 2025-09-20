namespace Niemi.Models.DTOs;

public class KeywordCategoryDto
{
    public string Category { get; set; } = string.Empty;
    public List<KeywordEntryDto> Entries { get; set; } = new List<KeywordEntryDto>();
}

public class KeywordEntryDto
{
    public int Id { get; set; }
    public string Keyword { get; set; } = string.Empty;
}
