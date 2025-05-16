using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Monzowler.Api;
using Monzowler.Application.Contracts.Services;
using Monzowler.Shared.Settings;

var rootCommand = new RootCommand("Monzowler CLI Crawler");
var urlArg = new Argument<string>("url", "The root URL to crawl");
var jobIdOpt = new Option<string>("--jobId", () => Guid.NewGuid().ToString(), "Optional job ID");

rootCommand.AddArgument(urlArg);
rootCommand.AddOption(jobIdOpt);

rootCommand.SetHandler(async (url, jobId) =>
{
    var exeDir = AppContext.BaseDirectory;

    var host = Host.CreateDefaultBuilder(args)
        .UseContentRoot(exeDir)
        .ConfigureServices((context, services) =>
        {
            services.AddLogging();
            services.Configure<CrawlerSettings>(context.Configuration.GetSection("Crawler"));
            services.AddStartupServices(context.Configuration);
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            services.AddLogging(config => config.AddConsole());
        })
        .Build();

    var spider = host.Services.GetRequiredService<ISpiderService>();
    await spider.CrawlAsync(url, jobId);
},urlArg, jobIdOpt);

return await rootCommand.InvokeAsync(args);