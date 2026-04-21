namespace JWTAuthAPI.Models
{
    public class AdminGlobalSearchDto
    {
        public string Query { get; set; } = string.Empty;
        public int TotalResults { get; set; }
        public List<GlobalSearchResultItemDto> Results { get; set; } = new();
    }

    public class GlobalSearchResultItemDto
    {
        public string Type { get; set; } = string.Empty;
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Subtitle { get; set; }
        public string? Status { get; set; }
        public string? NavigateTo { get; set; }
    }
}
