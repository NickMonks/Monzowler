# 🕸️🏦 Monzowler

A fast, performant webcrawler for the Monzo Task Challenge! 

# How to Run

## Option 1 (Recommended)

Run the `docker-compose.yml` file using the following command:

```curl
docker compose up -d 
```

To use the API endpoints specificed below, open `locahost:5002/swagger` endpoint and give it a try. Alternatively you can 
use the `Monzowler.Api.http` file or choose your favourite API Platform!

## Option 2 - run locally (Debug)

TBD

# 🧪 API Overview

The **Monzowler Crawler API** is an HTTP-based service for submitting, tracking, and retrieving the results of web crawl jobs. It supports asynchronous job management and sitemap retrieval, backed by persistent storage.

## 🔗 Base URL
`http://localhost:5002`

## 📌 API Endpoints

### `POST /crawl`

Submit a new crawl job.

#### Request Body

```json
{
  "url": "https://example.com"
}
```

#### Response - 202 Accepted
```json
{
"jobId": "abc123",
"status": "Created"
}
```

#### GET /crawl/{jobId}
Retrieve the status and metadata of a specific crawl job.

*Path Parameters*
- jobId — The ID returned when the crawl was submitted.

*Example Response*
```json
{
"jobId": "abc123",
"url": "https://monzo.com",
"status": "InProgresss",
"startedAt": "2024-04-20T14:32:00Z",
"completedAt": "null"
}
```
*Errors*
- `404 Not Found`: If the job ID does not exist.
- `500 Internal Server Error`: On unexpected errors.

#### GET /crawl/sitemap/{jobId}
Returns the list of pages crawled for a given job.

*Query Parameters (Optional)*
- `status`: Filter results by status code(s). You can pass multiple values.

Example Request:
```curl
GET /crawl/sitemap/abc123?status=Ok&status=Timeout
```

Example Response:
```json
[
  {
  "pageUrl": "https://example.com",
  "status": "Ok",
  "depth": 0
  },
  {
  "pageUrl": "https://example.com/about",
  "status": "Timeout",
  "depth": 1
  }
]
```

*Response Codes*
- `200 OK`: List of crawled pages.
- `204 No Content`: No matching pages found.
- `500 Internal Server Error`: On failure to fetch data.

# Architecture

## C4 Context Diagram

```mermaid
C4Context
  %% Monzowler C4 Context Diagram

  Person(user, "Developer/User", "Triggers crawl jobs via the API")
  
  System(monzowler, "Monzowler API", "Orchestrates and manages crawl jobs")
  System(localstack, "DynamoDB", "Stores job metadata and crawled pages")
  System(jaeger, "Jaeger", "Provides distributed tracing and observability")

  System_Ext(websites, "External Websites", "Targets of the web crawler")
  
  Rel(user, monzowler, "Submits crawl jobs to")
  Rel(monzowler, websites, "Crawls links from")
  Rel(monzowler, localstack, "Stores results and job data in")
  Rel(monzowler, jaeger, "Emits tracing data to")

  UpdateLayoutConfig($c4ShapeInRow="3", $c4BoundaryInRow="1")
  UpdateRelStyle(user, monzowler, $offsetX="-30", $offsetY="30")
  UpdateRelStyle(monzowler, websites, $offsetX="30", $offsetY="-40")
  UpdateRelStyle(monzowler, localstack, $offsetX="30", $offsetY="30")
  UpdateRelStyle(monzowler, jaeger, $offsetX="-30", $offsetY="-30")
```

## Code Architecture

## Politeness & `Robots.txt`

Something I discovered a while ago is that politeness is paramount for most of webcrawlers - we don't want to overload domains to look like they are having DDoS attacks or simply not degrage their performance.
That's why websites have the `/robots.txt` file with certain rules to follow on what websites are good / not good to crawl, subdomains that are not disallowed, etc.

On our webcrawler this is done through the `RobotsTxtService`

Additionally, we also ready the `After-Delay` rule, which is not a mandatory field in the robots.txt spec but an interesting use-case that we implemented. See below on the HttpClient section for more info.

## HTTP Client

The Http Client is written to be able to be performant and fast.

Additionally, it handles retries and throttling request when there is a requested delay from the `/robots.txt`

### Retry logic
Using `Polly` library, we can easily set some retries to the calling API when the URL experience either 5xx error codes or timeouts `408`. In that case, we setup an exponential backoff retry.
We also included the `429 too Many Requests` error code - we shouldn't hit this, but if we do there is a `Retry-After` header that we can read and throttle. 

### Polite Throttling

TBD

## Parsers

