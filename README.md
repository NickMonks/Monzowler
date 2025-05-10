# Monzowler

A fast, performant webcrawler for Monzo Task Challenge!

# Architecture

## Concurrency Model

## Parsers

## Infrastructure 
- Why do we use two parsers? Optimisation: HtmlAgilityPack is significantly faster than Playwright, since the former requires launching a headless browser, run JS, load assets, etc. If we really don't need to use it we will likely avoid using it. 
- Concurrency: we set up a semaphore for controlled concurrency but if we want to go a bit deeper we can set up workers model for concurrency. 

## How to Run

### Option 1 (Recommended)

Run the `docker-compose.yml` 

## Check if data exist:
- Modify the appsettings.json from `localstack:4566` to `localhost:4566`. 
- Install AWS CLI
- To scan the whole table:
```curl
aws --endpoint-url=http://localhost:4566 --region=us-east-1 dynamodb scan -
-table-name Crawler-Sitemap
```

# Testing

## Unit Test

TBD

## Integration Test

TBD

## System Test 

TBD

# Improvements & Future development

- If we had to extend this to multiple domains, then using Task.Delay() for each previous request is innefficient and wrong. A potential update would be to use a throttler that work as a Map for each domain, and it identifies the last time a request was done to that domain. If it's less than the delay time we will Task.Delay() 
- We also might want to crawl urls that have been already been visited but changed the payload - we could create a hash and do this
- What happens if the crawler crashes mid-way? we would like to be resilient and store each sitemap at least. 
- Robot.txt can have more directives - we had the most basic and others a bit more complicated, but allow could be added for example