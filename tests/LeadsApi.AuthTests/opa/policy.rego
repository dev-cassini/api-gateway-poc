package leads

default authz = {"allow": false, "status": 403, "message": "forbidden"}

authz = {"allow": false, "status": 401, "message": "unauthorized"} if {
  not has_valid_token
}

authz = {"allow": true} if {
  has_valid_token
  is_create
  has_scope("leads:import")
}

authz = {"allow": true} if {
  has_valid_token
  is_get
  has_any_role(["adviser", "customer"])
}

authz = {"allow": true} if {
  has_valid_token
  is_assign
  has_role("adviser")
  is_manager
}

is_create if {
  method == "POST"
  path == "/leads"
}

is_get if {
  method == "GET"
  startswith(path, "/leads/")
  not endswith(path, "/assign")
}

is_assign if {
  method == "POST"
  startswith(path, "/leads/")
  endswith(path, "/assign")
}

is_manager if {
  data.staff_types[user_id] == "manager"
}

has_any_role(expected_roles) if {
  some role in expected_roles
  has_role(role)
}

has_role(role) if {
  some token_role in roles
  token_role == role
}

has_scope(scope) if {
  some token_scope in scopes
  token_scope == scope
}

has_valid_token if {
  has_authorization
  count(token_parts) >= 2
  token_parts[0] != ""
}

has_authorization if {
  auth_header != ""
  startswith(lower(auth_header), "bearer ")
}

user_id := token_parts[0]
roles := split(token_parts[1], ",")
scopes := split(token_parts[2], ",") if {
  count(token_parts) >= 3
}
scopes := [] if {
  count(token_parts) < 3
}

token_parts := split(token, "|")

token := trim_space(trim_prefix(auth_header, "Bearer "))

method := upper(input.request.http.method)
path := input.request.http.path
auth_header := object.get(input.request.http.headers, "authorization", "")
