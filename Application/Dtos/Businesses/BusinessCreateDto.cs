using System.Text.Json.Serialization;
using BusinessDirectory.Domain.Enums;

namespace BusinessDirectory.Application.Dtos.Businesses;

public sealed class BusinessCreateDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("city")]
    public string City { get; set; } = string.Empty;

    [JsonPropertyName("address")]
    public string? Address { get; set; }

    [JsonPropertyName("businessUrl")]
    public string? BusinessUrl { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("phoneNumber")]
    public string? PhoneNumber { get; set; }

    [JsonPropertyName("imageUrl")]
    public string? ImageUrl { get; set; }

    [JsonPropertyName("businessNumber")]
    public string BusinessNumber { get; set; } = string.Empty;

    [JsonPropertyName("businessName")]
    public string BusinessName
    {
        get => Name;
        set => Name = value ?? string.Empty;
    }

    [JsonPropertyName("businessType")]
    public BusinessType? BusinessType { get; set; }

    [JsonPropertyName("websiteUrl")]
    public string? WebsiteUrl
    {
        get => BusinessUrl;
        set => BusinessUrl = value;
    }
}