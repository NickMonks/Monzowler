using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Monzowler.Application.Contracts.Services;
using Monzowler.Domain.Requests;
using Monzowler.Domain.Responses;
using Monzowler.Shared.Observability;
using Monzowler.Shared.Utilities;
using OpenQA.Selenium;

namespace Monzowler.Application.Parsers;

/// <summary>
/// Parser used for JavaScript-heavy websites that require full browser rendering.
/// for example websites that uses frameworks like React, Angular (where they hide the code under <script> tags)
/// especially those that are not SEO-optimized or do not expose static HTML content.
/// </summary>
/// <param name="provider"></param>
public class RenderedHtmlParser(IBrowserProvider provider, ILogger<RenderedHtmlParser> logger) : ISubParser
{
    public async Task<ParserResponse> ParseLinksAsync(ParserRequest request, CancellationToken ct)
    {
        logger.LogInformation("Start parsing links - {ParserName}", nameof(RenderedHtmlParser));
        using var span = TracingHelper.Source.StartActivity(nameof(RenderedHtmlParser));

        var html = await GetRenderedHtmlAsync(request.HtmlResult, ct);
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var links = doc.DocumentNode.SelectNodes("//a[@href]")
            ?.Select(a => a.GetAttributeValue("href", string.Empty))
            .Select(href => Sanitizer.SanitizeUrl(href, request.Url))
            .Where(u => u is not null && new Uri(u).Host == request.AllowedHost)
            .Distinct()
            .ToList() ?? [];

        return new ParserResponse
        {
            Links = links
        };
    }


    private async Task<string> GetRenderedHtmlAsync(string html, CancellationToken cancellationToken)
    {
        var driver = provider.GetDriver();

        // about:blank is an empty browser page - we then write our raw htlm from the client
        // to the document generated, using the DOM API
        driver.Navigate().GoToUrl("about:blank");
        ((IJavaScriptExecutor)driver).ExecuteScript("document.write(arguments[0]);", html);

        await WaitUntilDomReadyAsync(driver);

        return driver.PageSource;
    }

    /// <summary>
    /// Waits until the DOM is loaded - it simply adds some forced delay until the
    /// DOM is in ready state to be either completed or interactive. We limit this to be
    /// A couple of seconds maximum 
    /// </summary>
    /// <param name="driver"></param>
    private static async Task WaitUntilDomReadyAsync(IWebDriver driver)
    {
        var js = (IJavaScriptExecutor)driver;
        var maxRetries = 5;
        var ready = false;

        for (int i = 0; i < maxRetries && !ready; i++)
        {
            try
            {
                var state = js.ExecuteScript("return document.readyState")?.ToString();
                ready = state == "interactive" || state == "complete";
            }
            catch
            {
                // might be transient, retry
            }

            if (!ready)
                await Task.Delay(300);
        }
    }
}