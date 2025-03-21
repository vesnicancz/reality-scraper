using RealityScraper.Mailing;
using RealityScraper.Scraping.Model;

namespace RealityScraper.Tests.Mailing;

public class RazorEmailGeneratorTests
{
	//[Fact]
	public async Task EmailReportDebug()
	{
		// Arrange
		var generator = new RazorEmailGenerator();
		var model = new ScrapingReport
		{
			ReportDate = DateTime.Now,
			Results = new List<ScraperResult>
			{
				new ScraperResult
				{
					SiteName = "SReality",
					TotalListingCount = 5,
					NewListings = new List<ListingItem>
					{
						new ListingItem
						{
							Title = "Prodej rodinného domu 155 m², pozemek 1140 m²",
							Price = 6_750_000,
							Location = "Kejžlice",
							Url = "https://www.sreality.cz/detail/prodej/dum/rodinny/kejzlice-kejzlice-/3253998156",
							ImageUrl = "https://d18-a.sdn.cz/d_18/c_img_oW_C/nO1SBfAluiDshGGBCOCbPzsW/a70b.png?fl=res,800,600,3|shr,,20|webp,60",
							ExternalId = "3253998156"
						},
						new ListingItem
						{
							Title = "Prodej chaty 248 m², pozemek 3233 m²",
							Price = 3_990_000,
							Location = "Horní Radouň",
							Url = "https://www.sreality.cz/detail/prodej/dum/chata/horni-radoun-horni-radoun-/3021877836",
							ImageUrl ="https://d18-a.sdn.cz/d_18/c_img_m4_D/nPt0vAiT3tBIuqIm7B6CPh5/545a.jpeg?fl=res,800,600,3|shr,,20|webp,60",
							ExternalId = "3021877836"
						},
						new ListingItem
						{
							Title = "Prodej rodinného domu 350 m², pozemek 816 m²",
							Price = 8_490_000,
							Location = "Humpolec - Rozkoš",
							Url = "https://www.sreality.cz/detail/prodej/dum/rodinny/humpolec-rozkos-/488313420",
							ImageUrl = "https://d18-a.sdn.cz/d_18/c_img_oW_A/nO1SBfAluiyaoUfxCQHFt6/4942.png?fl=res,800,600,3|shr,,20|webp,60",
							ExternalId = "488313420"
						}
					},
					PriceChangedListings = new List<ListingItemWithNewPrice>
					{
						new ListingItemWithNewPrice
						{
							Title = "Prodej rodinného domu 90 m², pozemek 181 m²",
							Price = 5_500_000,
							OldPrice = 5_600_000,
							Location = "Herálec - Kamenice",
							Url = "https://www.sreality.cz/detail/prodej/dum/rodinny/heralec-kamenice-/67851596",
							ImageUrl = "https://d18-a.sdn.cz/d_18/c_img_m2_A/kaUOXOcprDy3auaIwgtU/3481.jpeg?fl=res,800,600,3|shr,,20|webp,60",
							ExternalId = "67851596"
						},
						new ListingItemWithNewPrice
						{
							Title = "Prodej rodinného domu 186 m², pozemek 1068 m²",
							Price = 10_500_000,
							OldPrice = 10_400_000,
							Location = "Vystrkov",
							Url = "https://www.sreality.cz/detail/prodej/dum/rodinny/vystrkov-vystrkov-/496734796",
							ImageUrl = "https://d18-a.sdn.cz/d_18/c_img_oX_A/kPWmeC9soBB5dBZg4ChC5Go/fcaa.jpeg?fl=res,800,600,3|shr,,20|webp,60",
							ExternalId = "496734796"
						},
						new ListingItemWithNewPrice
						{
							Title = "Prodej rodinného domu 207 m², pozemek 942 m²",
							Price = 5_390_000,
							OldPrice = null,
							Location = "Nádražní, Pacov",
							Url = "https://www.sreality.cz/detail/prodej/dum/rodinny/pacov-pacov-nadrazni/1560085068",
							ImageUrl = "https://d18-a.sdn.cz/d_18/c_img_oX_C/kb9Yg2GWeBykzchmCoh2sb/4888.jpeg?fl=res,800,600,3|shr,,20|webp,60",
							ExternalId = "1560085068"
						},
						new ListingItemWithNewPrice
						{
							Title = "SLEVA: Prodej rodinného domu 85 m², pozemek 789 m²",
							Price = null,
							OldPrice = 4_390_000,
							Location = "Štítného, Kamenice nad Lipou",
							Url = "https://www.sreality.cz/detail/prodej/dum/rodinny/kamenice-nad-lipou-kamenice-nad-lipou-stitneho/2195489356",
							ImageUrl = "https://d18-a.sdn.cz/d_18/c_img_oX_C/nPw2pRkMOeBQpVfb2CpHPR5/2b82.jpeg?fl=res,800,600,3|shr,,20|webp,60",
							ExternalId = "2195489356"
						}
					}
				},
				new ScraperResult
				{
					SiteName = "Reality iDNES",
					TotalListingCount = 3,
					NewListings = new List<ListingItem>
					{
						new ListingItem
						{
							Title = "Prodej chaty/chalupy 123 m² s pozemkem 2 130 m²",
							Price = 4_690_000,
							Location = "Důl",
							Url = "https://reality.idnes.cz/detail/prodej/dum/dul/67a35fbf709c746e0005ae80/",
							ImageUrl = "https://sta-reality2.1gr.cz/sta/compile/thumbs/c/7/c/a73c8ff33eedacfaf9aea737854b3.jpg",
							ExternalId = "67a35fbf709c746e0005ae80"
						},
						new ListingItem
						{
							Title = "Prodej domu 550 m² s pozemkem 1 699 m²",
							Price = 5_490_000,
							Location = "Moraveč",
							Url = "https://reality.idnes.cz/detail/prodej/dum/moravec/67112b48e9dfe5207305182b/",
							ImageUrl = "https://sta-reality2.1gr.cz/sta/compile/thumbs/8/2/7/ed0822060f1dc366843d268b3fbb3.jpg",
							ExternalId = "67112b48e9dfe5207305182b"
						}
					},
					PriceChangedListings = new List<ListingItemWithNewPrice>
					{
						new ListingItemWithNewPrice
						{
							Title = "Prodej domu 85 m² s pozemkem 789 m²",
							Price = 4_390_000,
							OldPrice = 4_490_000,
							Location = "Štítného, Kamenice nad Lipou",
							Url = "https://reality.idnes.cz/detail/prodej/dum/kamenice-nad-lipou-stitneho/66e43e9fef5d5228c8015b6b/",
							ImageUrl =  "https://sta-reality2.1gr.cz/sta/compile/thumbs/9/a/5/ebdd43dbef68ee82759a58f8961de.jpg",
							ExternalId = "66e43e9fef5d5228c8015b6b"
						},
						new ListingItemWithNewPrice
						{
							Title = "Prodej domu 144 m² s pozemkem 1 467 m²",
							Price = 4_990_000,
							OldPrice = 4_999_000,
							Location = "Kamenice nad Lipou - Nová Ves",
							Url = "https://reality.idnes.cz/detail/prodej/dum/kamenice-nad-lipou/66ffa8e3b077e967cc0d2f34/",
							ImageUrl = "https://sta-reality2.1gr.cz/sta/compile/thumbs/d/b/c/034f08f14239024c38b7def1160bc.jpg",
							ExternalId = "66ffa8e3b077e967cc0d2f34"
						}
					}
				},
				new ScraperResult
				{
					SiteName = "Reality.cz",
					TotalListingCount = 2
				}
			}
		};

		// Act
		var result = await generator.GenerateHtmlBodyAsync(model);

		// Assert
		Assert.NotNull(result);

		// write to file
		await File.WriteAllTextAsync("d:\\temp\\report.html", result);
	}
}