using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Claims;

namespace Tracker.Dotnet.Libs.Authorization;

public static class DependencyExtensions
{
    public static IServiceCollection AddJwtTrackerAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtConfiguration = configuration.GetSection("Jwt").Get<JwtConfiguration>();

        if (jwtConfiguration == null)
        {
            throw new ArgumentNullException("Jwt");
        }

        if (jwtConfiguration.Authority == null)
        {
            throw new ArgumentNullException(nameof(jwtConfiguration.Authority));
        }

        if (jwtConfiguration.Audience == null)
        {
            throw new ArgumentNullException(nameof(jwtConfiguration.Audience));
        }

        services.AddHttpContextAccessor();
        services.Configure<JwtConfiguration>(configuration.GetSection("Jwt"));
        services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false; // only for dev

            options.TokenValidationParameters.NameClaimType = "username";  // username is in this claim
            options.TokenValidationParameters.RoleClaimType = ClaimTypes.Role; // roles are in this claim
            options.TokenValidationParameters.ValidIssuer = jwtConfiguration.Authority;
            options.TokenValidationParameters.ValidAudience = jwtConfiguration.Audience;
            options.TokenValidationParameters.ValidateAudience = true;
            options.TokenValidationParameters.RequireSignedTokens = false; // dev
            options.TokenValidationParameters.ValidateIssuerSigningKey = false; // dev

            options.TokenValidationParameters.SignatureValidator = (token, parameters) =>
            {
                // validate just existing of the signature because here is used self signed certificate
                return new JsonWebToken(token);
            };

            options.IncludeErrorDetails = true;
        });

        return services;
    }
}
