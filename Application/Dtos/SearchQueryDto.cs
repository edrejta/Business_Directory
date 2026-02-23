namespace BusinessDirectory.Application.Dtos;

public sealed class SearchQueryDto
{
    public string? Keyword { get; set; }
    public string? Category { get; set; }
    public string? Location { get; set; }
    public int Limit { get; set; } = 20;
    public int Page { get; set; } = 1;
    public bool OnlyWithCoordinates { get; set; }
    public string? Bbox { get; set; }
    public double? Lat { get; set; }
    public double? Lng { get; set; }
    public double? RadiusKm { get; set; }
    public string? SortBy { get; set; }
}
