using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Monzowler.Api;
using Monzowler.Application.Contracts.Services;
using Monzowler.Domain.Entities;
using Monzowler.Shared.Settings;

var rootCommand = new RootCommand("Monzowler CLI Crawler");

var urlArg = new Argument<string>(
    name: "url",
    description: "The root URL to crawl"
);

var maxDepthOpt = new Option<int>(
    aliases: new[] { "--maxDepth", "-d" },
    description: "Maximum depth of the crawl from the root URL"
);

var maxRetriesOpt = new Option<int>(
    aliases: new[] { "--maxRetries", "-r" },
    description: "Maximum number of retries for a single URL"
);

rootCommand.AddArgument(urlArg);
rootCommand.AddOption(maxDepthOpt);
rootCommand.AddOption(maxRetriesOpt);

rootCommand.SetHandler(async (string url, int maxDepth, int maxRetries) =>
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
    await spider.CrawlAsync(new CrawlParameters
    {
        RootUrl = url,
        MaxDepth = maxDepth,
        MaxRetries = maxRetries,
        JobId = Guid.NewGuid().ToString()
    });
}, urlArg, maxDepthOpt, maxRetriesOpt);

return await rootCommand.InvokeAsync(args);