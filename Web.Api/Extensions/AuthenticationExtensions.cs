using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using RealityScraper.Web.Api.Configuration;

namespace RealityScraper.Web.Api.Extensions;

internal static class AuthenticationExtensions
{
	public static IServiceCollection AddOidcAuthentication(
		this IServiceCollection services,
		IConfiguration configuration,
		IWebHostEnvironment environment)
	{
		var authOptions = configuration
			.GetSection(OidcAuthenticationOptions.SectionName)
			.Get<OidcAuthenticationOptions>();

		if (authOptions is not { Enabled: true })
		{
			return services;
		}

		if (string.IsNullOrWhiteSpace(authOptions.Authority))
		{
			throw new InvalidOperationException("Authentication:Authority must be configured when authentication is enabled.");
		}

		if (string.IsNullOrWhiteSpace(authOptions.ClientId))
		{
			throw new InvalidOperationException("Authentication:ClientId must be configured when authentication is enabled.");
		}

		if (authOptions.Scopes is not { Length: > 0 } || !authOptions.Scopes.Contains("openid"))
		{
			throw new InvalidOperationException("Authentication:Scopes must contain 'openid' when authentication is enabled.");
		}

		services.Configure<ForwardedHeadersOptions>(options =>
		{
			options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

			// Bez známé proxy by middleware důvěřoval X-Forwarded-* od libovolného klienta.
			// IP reverse proxy lze zadat v ForwardedHeaders:KnownProxies; bez konfigurace
			// se akceptuje jen jedna úroveň proxy (ForwardLimit).
			options.ForwardLimit = 1;
			var knownProxies = configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>();
			if (knownProxies is { Length: > 0 })
			{
				options.KnownIPNetworks.Clear();
				options.KnownProxies.Clear();
				foreach (var proxy in knownProxies)
				{
					options.KnownProxies.Add(System.Net.IPAddress.Parse(proxy));
				}
			}
			else
			{
				options.KnownIPNetworks.Clear();
				options.KnownProxies.Clear();
			}
		});

		services
			.AddAuthentication(options =>
			{
				options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
				options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
			})
			.AddCookie(options =>
			{
				options.Cookie.HttpOnly = true;
				options.Cookie.SecurePolicy = environment.IsDevelopment()
					? CookieSecurePolicy.SameAsRequest
					: CookieSecurePolicy.Always;
				options.Cookie.SameSite = SameSiteMode.Lax;
				options.ExpireTimeSpan = TimeSpan.FromHours(8);
				options.SlidingExpiration = true;
			})
			.AddOpenIdConnect(options =>
			{
				options.Authority = authOptions.Authority;
				options.ClientId = authOptions.ClientId;
				options.ClientSecret = authOptions.ClientSecret;
				options.ResponseType = OpenIdConnectResponseType.Code;
				options.GetClaimsFromUserInfoEndpoint = true;
				options.RequireHttpsMetadata = authOptions.RequireHttpsMetadata;
				options.CallbackPath = authOptions.CallbackPath;
				options.SignedOutCallbackPath = authOptions.SignedOutCallbackPath;

				options.Scope.Clear();
				foreach (var scope in authOptions.Scopes)
				{
					options.Scope.Add(scope);
				}

				options.MapInboundClaims = false;
				options.TokenValidationParameters.NameClaimType = "name";
				options.TokenValidationParameters.RoleClaimType = "role";
			});

		return services;
	}

	public static bool IsAuthenticationEnabled(this IConfiguration configuration)
	{
		return configuration.GetValue<bool>($"{OidcAuthenticationOptions.SectionName}:Enabled");
	}

	public static WebApplication UseOidcAuthentication(this WebApplication app)
	{
		if (!app.Configuration.IsAuthenticationEnabled())
		{
			return app;
		}

		app.UseAuthentication();
		app.UseAuthorization();

		return app;
	}

	public static WebApplication MapAuthenticationEndpoints(this WebApplication app)
	{
		if (!app.Configuration.IsAuthenticationEnabled())
		{
			return app;
		}

		app.MapGet("/account/login", (string? returnUrl) =>
		{
			var redirectUri = "/";
			if (!string.IsNullOrEmpty(returnUrl)
				&& Uri.TryCreate(returnUrl, UriKind.Relative, out _)
				&& returnUrl.StartsWith('/') && !returnUrl.StartsWith("//") && !returnUrl.StartsWith("/\\"))
			{
				redirectUri = returnUrl;
			}

			return Results.Challenge(new AuthenticationProperties { RedirectUri = redirectUri }, [OpenIdConnectDefaults.AuthenticationScheme]);
		}).AllowAnonymous();

		app.MapPost("/account/logout", () =>
			Results.SignOut(
				new AuthenticationProperties { RedirectUri = "/" },
				[CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]))
			.AllowAnonymous();

		app.MapGet("/account/user-info", (HttpContext context) =>
		{
			if (context.User.Identity?.IsAuthenticated != true)
			{
				return Results.Unauthorized();
			}

			return Results.Ok(new
			{
				Name = context.User.Identity.Name,
				Email = context.User.FindFirst("email")?.Value
			});
		});

		return app;
	}
}