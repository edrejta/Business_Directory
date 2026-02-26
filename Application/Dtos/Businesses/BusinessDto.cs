using System.Text.Json.Serialization;
using BusinessDirectory.Domain.Enums;

namespace BusinessDirectory.Application.Dtos.Businesses;

public sealed class BusinessDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("ownerId")]
    public Guid OwnerId { get; set; }

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

    [JsonPropertyName("status")]
    public BusinessStatus Status { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("openDays")]
    public string? OpenDays { get; set; }

    [JsonPropertyName("suspensionReason")]
    public string? SuspensionReason { get; set; }

    [JsonPropertyName("isFavorite")]
    public bool IsFavorite { get; set; }

    [JsonPropertyName("businessName")]
    public string BusinessName
    {
        get => Name;
        set => Name = value ?? string.Empty;
    }

    [JsonPropertyName("businessType")]
    public BusinessType BusinessType { get; set; } = BusinessType.Unknown;

    [JsonPropertyName("websiteUrl")]
    public string? WebsiteUrl
    {
        get => BusinessUrl;
        set => BusinessUrl = value;
    }
}