Once our worker gets a response from the API, as per our task requirements we need to be able to parse the HTML, and extract the links.
This is relatively simple: In HTML, the anchor tag (<`a>`) creates hyperlinks so we just need to retrieve this from the DOM, and extract the `href` link.
The difficulty are mainly two:
- For Static websites this simple - we just need to extract links from the anchor tags. However modern websites are JavaScript-heavy websites, and they need rendering before getting the HTML (unless it's been SEO-optimised). In order to solve this, we created two parsers:
  - Static HTML parser: Fast and simple, using a popular library, `HtmlAgilityPack`.
  - Rendered HTLM Parser using selenium: Much slower because it requires running the browser through a WebDriver (in our case `chromedriver`) , copying the heavy JS code and past in a blank page and wait for completion.
  - Because the latter is much slower, we set up a fallback mechanism to first run static HTML parser, and if fails try the other one.
- Some of these links are broken or are not crawlable (e.g. pdfs, jpg, etc), so we need to sanitize them

With that, we created an efficient parser flow logic that handles different exceptions and sets the Parser status code.


# Concurrency Model

As we want to make our Web Crawler performant and efficient we want to be able to allow concurrent tasks for all the different URLs. After considering different options,
I decided to use a consumer-producer pattern via .NET's `Channel<T>`.`Channel<T>` is implemented with low-lock and async-native constructs, whereas lock/SemaphoreSlim uses traditional synchronization primitives that scale worse under high contention.
Also, is more readable approach, less error-prone and more mantainable!

As a summary, Channels are an implementation of the producer/consumer programming model: producers asynchronously produce data, and consumers asynchronously consume that data. The data is passed in a FIFO (First-In, First-Out) queue data structure.

The approach taken is the following:
- The main thread starts by enqueuing the root link into our `Channel<Link>`, encapsulated in the `CrawlerSession` class. 
- We generate a number of concurrent worker to consume the channel (limited by the `MaxConcurrenty` settings). The `await foreach (var item in session.ChannelSession.Reader.ReadAllAsync())` will block until a link is available in the channel. 
- The worker pulls a message from the channel when available, and does a few things:
   - Downloads the page at the link URL
   - Parses the HTML (either static or dynamically rendered — see the `Parsers` section).
   - Checks if the link has been visited or exceeds `maxDepth` (using thread-safe HashMap/Dictionaries to avoid data races).
   - If valid, messages are enqueued into the channel for further crawling.

A key challenge was knowing when to shut down the crawler. Since ReadAllAsync() blocks indefinitely unless the channel is closed, we needed a safe way to signal completion when no more work remains. So I introduced a thread-safe work item counter, where we:
- Increments each time a link is enqueued.
- Decrements when a link is fully processed.
- When the counter reaches zero, we complete the channel - which signals the workers to exit. 
Special care needs to be done to ensure we are peforming atomic increase/decrease operations, otherwise we risk to have race conditions. This can be done using Interlocked or pure locks.
I choose Interlock because it much more faster (CPU instruction level). 

## Alternatives

Other alternatives where explored first, see below why they weren't chosen.

### Pure `Async/Await`:
While the pure async/await model works correctly and efficiently on a per-operation basis, it is less performant in our context because it executes sequentially by default. Even though await allows the thread to return to the thread pool (freeing it for other work), each operation still waits for the previous one to complete before starting the next.

This pattern is ideal for I/O-bound, request/response-style applications like web APIs, where multiple client requests are processed concurrently using the same shared thread pool. However, in our case — a recursive, high-throughput web crawler — we need to process many independent HTTP requests concurrently to maximize throughput.

To achieve that, we require true parallelism and concurrency — not just non-blocking I/O — so we can crawl multiple pages at once, discover more links, and fan out the crawl efficiently. This is where the producer-consumer model with Channel<T> and a controlled number of async workers becomes far more effective.

### Multithreading:
The initial approach was to spawn multiple asynchronous tasks for each URL using Task.WhenAll() to wait for them to complete. While this provides basic concurrency, it lacks central coordination, and quickly becomes problematic as the number of discovered links grows.

This model leads to:

- Unbounded concurrency, where hundreds or thousands of tasks may be created with no throttling
- Thread pool exhaustion in high-load scenarios
- No backpressure, making it easy to overwhelm system or network resources
- Difficult error handling and retries, especially for transient failures or timeouts

While some of these issues can be mitigated (e.g., using `SemaphoreSlim` to cap concurrency), doing so adds complexity, and still lacks a centralized, coordinated pipeline for managing crawl state, retries, and graceful shutdown. 
In practice, it became clear that this approach was both inefficient and hard to maintain for a recursive, stateful crawling task.

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
- If we need real-time updates - we could consider long polling. 
- The way we implemented our core logic behaves like a BFS (Breath-First Search), although is not absolutely guaranteed due to the use of concurrent workers. However, depending on of our approach we could have choose DFS approach. I though DFS was less useful because we likely want to explore the links closest to the root domain and less on deep links. However if the requirements were different I would implement this differently.

## Parsers
Although we are using fallback mechanism to run the rendered HTML parser, we could have many of these to parse actual files like .pdf, .docx, etc. 

## Infrastructure
Currently our crawler is a good solution and the code is production-ready. However, in order to make this really production ready we should think of setting up the infrastructure and CI/CD deployment pipelines. A suggestion on how

## CI/CD Development
- Setup protection branch and CI jobs to run test, perform security scan, linting, etc
- Trigger Github actions to create releases. An option could be to connect this to ArgoCD for deploying the binaries, and Spacelift for provision terraform changes. 

