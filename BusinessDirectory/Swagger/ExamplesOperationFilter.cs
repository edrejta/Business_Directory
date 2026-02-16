using BusinessDirectory.Application.Dtos;
using BusinessDirectory.Domain.Enums;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BusinessDirectory.Swagger;

public sealed class ExamplesOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var path = context.ApiDescription.RelativePath ?? string.Empty;
        var method = context.ApiDescription.HttpMethod ?? string.Empty;

        if (method.Equals("POST", StringComparison.OrdinalIgnoreCase) && path.Contains("auth/register"))
        {
            SetRequestExample(operation, new OpenApiObject
            {
                ["username"] = new OpenApiString("john_doe"),
                ["email"] = new OpenApiString("john@example.com"),
                ["password"] = new OpenApiString("password123")
            });

            SetResponseExample(operation, StatusCodes.Status201Created, new OpenApiObject
            {
                ["token"] = new OpenApiString("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."),
                ["id"] = new OpenApiString(Guid.Empty.ToString()),
                ["username"] = new OpenApiString("john_doe"),
                ["email"] = new OpenApiString("john@example.com"),
                ["role"] = new OpenApiString(UserRole.User.ToString())
            });
        }

        if (method.Equals("POST", StringComparison.OrdinalIgnoreCase) && path.Contains("auth/login"))
        {
            SetRequestExample(operation, new OpenApiObject
            {
                ["email"] = new OpenApiString("john@example.com"),
                ["password"] = new OpenApiString("password123")
            });

            SetResponseExample(operation, StatusCodes.Status200OK, new OpenApiObject
            {
                ["token"] = new OpenApiString("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."),
                ["id"] = new OpenApiString(Guid.Empty.ToString()),
                ["username"] = new OpenApiString("john_doe"),
                ["email"] = new OpenApiString("john@example.com"),
                ["role"] = new OpenApiString(UserRole.User.ToString())
            });
        }

        if (method.Equals("POST", StringComparison.OrdinalIgnoreCase) && path.StartsWith("businesses"))
        {
            SetRequestExample(operation, new OpenApiObject
            {
                ["businessName"] = new OpenApiString("Sample Cafe"),
                ["address"] = new OpenApiString("123 Main St"),
                ["city"] = new OpenApiString("Prishtina"),
                ["email"] = new OpenApiString("contact@samplecafe.com"),
                ["phoneNumber"] = new OpenApiString("+38344123456"),
                ["businessType"] = new OpenApiInteger((int)BusinessType.Cafe),
                ["description"] = new OpenApiString("Cozy cafe with great coffee."),
                ["imageUrl"] = new OpenApiString("https://example.com/images/cafe.jpg")
            });

            SetResponseExample(operation, StatusCodes.Status201Created, new OpenApiObject
            {
                ["id"] = new OpenApiString(Guid.Empty.ToString()),
                ["ownerId"] = new OpenApiString(Guid.Empty.ToString()),
                ["businessName"] = new OpenApiString("Sample Cafe"),
                ["address"] = new OpenApiString("123 Main St"),
                ["city"] = new OpenApiString("Prishtina"),
                ["email"] = new OpenApiString("contact@samplecafe.com"),
                ["phoneNumber"] = new OpenApiString("+38344123456"),
                ["businessType"] = new OpenApiInteger((int)BusinessType.Cafe),
                ["description"] = new OpenApiString("Cozy cafe with great coffee."),
                ["imageUrl"] = new OpenApiString("https://example.com/images/cafe.jpg"),
                ["status"] = new OpenApiString(BusinessStatus.Pending.ToString()),
                ["createdAt"] = new OpenApiDateTime(DateTime.UtcNow)
            });
        }
    }

    private static void SetRequestExample(OpenApiOperation operation, IOpenApiAny example)
    {
        if (operation.RequestBody?.Content is null)
            return;

        if (operation.RequestBody.Content.TryGetValue("application/json", out var content))
        {
            content.Example = example;
        }
    }

    private static void SetResponseExample(OpenApiOperation operation, int statusCode, IOpenApiAny example)
    {
        var key = statusCode.ToString();
        if (!operation.Responses.TryGetValue(key, out var response))
            return;

        if (response.Content.TryGetValue("application/json", out var content))
        {
            content.Example = example;
        }
    }
}
