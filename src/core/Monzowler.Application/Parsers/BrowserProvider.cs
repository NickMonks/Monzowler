using Monzowler.Application.Contracts.Services;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace Monzowler.Application.Parsers;

/// <summary>
/// Represents a browser provider that manages a Selenium Chrome WebDriver instance.
/// A WebDriver is an API that allows you to control a real web browser programmatically,
/// making it useful for JavaScript-heavy websites where static HTML isn't sufficient.
///
/// This implementation supports both local development and Docker-based environments:
/// - In Docker (e.g., ARM with Chromium), it uses environment variables to locate the
///   Chromium binary and ChromeDriver explicitly, bypassing Selenium Manager. We set them
///   inside the docker-compose.yaml
/// - Locally, it falls back to default behavior if paths are not specified,
///   allowing auto-resolution via Selenium Manager.
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

            // Allow override via environment variables - we need to do this 
            // to run docker with selenium!
            var chromeBinary = Environment.GetEnvironmentVariable("CHROME_BINARY");
            var driverPath = Environment.GetEnvironmentVariable("CHROMEDRIVER_PATH");

            options.AddArgument("--headless=new");
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--disable-gpu");

            if (!string.IsNullOrWhiteSpace(chromeBinary))
            {
                options.BinaryLocation = chromeBinary;
            }

            if (!string.IsNullOrWhiteSpace(driverPath))
            {
                var service = ChromeDriverService.CreateDefaultService(driverPath);
                service.SuppressInitialDiagnosticInformation = true;
                service.EnableVerboseLogging = false;

                return new ChromeDriver(service, options);
            }

            // Fallback to use default options (local)
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