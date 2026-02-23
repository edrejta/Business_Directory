namespace BusinessDirectory.Application.Options;

public sealed class EmailVerificationSettings
{
    public const string SectionName = "EmailVerification";

    public bool RequireVerifiedEmailForLogin { get; set; } = true;
    public string VerificationBaseUrl { get; set; } = "http://localhost:3000/verify-email";
    public int TokenExpiryHours { get; set; } = 24;
}
