# Leads API (PoC)

This is a minimal ASP.NET Core API targeting `net10.0` for the API gateway PoC.

## Endpoints

- `POST /leads` -> route is anonymous in the API; gateway is expected to require scope `leads:import`.
- `GET /leads/{leadId}` -> route is anonymous in the API; gateway is expected to require role `adviser` or `customer`.
- `POST /leads/{leadId}/assign` -> route is anonymous in the API; gateway is expected to require role `adviser`.
- `POST /leads/{leadId}/assign` also performs an inline manager check in the API via staff directory lookup.

## JWT authentication

The API uses ASP.NET Core `JwtBearer` authentication to populate `HttpContext.User`
from the bearer token forwarded by Kong. The API does not enforce access control
policies directly; authorization is expected to be enforced by Kong.

For local testing, the signing key is configured via `Jwt:SigningKey`.

`POST /leads` sets `CreatedBy` from the token email claim (`email` / `ClaimTypes.Email`). If the claim is absent, `CreatedBy` is `null`.

## Staff directory API contract

`POST /leads/{leadId}/assign` calls:

- `GET {StaffDirectory:BaseUrl}/staff/{userId}/type`

Expected response body:

```json
{ "staffType": "manager" }
```

Any non-200 response (or non-manager `staffType`) denies the assignment request.
