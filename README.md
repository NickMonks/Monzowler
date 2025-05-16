# 🕸️🏦 Monzowler

A fast, performant webcrawler for the Monzo Task Challenge!

# 📦 Tech Stack
- ASP.NET Core – API: Webcrawler service
- DynamoDB - main persistence to store `Jobs` and `Sitemaps`
- Jaeger - Distributed tracing for observability

# Requirements

For information regarding assumed requirements check docs/requirements.md.

# How to Run

## Option 1 (Recommended)

Run the `docker-compose.yml` file using the following command:

```curl
docker compose up -d 
```

To use the API endpoints specificed below, open `locahost:5002/swagger/index.html` endpoint and give it a try there. The flow is the following:
- Call `POST /Crawl` by clicking _Try It Out_ button and set the request body, e.g.
```json
{
  "url": "https://monzo.com/",
  "maxDepth": 3,
  "maxRetries": 2
}
```
- It will return a `jobId` in the response. Use the `GET /Crawl/{jobId}` endpoint to track the status (it should take a few seconds if depth < 2).
- When status is `"Completed"`, check the `GET /Crawl/sitemap/{jobId}` endpoint - you can get the sitemap JSON response and filter by status.

For more info about the API specs, check API Overview section. 

Alternatively you can use the `Monzowler.Api.http` file or choose your favourite API Platform to hit the controller. 

## Option 2 - Execute Binary directly

Binaries have been generated for each OS - choose your binary and execute it with the following arguments:

```curl
.\releases\win-x64\webcrawler.exe "https://monzo.com" --jobId my-job-id --depth 2 --maxConcurrency 4
```

## Option 3 - Command Line

:warning: For this option you need to install dotnet +8. Follow [this](https://dotnet.microsoft.com/en-us/download) link to do so. 

If you don't want to run the API and simply what the logs - use the CLI tool under `api/Monzowler.CLI`. And follow the steps:

Navigate to `src/api/Monzowler.CLI` directory and run:

```curl
dotnet run -- {your_url} --jobId {your_job_id}
```

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

For information regarding the architecture of the project and deep dives please check docs/architecture.md.

# Testing

Inside the `/test` folder, I set up two testing project: `UnitTest` and `IntegrationTest`.
I tried to follow the [testing pyramid](https://martinfowler.com/articles/practical-test-pyramid.html) approach: unit tests for Core layers and integration tests for API and Infrastructure.

Some description provided below:
- **Unit tests:** I tried to cover  test the smallest pieces of code in isolation directly. For example any persistence, http client call I tried to keep away external functionality using mocks (e.g. `Moq` library). Inside these tests I tried to cover for edge cases and assumptions. 
- **Integration tests:** Aims to ensure that different parts of the application work together as expected.
    - The approach is to test the Controller directly using the `TestContainers` library, a lightweight solution to run some dependencies as docker containers. For more info, check [here](https://testcontainers.com/)
    - I also combined it with some API client mock to test external calls to specific domains. In our code we test different scenarios but we could cover much more outside the MVP. 

# 