using System.Net;
using RealityScraper.Infrastructure.Utilities;

namespace RealityScraper.Infrastructure.Tests.Utilities;

public class UrlSafetyValidatorTests
{
	[Theory]
	// veřejné adresy
	[InlineData("8.8.8.8", true)]
	[InlineData("1.1.1.1", true)]
	[InlineData("172.15.0.1", true)]   // těsně pod RFC1918 172.16/12
	[InlineData("172.32.0.1", true)]   // těsně nad RFC1918 172.16/12
	[InlineData("100.63.255.255", true)] // těsně pod CGNAT 100.64/10
	[InlineData("100.128.0.0", true)]  // těsně nad CGNAT 100.64/10
	// loopback
	[InlineData("127.0.0.1", false)]
	[InlineData("127.255.255.255", false)]
	// "this host" 0.0.0.0/8
	[InlineData("0.0.0.0", false)]
	// RFC1918 privátní rozsahy
	[InlineData("10.0.0.1", false)]
	[InlineData("172.16.0.1", false)]
	[InlineData("172.31.255.255", false)]
	[InlineData("192.168.1.1", false)]
	// CGNAT 100.64.0.0/10
	[InlineData("100.64.0.1", false)]
	[InlineData("100.127.255.255", false)]
	// link-local vč. cloud metadata
	[InlineData("169.254.0.1", false)]
	[InlineData("169.254.169.254", false)]
	// IPv6
	[InlineData("::1", false)]                      // loopback
	[InlineData("fe80::1", false)]                  // link-local
	[InlineData("fc00::1", false)]                  // unique local
	[InlineData("fd00::1", false)]                  // unique local
	[InlineData("2606:4700:4700::1111", true)]      // veřejná IPv6
	// IPv4 mapované do IPv6
	[InlineData("::ffff:127.0.0.1", false)]
	[InlineData("::ffff:10.0.0.1", false)]
	[InlineData("::ffff:8.8.8.8", true)]
	public void IsPublicAddress_ClassifiesAddress(string address, bool expected)
	{
		var ip = IPAddress.Parse(address);

		var result = UrlSafetyValidator.IsPublicAddress(ip);

		Assert.Equal(expected, result);
	}

	[Theory]
	[InlineData("ftp://example.com/file")]
	[InlineData("file:///etc/passwd")]
	[InlineData("gopher://example.com")]
	public async Task IsPublicHttpTargetAsync_RejectsNonHttpSchemes(string url)
	{
		var validator = new UrlSafetyValidator();

		var result = await validator.IsPublicHttpTargetAsync(new Uri(url), CancellationToken.None);

		Assert.False(result);
	}

	[Fact]
	public async Task IsPublicHttpTargetAsync_RejectsLoopbackHost()
	{
		var validator = new UrlSafetyValidator();

		var result = await validator.IsPublicHttpTargetAsync(new Uri("http://127.0.0.1/latest/meta-data"), CancellationToken.None);

		Assert.False(result);
	}

	[Fact]
	public async Task IsPublicHttpTargetAsync_RejectsCloudMetadataAddress()
	{
		var validator = new UrlSafetyValidator();

		var result = await validator.IsPublicHttpTargetAsync(new Uri("http://169.254.169.254/latest/meta-data"), CancellationToken.None);

		Assert.False(result);
	}

	[Fact]
	public async Task IsPublicHttpTargetAsync_RejectsUnresolvableHost()
	{
		var validator = new UrlSafetyValidator();

		var result = await validator.IsPublicHttpTargetAsync(
			new Uri("http://nonexistent.invalid.host.example.local/"), CancellationToken.None);

		Assert.False(result);
	}
}
