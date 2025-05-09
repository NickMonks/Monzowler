using Microsoft.Playwright;

namespace Monzowler.Crawler.Parsers;

/// <summary>
/// Playwright CreateAsync spins up a new Node.js instance each time.
/// To improve performance we create a browser provider for this only once
/// We use lazy defer to avoid instantiate it if we don't ever use it. 
/// </summary>
public class BrowserProvider : IAsyncDisposable
{
    private readonly Lazy<Task<IPlaywright>> _playwright;
    private readonly Lazy<Task<IBrowser>> _browser;

    public BrowserProvider()
    {
        _playwright = new(() => Playwright.CreateAsync());
        _browser = new(async () =>
        {
            var playwright = await _playwright.Value;
            return await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true
            });
        });
    }

    public async Task<IBrowser> GetBrowserAsync() => await _browser.Value;

    public async ValueTask DisposeAsync()
    {
        if (_browser.IsValueCreated)
            await (await _browser.Value).CloseAsync();

        if (_playwright.IsValueCreated)
            (await _playwright.Value).Dispose();
    }
}