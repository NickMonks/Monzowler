# Functional Requirements
- Given a starting URL, the crawler should visit each URL it finds on the same domain.
- Users should be able to see each URL printed and a list of links found on that page
- The crawler should be limited to one domain, e.g. "https://monzo.com/"

# Non-Functional Requirements
- Low latency - we want to be as performant and efficient as possible
- Scalable solution - we should we able to 
- Fault-tolerant 
- Politeness: We should be able to respect `/robots.txt` to avoid overwhelming other domains. 

