# Leads API (PoC)

This is a minimal ASP.NET Core API targeting `net10.0` for the API gateway PoC.

## Endpoints

- `POST /leads` -> route is anonymous in the API; gateway is expected to require scope `leads:import`.
- `GET /leads/{leadId}` -> route is anonymous in the API; gateway is expected to require role `adviser` or `customer`.
- `POST /leads/{leadId}/assign` -> route is anonymous in the API; gateway is expected to require role `adviser`.
- `POST /leads/{leadId}/assign` also performs an inline manager check in the API via staff directory lookup.

## Demo authentication

The API uses a simple bearer token parser for local PoC use to populate `HttpContext.User`
after Kong forwards the `Authorization` header:

`Authorization: Bearer <userId>|<comma-separated-roles>[|<comma-separated-scopes>[|<email>]]`

Examples:

- `Bearer adviser-01|adviser|leads:import|adviser-01@example.com`
- `Bearer customer-17|customer`
- `Bearer manager-22|adviser`

`POST /leads` sets `CreatedBy` from the token email claim (`email` / `ClaimTypes.Email`). If the claim is absent, `CreatedBy` is `null`.

## Staff directory API contract

`POST /leads/{leadId}/assign` calls:

- `GET {StaffDirectory:BaseUrl}/staff/{userId}/type`

Expected response body:

```json
{ "staffType": "manager" }
```

Any non-200 response (or non-manager `staffType`) denies the assignment request.
