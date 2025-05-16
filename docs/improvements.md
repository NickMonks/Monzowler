Here are some key areas where the current crawler could be enhanced further, with some potential ideas for future improvements:

## Handling Updated URLs
Currently, the crawler avoids reprocessing URLs that have already been visited. However, content on a URL might change over time.
A future improvement could involve computing a content hash of crawled pages, enabling detection of changes and allowing re-crawling when needed.

# Resiliency & Fault Tolerance
If the crawler crashes mid-process, the current implementation would lose the partially crawled sitemap.
To enhance resiliency we should:
- have intermediate sitemap results should be persisted incrementally. 
- This would allow resuming jobs without starting from scratch.

# BFS vs. DFS Approach
The core crawling logic currently follows a Breadth-First Search (BFS)-like behavior.
While concurrency makes this not strictly BFS, the approach prioritizes exploring links closer to the root domain — which aligns with typical crawl priorities.
For different use cases (e.g., prioritizing deep exploration), a Depth-First Search (DFS) strategy could be considered.

# Real-time Job Updates
Currently, clients must poll the API to check job status. In real production system short-polling might not be the best option. 
Other alternatives: Implementing long polling, Server-Sent Events (SSE) for real-time updates.
This reduces unnecessary load from frequent polling, though should be balanced to avoid over-engineering for small-scale use.

# Efficiency & Caching
Each crawl job currently re-fetches all URLs, which is inefficient.
A distributed cache (e.g., Redis) could prevent redundant crawls of unchanged URLs.
However, we need to be careful for account for:
- Freshly discovered links on revisited pages.
- Expiry policies to balance freshness vs. performance.
An practical solution is to hash the string response and check if we already have been visited, but this is out of scope for the MVP. 

# Database Improvements
Potential enhancements for DynamoDB persistence:
- Support paginated responses when querying large sitemaps. Currently if you crawl a very large website, good luck getting the response!
- As mentioned above too, Persist partial crawl results mid-process for robustness and resume capability.

# Extending Parsers
Beyond HTML, the parser architecture could be extended to handle other content types like PDFs, DOCX, or even structured JSON feeds.

# Infrastructure & Scalability
While the current solution is designed to be production-quality code in mind, further infrastructure improvements would include:
- Deploying the crawler as a serverless Lambda function for example.
- Using SQS for decoupled, scalable crawl job processing (as we are using AWS).
- Introducing CI/CD pipelines for build, test, deploy automation.

# CI/CD Pipeline Enhancements
To streamline the development workflow we should do a few things in our project:
- Enforce branch protections, PR checks, unit tests, linting, security scans through CI (e.g. Github Actions, CircleCI)
- Automate releases with GitHub Actions, possibly integrating with:
   - ArgoCD for deployment orchestration. 
   - Spacelift for infrastructure provisioning (Terraform).

# Observability Improvements
While basic observability is in place (via Jaeger and OTLP exporters):
- Span coverage could be improved for more insightful tracing.
- Refactor cross-cutting concerns like logging & tracing to improve code readability maintainability - now is a bit messy. 