using OpenQA.Selenium;

namespace Monzowler.Application.Contracts.Services;

public interface IBrowserProvider
{
    IWebDriver GetDriver();
}