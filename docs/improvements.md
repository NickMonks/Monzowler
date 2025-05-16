# Improvements & Future development
Below are some thoughts & ideas to improve the current state of our project.

- We also might want to crawl urls that have been already been visited but changed the payload - we could create a hash and do this

## Resiliency
What happens if the crawler crashes mid-way? we will lose the pages crawl and we will need to start again!
we would like to be resilient and store each sitemap at least.

## BFS vs. DFS
The way we implemented our core logic behaves like a BFS (Breath-First Search), although is not absolutely guaranteed due to the use of concurrent workers. However, depending on of our approach we could have choose DFS approach. I though DFS was less useful because we likely want to explore the links closest to the root domain and less on deep links. However if the requirements were different I would implement this differently.

## Real-time updates
Right now when a job is created we simply return a job-id for the client to poll our server - this is inefficient because it created load on our service inecessarily.
One option is to implement long polling or SSE (Server-side events) to enhance this. But might be overengineering if short polling seems to work fine on our service.

## Efficiency
We are crawling on a domain every time a new job is requested. However this is very inefficient - the same URLs will be crawled again and again, plus http calls are our main bottleneck!
Therefore we can set up some distributed caching like Redis for this purpose.

However we need to be careful on this - what if some pages have new URLs to be crawled? A tradeoff needs to be done to efficiently deal with this.

## Database improvements
- Paginated response
- Commit crawls midway

## Parsers
Although we are using fallback mechanism to run the rendered HTML parser, we could have many of these to parse actual files like .pdf, .docx, etc.

## Infrastructure
Currently our crawler is a good solution and the code is production-ready. However, in order to make this really production ready we should think of setting up the infrastructure and CI/CD deployment pipelines. A suggestion on how
I would do it is by deploying the webcrawler using a lambda and use it as a consumer of our messages in a queue, which since we are using DynamoDB AWS ecosystem I would probably use SQS. 

## CI/CD Development
- Setup protection branch and CI jobs to run test, perform security scan, linting, etc
- Trigger Github actions to create releases. An option could be to connect this to ArgoCD for deploying the binaries, and Spacelift for provision terraform changes.

## Observability
Even if Jaeger OTLP exporter is helpful for observability, there is a lot of improvement: some span might've been missed, and from the code perspective this cross-cutting concern makes the code a bit ugly. If I had more time I would
definitely work on refactor this. 