#!/usr/bin/env bash
# End-to-end smoke test against a running InventoryPro API.
#
# Usage:
#   BASE_URL=http://localhost:5231 ./scripts/smoke-test.sh
set -euo pipefail

BASE_URL="${BASE_URL:-http://localhost:5231}"
ADMIN_EMAIL="${ADMIN_EMAIL:-admin@inventorypro.local}"
ADMIN_PASSWORD="${ADMIN_PASSWORD:-Admin@123}"
STAFF_EMAIL="${STAFF_EMAIL:-staff@inventorypro.local}"
STAFF_PASSWORD="${STAFF_PASSWORD:-Staff@123}"

pass() { printf '  [PASS] %s\n' "$1"; }
fail() { printf '  [FAIL] %s\n' "$1"; exit 1; }
info() { printf '\n== %s ==\n' "$1"; }

json_field() {
  python3 -c "import sys,json;print(json.load(sys.stdin)$1)"
}

status_code() {
  curl -s -o /dev/null -w '%{http_code}' "$@"
}

login_token() {
  curl -s -X POST "$BASE_URL/api/auth/login" \
    -H 'Content-Type: application/json' \
    -d "{\"email\":\"$1\",\"password\":\"$2\"}" | json_field '["data"]["token"]'
}

info "Health check"
HEALTH=$(curl -s "$BASE_URL/health")
[ "$HEALTH" = "Healthy" ] || fail "health returned '$HEALTH'"
pass "GET /health -> Healthy"

info "Authentication"
ADMIN_TOKEN=$(login_token "$ADMIN_EMAIL" "$ADMIN_PASSWORD")
STAFF_TOKEN=$(login_token "$STAFF_EMAIL" "$STAFF_PASSWORD")
[ -n "$ADMIN_TOKEN" ] || fail "admin login returned no token"
[ -n "$STAFF_TOKEN" ] || fail "staff login returned no token"
pass "admin and staff can log in and receive JWTs"

info "Authorization"
UNAUTH=$(status_code "$BASE_URL/api/products")
[ "$UNAUTH" = "401" ] || fail "expected 401 without a token, got $UNAUTH"
pass "GET /api/products without a token -> 401"

STAFF_ADMIN=$(status_code "$BASE_URL/api/dashboard/admin" -H "Authorization: Bearer $STAFF_TOKEN")
[ "$STAFF_ADMIN" = "403" ] || fail "expected 403 for staff on admin dashboard, got $STAFF_ADMIN"
pass "staff on admin endpoint -> 403"

info "Catalog read"
PRODUCT_TOTAL=$(curl -s "$BASE_URL/api/products?page=1&pageSize=5" \
  -H "Authorization: Bearer $ADMIN_TOKEN" | json_field '["data"]["totalCount"]')
[ "$PRODUCT_TOTAL" -ge 1 ] || fail "expected at least one seeded product"
pass "products listed (totalCount=$PRODUCT_TOTAL)"

info "Stock movement"
PRODUCT_ID=$(curl -s "$BASE_URL/api/products?page=1&pageSize=1&sortBy=name" \
  -H "Authorization: Bearer $ADMIN_TOKEN" | json_field '["data"]["items"][0]["id"]')
QTY_BEFORE=$(curl -s "$BASE_URL/api/products/$PRODUCT_ID" \
  -H "Authorization: Bearer $ADMIN_TOKEN" | json_field '["data"]["quantityInStock"]')

curl -s -X POST "$BASE_URL/api/purchases" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H 'Content-Type: application/json' \
  -d "{\"supplierId\":1,\"notes\":\"Smoke test\",\"items\":[{\"productId\":$PRODUCT_ID,\"quantity\":10,\"unitCost\":5.00}]}" > /dev/null

QTY_AFTER=$(curl -s "$BASE_URL/api/products/$PRODUCT_ID" \
  -H "Authorization: Bearer $ADMIN_TOKEN" | json_field '["data"]["quantityInStock"]')
[ "$QTY_AFTER" -eq $((QTY_BEFORE + 10)) ] || fail "purchase did not increase stock ($QTY_BEFORE -> $QTY_AFTER)"
pass "purchase increased stock by 10 ($QTY_BEFORE -> $QTY_AFTER)"

curl -s -X POST "$BASE_URL/api/sales" \
  -H "Authorization: Bearer $STAFF_TOKEN" \
  -H 'Content-Type: application/json' \
  -d "{\"customerName\":\"Smoke Test\",\"items\":[{\"productId\":$PRODUCT_ID,\"quantity\":2}]}" > /dev/null

QTY_AFTER_SALE=$(curl -s "$BASE_URL/api/products/$PRODUCT_ID" \
  -H "Authorization: Bearer $ADMIN_TOKEN" | json_field '["data"]["quantityInStock"]')
[ "$QTY_AFTER_SALE" -eq $((QTY_AFTER - 2)) ] || fail "sale did not decrease stock ($QTY_AFTER -> $QTY_AFTER_SALE)"
pass "sale decreased stock by 2 ($QTY_AFTER -> $QTY_AFTER_SALE)"

info "Validation and error handling"
OVERSELL_CODE=$(status_code -X POST "$BASE_URL/api/sales" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H 'Content-Type: application/json' \
  -d "{\"items\":[{\"productId\":$PRODUCT_ID,\"quantity\":999999}]}")
[ "$OVERSELL_CODE" = "409" ] || fail "expected 409 for overselling, got $OVERSELL_CODE"
pass "overselling returns 409 Conflict"

INVALID_CODE=$(status_code -X POST "$BASE_URL/api/auth/login" \
  -H 'Content-Type: application/json' \
  -d '{"email":"not-an-email","password":"x"}')
[ "$INVALID_CODE" = "400" ] || fail "expected 400 for invalid payload, got $INVALID_CODE"
pass "invalid payload returns 400 Bad Request"

printf '\nAll smoke tests passed.\n'
