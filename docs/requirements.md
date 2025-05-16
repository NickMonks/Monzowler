# Functional Requirements
- Given a starting URL, the crawler should visit every URL it discovers within the same domain.
- For each visited page, the crawler should:
   - Print the page URL. 
   - Output the list of links found on that page. 
- The crawler should restrict itself to a single domain, e.g., https://monzo.com/, and must not follow external links (such as `facebook.com` or `community.monzo.com`).

# Non-Functional Requirements (as defined by the task)
- Low Latency: The crawler should be performant and efficient in processing web pages.
- Scalable: The design should support scaling to larger sites with many pages.
- Fault Tolerance: The crawler should handle unexpected failures gracefully (e.g., broken links, timeouts).
- Politeness: The crawler should respect the `/robots.txt` directives to avoid overwhelming other domains or violating disallow rules.

# Beyond the Original Task
While the assignment described a relatively simple crawler, I deliberately designed the solution with production-grade principles in mind.
These additions aren't strictly required by the task but represent how I would approach building a scalable system for real-world usage.
- Asynchronous Job Processing: Crawling operations are handled as background jobs, decoupled from API responses, ensuring responsiveness even for large sites.
- Concurrency & Efficiency in mind by relying on multithreading and parallelism for processing multiple URLs concurrently.
- Observability: Added some observability (structured logs, tracing) to monitor crawl performance and status — similar to a real-world microservice. 
- Persistence & State Management: Crawler progress and results are persisted in DynamoDB, which allows to enable job tracking, retries, and sitemap retrieval, even after failures.
- Dockerized: The solution is fully containerized, ensuring a consistent and portable environment across platforms. 
- CLI & API Entrypoints: Provided both a CLI interface and an API layer, giving flexibility in how the crawler is invoked.

