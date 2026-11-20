using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Shandis.Infrastructure.Identity;

namespace Shandis.Api.Endpoints;

public static class AuthenticationEndpoints
{
    public static IEndpointRouteBuilder MapShandisAuthenticationEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(
                "/api/accounts/login",
                LoginWithIdentifierAsync)
            .AllowAnonymous()
            .WithName("LoginWithEmailOrUsername");

        return endpoints;
    }

    private static async Task<IResult> LoginWithIdentifierAsync(
        LoginWithIdentifierRequest request,
        UserManager<ShandisUser> userManager,
        SignInManager<ShandisUser> signInManager)
    {
        var identifier = request.Identifier?.Trim();

        if (string.IsNullOrWhiteSpace(identifier) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.BadRequest(new
            {
                message = "Email/username and password are required."
            });
        }

        var user = await userManager.FindByEmailAsync(identifier)
            ?? await userManager.FindByNameAsync(identifier);

        if (user is null || !user.IsActive)
        {
            return Results.Problem(
                detail: "Failed",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        // Use Identity's opaque bearer-token handler, but pass the resolved
        // user so authentication works with either email or username.
        signInManager.AuthenticationScheme = IdentityConstants.BearerScheme;

        var result = await signInManager.PasswordSignInAsync(
            user,
            request.Password,
            isPersistent: false,
            lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            return Results.Problem(
                detail: "Failed",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        return TypedResults.Empty;
    }
}

public sealed record LoginWithIdentifierRequest(
    string? Identifier,
    string? Password);
