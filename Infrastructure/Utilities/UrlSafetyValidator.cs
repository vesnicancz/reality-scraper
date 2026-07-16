using System.Net;
using System.Net.Sockets;
using RealityScraper.Application.Interfaces.Scraping;

namespace RealityScraper.Infrastructure.Utilities;

/// <summary>
/// Povoluje jen http/https cíle resolvované na veřejné adresy - brání odchozím
/// požadavkům do interní sítě (SSRF) přes uživatelem zadanou cílovou URL nebo
/// podvržený atribut obrázku na scrapované stránce.
/// </summary>
public class UrlSafetyValidator : IUrlSafetyValidator
{
	public async Task<bool> IsPublicHttpTargetAsync(Uri uri, CancellationToken cancellationToken)
	{
		if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
		{
			return false;
		}

		IPAddress[] addresses;
		try
		{
			addresses = await Dns.GetHostAddressesAsync(uri.DnsSafeHost, cancellationToken);
		}
		catch (SocketException)
		{
			return false;
		}

		return addresses.Length > 0 && addresses.All(IsPublicAddress);
	}

	internal static bool IsPublicAddress(IPAddress address)
	{
		if (address.IsIPv4MappedToIPv6)
		{
			address = address.MapToIPv4();
		}

		if (IPAddress.IsLoopback(address) || address.IsIPv6LinkLocal || address.IsIPv6UniqueLocal || address.IsIPv6Multicast)
		{
			return false;
		}

		if (address.AddressFamily == AddressFamily.InterNetwork)
		{
			var bytes = address.GetAddressBytes();
			return bytes[0] switch
			{
				0 => false,
				10 => false,
				100 when bytes[1] >= 64 && bytes[1] <= 127 => false, // CGNAT 100.64.0.0/10
				127 => false,
				169 when bytes[1] == 254 => false, // link-local vč. cloud metadata 169.254.169.254
				172 when bytes[1] >= 16 && bytes[1] <= 31 => false,
				192 when bytes[1] == 168 => false,
				_ => true
			};
		}

		return true;
	}
}
