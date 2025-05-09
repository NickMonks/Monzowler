using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace Monzowler.Crawler.Parsers;

public class BrowserProvider : IDisposable
{
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