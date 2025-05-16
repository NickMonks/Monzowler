# 🕸️🏦 Monzowler

A fast, performant webcrawler for the Monzo Task Challenge!

## Overview 

For a detailed description of the functional and non-functional requirements, refer to the [Requirements](docs/requirements.md) document.

This solution provides a web API interface to trigger the web crawler and process crawl requests asynchronously as background jobs. When invoking the API to start a new crawl, it responds immediately with a job ID and an Accepted status. This allows you to:
- Track the progress of the web crawling job.
- Retrieve the resulting sitemap, with the ability to filter by status.

The API behavior and usage are described in more detail in the API Overview and How to Run sections.

### Why this design?
There are two primary reasons for this architecture:
- **Asynchronous job processing** decouples the web crawling workload from the API response lifecycle. This prevents long-running HTTP requests, enables background processing, and allows results to be persisted in a datastore — following patterns used in production-grade systems. Basic observability features (metrics/traces) are also included, as detailed in the [Architecture](docs/architecture.md) Observability section.
- **Cross-platform compatibility**. By containerizing the solution, we ensure an environment that is OS-agnostic and easy to run without local setup headaches.

### CLI Alternative
For a simpler, more lightweight experience, you can also use the CLI entry point to execute the crawler directly. Instructions for running the CLI are provided in the How to Run section.

# How to Run

Make sure you have Docker installed on your machine.
Alternatively, If you prefer not to use Docker, you'll need to have .NET 8.0 SDK or higher installed for options 3 & 4 (manual runs).

## Option 1: API through Docker Compose (Recommended)

1. Ensure you have docker installed on your machine.
2. Run the following command from the project root to build and start the services:
```curl
docker compose down -v; docker compose up -d --build --force-recreate
```
3. Open your browser and navigate to: `http://localhost:5002/swagger/index.html`

:bulb: Note that you can run the API directly from swagger, simply click on **Try it Out** button (click on any request and right-side of the bar).

4. From there, you can explore the API endpoints. Typical flow:
- POST /Crawl → Initiate a crawl request:
```json
{
  "url": "https://monzo.com/",
  "maxDepth": 3,
  "maxRetries": 2
}
```
- You'll receive a jobId with a 202 Accepted response.
- Track the job status using `GET /Crawl/{jobId}`.
- Once status is "Completed", retrieve the sitemap using `GET /Crawl/sitemap/{jobId}`. 

For more info about the API specs, check API Overview section. Alternatively you can use the `Monzowler.Api.http` file or choose your favourite API Platform to hit the controller. 

## Option 2 - Run CLI (Docker)
If you prefer the CLI version:

1. From the project root, build the CLI Docker image:
```curl
docker build -f src/api/Monzowler.CLI/Dockerfile -t monzowler-cli .
```

Then run the crawler with your desired parameters. An example is provided below:
```curl
docker run --rm monzowler-cli https://monzo.com --maxDepth 1 --maxRetries 2
```

This method is lightweight and suitable for quick, direct crawls without the API layer.

## Option 3 - Command Line (Locally)

:warning: For this option you need to install dotnet +8. Follow [this](https://dotnet.microsoft.com/en-us/download) link to do so.

If you don't want to run the API nor generating the logs you can simply run the CLI project -  under `api/Monzowler.CLI`.

Navigate to `src/api/Monzowler.CLI` directory and run:

```curl
dotnet run -- {your_url} --maxDepth ${max_depth} --maxRetries ${max_retries}
```

## Option 4 - Generate Binaries

:warning: For this option you need to install dotnet +8. Follow [this](https://dotnet.microsoft.com/en-us/download) link if you want to proceed with this step.

Run the `./build-binaries.sh` - it will generate binaries for all OS. Then, go to the root folder and run:

```curl
.\releases\win-x64\webcrawler.exe ${your_url} --maxDepth ${max_depth} --maxRetries ${max_retries}
```

You can also find these on the Releases tag in the Github repository.

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

For information regarding the architecture of the project and deep dives please go [here](docs/architecture.md).

# Testing

Inside the `/test` folder, I set up two testing project: `UnitTest` and `IntegrationTest`.
I tried to follow the [testing pyramid](https://martinfowler.com/articles/practical-test-pyramid.html) approach: unit tests for Core layers and integration tests for API and Infrastructure.

Some description provided below:
- **Unit tests:** I tried to cover  test the smallest pieces of code in isolation directly. For example any persistence, http client call I tried to keep away external functionality using mocks (e.g. `Moq` library). Inside these tests I tried to cover for edge cases and assumptions. 
- **Integration tests:** Aims to ensure that different parts of the application work together as expected.
    - The approach is to test the Controller directly using the `TestContainers` library, a lightweight solution to run some dependencies as docker containers. For more info, check [here](https://testcontainers.com/)
    - I also combined it with some API client mock to test external calls to specific domains. In our code we test different scenarios but we could cover much more outside the MVP. 

Additionally I tested the webcrawler for multiple domains, I'm sure most the websites will still have bugs - however for an MVP I feel this is enough.
Some domains I tried and tested:
- https://monzo.com/
- https://www.bbc.co.uk/news
- https://www.youtube.com/

# 📦 Tech Stack
- ASP.NET Core – API: Webcrawler service
- DynamoDB - main persistence to store `Jobs` and `Sitemaps`
- Jaeger - Distributed tracing for observability

# Future & Improvements
Some of the future improvements I would like to introduce outside MVP-scope are gathered [here](docs/improvements.md). 