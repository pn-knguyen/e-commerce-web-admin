using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace e_commerce_web_admin.Services.CustomerMessages;

public static class CustomerMessageAuthenticationDefaults
{
    public const string Scheme = "CustomerMessageBearer";
    public const string CustomerIdClaim = "techstore:customer_id";
    public const string ScopeClaim = "scope";
    public const string AccessScope = "customer_messages";
    public const string AiReceiptScope = "customer_messages.ai_receipt";
    public const string QuestionHashClaim = "question_hash";
    public const string ReplyHashClaim = "reply_hash";
    public const string MetadataHashClaim = "metadata_hash";
    public const string AiProviderClaim = "ai_provider";
    public const string AiModelClaim = "ai_model";
}

public sealed class CustomerMessageJwtOptions
{
    public const string SectionName = "CustomerMessages:Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string AccessAudience { get; set; } = string.Empty;
    public string AiReceiptAudience { get; set; } = string.Empty;
    public string SigningKey { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 60;
    public int AiReceiptMinutes { get; set; } = 5;
}

public sealed record CustomerAiReceiptValidationResult(
    bool Succeeded,
    string? ReceiptId,
    string? AiProvider,
    string? AiModel,
    string? Error)
{
    public static CustomerAiReceiptValidationResult Success(
        string receiptId,
        string? aiProvider,
        string? aiModel) =>
        new(true, receiptId, aiProvider, aiModel, null);

    public static CustomerAiReceiptValidationResult Failed(string error) =>
        new(false, null, null, null, error);
}

public interface ICustomerAiReceiptValidator
{
    CustomerAiReceiptValidationResult Validate(
        string? receipt,
        long customerId,
        string question,
        string reply,
        string? metadataJson);
}

public sealed class CustomerAiReceiptValidator(
    IOptions<CustomerMessageJwtOptions> options) : ICustomerAiReceiptValidator
{
    private readonly CustomerMessageJwtOptions _options = options.Value;
    private readonly JwtSecurityTokenHandler _handler = new();

    public CustomerAiReceiptValidationResult Validate(
        string? receipt,
        long customerId,
        string question,
        string reply,
        string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(receipt))
        {
            return CustomerAiReceiptValidationResult.Failed("Thiếu chứng thực phản hồi AI.");
        }

        try
        {
            var principal = _handler.ValidateToken(
                receipt,
                BuildValidationParameters(),
                out _);

            if (!HasClaim(principal, CustomerMessageAuthenticationDefaults.ScopeClaim,
                    CustomerMessageAuthenticationDefaults.AiReceiptScope) ||
                !TryGetCustomerId(principal, out var receiptCustomerId) ||
                receiptCustomerId != customerId)
            {
                return CustomerAiReceiptValidationResult.Failed("Chứng thực phản hồi AI không hợp lệ.");
            }

            if (!HashMatches(principal, CustomerMessageAuthenticationDefaults.QuestionHashClaim, question) ||
                !HashMatches(principal, CustomerMessageAuthenticationDefaults.ReplyHashClaim, reply) ||
                !HashMatches(principal, CustomerMessageAuthenticationDefaults.MetadataHashClaim, metadataJson ?? string.Empty))
            {
                return CustomerAiReceiptValidationResult.Failed("Nội dung phản hồi AI không khớp chứng thực.");
            }

            var receiptId = principal.FindFirstValue(JwtRegisteredClaimNames.Jti);
            return string.IsNullOrWhiteSpace(receiptId)
                ? CustomerAiReceiptValidationResult.Failed("Chứng thực phản hồi AI thiếu mã định danh.")
                : CustomerAiReceiptValidationResult.Success(
                    receiptId,
                    principal.FindFirstValue(CustomerMessageAuthenticationDefaults.AiProviderClaim),
                    principal.FindFirstValue(CustomerMessageAuthenticationDefaults.AiModelClaim));
        }
        catch (SecurityTokenException)
        {
            return CustomerAiReceiptValidationResult.Failed("Chứng thực phản hồi AI đã hết hạn hoặc không hợp lệ.");
        }
        catch (ArgumentException)
        {
            return CustomerAiReceiptValidationResult.Failed("Chứng thực phản hồi AI không hợp lệ.");
        }
    }

    private TokenValidationParameters BuildValidationParameters() => new()
    {
        ValidateIssuer = true,
        ValidIssuer = _options.Issuer,
        ValidateAudience = true,
        ValidAudience = _options.AiReceiptAudience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30),
    };

    private static bool TryGetCustomerId(ClaimsPrincipal principal, out long customerId) =>
        long.TryParse(
            principal.FindFirstValue(CustomerMessageAuthenticationDefaults.CustomerIdClaim),
            out customerId);

    private static bool HasClaim(ClaimsPrincipal principal, string type, string value) =>
        principal.Claims.Any(claim =>
            claim.Type == type &&
            string.Equals(claim.Value, value, StringComparison.Ordinal));

    private static bool HashMatches(ClaimsPrincipal principal, string claimType, string value)
    {
        var expected = principal.FindFirstValue(claimType);
        if (string.IsNullOrWhiteSpace(expected))
        {
            return false;
        }

        var actualBytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        byte[] expectedBytes;
        try
        {
            expectedBytes = Base64UrlEncoder.DecodeBytes(expected);
        }
        catch (FormatException)
        {
            return false;
        }

        return actualBytes.Length == expectedBytes.Length &&
            CryptographicOperations.FixedTimeEquals(actualBytes, expectedBytes);
    }
}
