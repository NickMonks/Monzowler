using Microsoft.Playwright;

namespace Monzowler.Crawler.Parsers;

public class JsHtmlParser {
    public async Task<string> GetRenderedHtmlAsync(string url) {
        using var playwright = await Playwright.CreateAsync();

        await using var browser = await playwright.Chromium.LaunchAsync(new()
        {
            Headless = true,
        });
        
        // open a new page within the current browser context
        var page = await browser.NewPageAsync();

        // visit the target page
        await page.GotoAsync(url);

        // retrieve the source HTML code of the page
        // and print it
        return await page.ContentAsync();
    }

    public async Task<List<string>> ParseLinksAsync(string pageUrl, string allowedHost) {
        try {
            var html = await GetRenderedHtmlAsync(pageUrl);
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);
            return doc.DocumentNode.SelectNodes("//a[@href]")
                ?.Select(a => a.GetAttributeValue("href", string.Empty))
                .Select(href => {
                    try {
                        var candidate = new Uri(new Uri(pageUrl), href).ToString().TrimEnd('/');
                        return candidate;
                    } catch {
                        return null;
                    }
                })
                .Where(u => u is not null && new Uri(u).Host == allowedHost)
                .Distinct()
                .ToList() ?? new List<string>();
        } catch {
            // fallback: return empty list
            return new List<string>();
        }
    }
}