using Microsoft.Playwright;

namespace Monzowler.Crawler.Parsers;

public class HeadlessParser(BrowserProvider provider) : ISubParser
{
    public async Task<string> GetRenderedHtmlAsync(string url)
    {
        var browser = await provider.GetBrowserAsync();
        var page = await browser.NewPageAsync();

        await page.GotoAsync(url, new PageGotoOptions { Timeout = 10000 });
        var content = await page.ContentAsync();
        await page.CloseAsync(); // don't leak pages!

        return content;
    }

    public async Task<List<string>> ParseLinksAsync(string pageUrl, string allowedHost, CancellationToken ct)
    {
        var html = await GetRenderedHtmlAsync(pageUrl);
        var doc = new HtmlAgilityPack.HtmlDocument();
        doc.LoadHtml(html);

        return doc.DocumentNode.SelectNodes("//a[@href]")
            ?.Select(a => a.GetAttributeValue("href", string.Empty))
            .Select(href => {
                try {
                    if (string.IsNullOrWhiteSpace(href) || href.StartsWith("#")) return null;

                    var candidateUri = new Uri(new Uri(pageUrl), href);
                        
                    //only crawl http/https links - filter other types of links like mailto:, javascript:, etc
                    if (candidateUri.Scheme != Uri.UriSchemeHttp && candidateUri.Scheme != Uri.UriSchemeHttps)
                        return null;
                    
                    //We don't want to crawl or parse documents like pdf and other formats - I set a few of them, but they could be many!
                    var path = candidateUri.AbsolutePath.ToLowerInvariant();
                    var excludedExtensions = new[] {
                        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".zip", ".rar", ".jpg", ".png", ".gif", ".mp4", ".mp3", ".m4a"
                    };

                    if (excludedExtensions.Any(ext => path.EndsWith(ext)))
                        return null;

                    return candidateUri.ToString().TrimEnd('/');
                } catch {
                    return null;
                }
            })
            .Where(u => u is not null && new Uri(u).Host == allowedHost)
            .Distinct()
            .ToList() ?? new List<string>();
    }
}
