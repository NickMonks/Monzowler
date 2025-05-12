using Monzowler.Application.Contracts.Services;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace Monzowler.Application.Services;

/// <summary>
/// Represents a browser provider that manages a Selenium Chrome WebDriver instance.
/// A WebDriver is an API that allows you to control a real web browser programmatically,
/// making it useful for JavaScript-heavy websites where static HTML isn't sufficient.
/// 
/// The WebDriver is created lazily to avoid the high cost of launching a browser
/// unless it's explicitly needed.
/// </summary>
public class BrowserProvider : IBrowserProvider, IDisposable
{
    // Lazily initialize the WebDriver to avoid creating a browser instance
    // unless it's actually needed, as this operation is resource-intensive.
    private readonly Lazy<IWebDriver> _driver;

    public BrowserProvider()
    {
        _driver = new(() =>
        {
            var options = new ChromeOptions();
            options.AddArgument("--headless=new");
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--disable-gpu");

            return new ChromeDriver(options);
        });
    }

    public IWebDriver GetDriver() => _driver.Value;

    public void Dispose()
    {
        if (_driver.IsValueCreated)
        {
            _driver.Value.Quit();
            _driver.Value.Dispose();
        }
    }
}