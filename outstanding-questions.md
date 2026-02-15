# Outstanding Questions

- What should happen when a request is submitted without an Authorization header, assuming the endpoint is genuinely configured to allow anonymous?
- Where should resource-guarding 403 policies be applied (for example, `GET /leads/{leadId}` requiring the lead to belong to the calling user): within service code, or as OPA policy at the API gateway?
