namespace RealityScraper.Application.Interfaces.Scraping;

/// <summary>
/// Ověřuje, že cílová URL míří na veřejnou adresu přes http/https. Chrání před SSRF -
/// scrapované cílové URL i URL obrázků pocházejí z uživatelského/cizího vstupu a nesmí
/// směřovat do interní sítě (loopback, RFC1918, link-local vč. cloud metadata apod.).
/// </summary>
public interface IUrlSafetyValidator
{
	Task<bool> IsPublicHttpTargetAsync(Uri uri, CancellationToken cancellationToken);
}
