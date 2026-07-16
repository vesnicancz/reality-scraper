namespace RealityScraper.Web.Api.Configuration;

public sealed class OidcAuthenticationOptions
{
	public const string SectionName = "Authentication";

	public bool Enabled { get; set; }

	/// <summary>
	/// Vědomé povolení běhu bez autentizace i v produkci. Bez tohoto flagu aplikace
	/// v produkčním prostředí odmítne nastartovat, pokud je autentizace vypnutá.
	/// </summary>
	public bool AllowAnonymous { get; set; }
	public string Authority { get; set; } = string.Empty;
	public string ClientId { get; set; } = string.Empty;
	public string ClientSecret { get; set; } = string.Empty;
	public string[] Scopes { get; set; } = ["openid", "profile", "email"];
	public bool RequireHttpsMetadata { get; set; } = true;
	public string CallbackPath { get; set; } = "/signin-oidc";
	public string SignedOutCallbackPath { get; set; } = "/signout-callback-oidc";
}