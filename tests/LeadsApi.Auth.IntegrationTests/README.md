# LeadsApi.Auth.IntegrationTests

Remote auth integration tests for the QA environment.

These tests call Kong in QA and acquire user-context JWTs from the in-house IDP impersonation flow.

## Required environment variables

- `AUTH_TEST_ENV` (must be `qa`)
- `AUTH_TEST_BASE_URL` (Kong URL, e.g. `https://qa-api.company.com`)
- `AUTH_TEST_IDP_TOKEN_ENDPOINT` (IDP token endpoint)
- `AUTH_TEST_CLIENT_ID`
- `AUTH_TEST_CLIENT_SECRET`
- `AUTH_TEST_USER_ADVISER`
- `AUTH_TEST_USER_CUSTOMER`
- `AUTH_TEST_USER_MANAGER`
- `AUTH_TEST_USER_NON_PRIVILEGED`

## Optional environment variables

- `AUTH_TEST_AUDIENCE`
- `AUTH_TEST_MACHINE_SCOPE` (default `openid profile`)
- `AUTH_TEST_SCOPE_LEADS_IMPORT` (default `leads:import`)
- `AUTH_TEST_SCOPE_READ` (default `profile:read`)
- `AUTH_TEST_IMPERSONATION_MODE` (`token_exchange` or `endpoint`, default `token_exchange`)
- `AUTH_TEST_IMPERSONATION_GRANT_TYPE` (default `urn:ietf:params:oauth:grant-type:token-exchange`)
- `AUTH_TEST_REQUESTED_SUBJECT_FIELD` (default `requested_subject`)
- `AUTH_TEST_IDP_IMPERSONATION_ENDPOINT` (required when `AUTH_TEST_IMPERSONATION_MODE=endpoint`)
