﻿namespace RealityScraper.Infrastructure.Configuration;

public class SmtpOptions
{
	public string Server { get; set; }

	public int Port { get; set; }

	public string Username { get; set; }

	public string Password { get; set; }

	public bool EnableSsl { get; set; }

	public string FromAddress { get; set; }

	public string FromName { get; set; }
}