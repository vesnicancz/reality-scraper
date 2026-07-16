using RealityScraper.Application.Abstractions.Messaging;

namespace RealityScraper.Application.Features.ScraperTasks.RunNow;

// Pracuje nad TaskBase přes ITaskRepository, funguje tedy pro libovolný typ tasku
// (scraper i report), ne jen scraper.
public record RunTaskNowCommand(Guid Id) : ICommand;