﻿namespace RealityScraper.Persistence.Seeding.Configuration;

public class TaskConfiguration
{
	public string Name { get; set; }

	public string CronExpression { get; set; }

	public bool Enabled { get; set; } = true;

	public ScrapingConfiguration ScrapingConfiguration { get; set; }
}