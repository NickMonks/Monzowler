# Monzowler

A fast, performant webcrawler for Monzo Task Challenge!

# How to Run

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
  
  Rel(user, monzowler)
  Rel(monzowler, websites)
  Rel(monzowler, localstack)
  Rel(monzowler, jaeger)

  UpdateLayoutConfig($c4ShapeInRow="3", $c4BoundaryInRow="1")
  UpdateRelStyle(user, monzowler, $offsetX="-30", $offsetY="30")
  UpdateRelStyle(monzowler, websites, $offsetX="30", $offsetY="-40")
  UpdateRelStyle(monzowler, localstack, $offsetX="30", $offsetY="30")
  UpdateRelStyle(monzowler, jaeger, $offsetX="-30", $offsetY="-30")
```

## Code Architecture


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

## Parsers



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