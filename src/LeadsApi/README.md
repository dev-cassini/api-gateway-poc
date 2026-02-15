# Leads API (PoC)

This is a minimal ASP.NET Core API targeting `net10.0` for the API gateway PoC.

## Endpoints

- `POST /leads` -> requires scope `leads:import`.
- `GET /leads/{leadId}` -> requires role `adviser` or `customer`.
- `POST /leads/{leadId}/assign` -> requires role `adviser` and staff type `manager` (looked up via external API).

## Demo authentication

The project uses a simple bearer token parser for local PoC use:

`Authorization: Bearer <userId>|<comma-separated-roles>[|<comma-separated-scopes>]`

Examples:

- `Bearer adviser-01|adviser|leads:import`
- `Bearer customer-17|customer`
- `Bearer manager-22|adviser`

## Staff directory API contract

`POST /leads/{leadId}/assign` calls:

- `GET {StaffDirectory:BaseUrl}/staff/{userId}/type`

Expected response body:

```json
{ "staffType": "manager" }
```

Any non-200 response (or non-manager `staffType`) denies the assignment request.
