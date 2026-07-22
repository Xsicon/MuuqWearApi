# MuuqWear API — Admin RBAC

Section-level role-based access for the Blazor admin portal (`MuuqWear.Web`).

## JWT role claim

| Item | Value |
|------|--------|
| Claim name | `app_role` |
| ASP.NET role claim type | `app_role` (see `Program.cs` → `RoleClaimType`) |
| Login response field | `data.role` (same string as claim) |

After `POST /api/Auth/login`, the API returns a **custom HS256 JWT** (not the raw Supabase access token). The token includes:

- `sub` / `NameIdentifier` → user id
- `email`
- `app_role` → profile role slug (e.g. `operations_manager`)

Storefront users receive `app_role: user`. Staff portal roles are listed below.

## Staff roles

| Role slug | Display name |
|-----------|----------------|
| `admin` | Admin (full access) |
| `operations_manager` | Operations Manager |
| `support_team` | Customer Support |
| `merchandising` | Merchandising |
| `content_team` | Creative & Content |
| `technology_systems` | Technology & Systems |

Constants: `MuuqWear.Model.DTO.AdminSettingsUserDTO.AdminRoles`

## Authorization policies

Defined in `MuuqWear.Application/Shared/AdminAuthorizationPolicies.cs`.

| Policy | Allowed roles |
|--------|----------------|
| `StaffPortal` | All 6 staff roles |
| `AdminOrders` | `admin`, `operations_manager` |
| `AdminProducts` | `admin`, `merchandising` |
| `AdminContent` | `admin`, `content_team` |
| `AdminAffiliates` | `admin`, `operations_manager` |
| `AdminSupport` | `admin`, `support_team` |
| `AdminCareers` | `admin`, `operations_manager` |
| `AdminSystem` | `admin`, `technology_systems` |
| `AdminCustomers` | `admin` only |
| `AdminCustomerNotesRead` | `admin`, `support_team` (read-only customer list + notes) |
| `AdminAnalytics` | `admin`, `operations_manager` |
| `AdminOnly` | `admin` only |

`admin` satisfies every policy via role inclusion in each policy definition.

## Forbidden responses

Authenticated users without the required role receive **HTTP 403** with:

```json
{
  "success": false,
  "message": "You do not have permission to access this resource.",
  "data": null
}
```

Unauthenticated requests receive **401**.

Handler: `MuuqWear.API/Authorization/ForbiddenJsonAuthorizationMiddlewareResultHandler.cs`

## Endpoint matrix (v1)

### Staff portal (all 6 roles)

| Method | Path | Policy |
|--------|------|--------|
| GET | `api/AdminBadge/counts` | StaffPortal |
| GET | `api/Notification/recent` | StaffPortal |
| POST | `api/Profile/notifications-read` | StaffPortal |

### Orders — AdminOrders

| Method | Path |
|--------|------|
| GET | `api/Order/admin` |
| GET | `api/Order/admin/{orderId}` |
| PATCH | `api/Order/admin/{orderId}/status` |
| PATCH | `api/Order/admin/bulk-status` |
| GET | `api/Return/admin` |
| PATCH | `api/Return/admin/{returnId}/status` |

### Products — AdminProducts (mutations + stock)

Staff-only catalog management. **Storefront catalog reads are public** (no JWT).

| Method | Path | Policy |
|--------|------|--------|
| POST | `api/Product/add` | AdminProducts |
| PUT | `api/Product/update/{id}` | AdminProducts |
| DELETE | `api/Product/delete/{id}` | AdminProducts |
| POST | `api/Product/upload-image` | AdminProducts |
| POST | `api/Product/images/add` | AdminProducts |
| DELETE | `api/Product/images/{imageId}` | AdminProducts |
| GET/PATCH/POST/DELETE | `api/Product/.../size-stock` | AdminProducts |

**Public storefront reads** (`[AllowAnonymous]` — used by MuuqWear.Web without auth):

| Method | Path |
|--------|------|
| GET | `api/Product/all` |
| GET | `api/Product/home` |
| GET | `api/Product/{id}` |
| GET | `api/Product/{id}/related` |

### Content — AdminContent

All routes on `api/Content/*` (controller-level policy).

### Affiliates — AdminAffiliates

All `api/Affiliate/admin/*` routes.

### Support — AdminSupport

| Method | Path |
|--------|------|
| GET | `api/Help/admin/tickets` |
| GET | `api/Help/admin/tickets/{ticketId}` |
| PATCH | `api/Help/admin/tickets/{ticketId}/status` |
| GET | `api/Help/admin/stats` |
| GET | `api/Chat/active-sessions` |
| GET | `api/Chat/messages/{sessionId}` |
| GET | `api/Chat/session/{sessionId}` |
| POST | `api/Chat/close/{sessionId}` |
| GET | `api/Chat/session/{sessionId}/status` |

### Careers — AdminCareers

Admin routes on `api/JobPosting/*` (except `open`, public application submit, resume upload).

### System — AdminSystem

All `api/AdminSystem/*` routes.

### Customers — AdminCustomers / AdminCustomerNotesRead

| Method | Path | Policy |
|--------|------|--------|
| GET | `api/Customer` | AdminCustomerNotesRead |
| GET | `api/Customer/{id}/notes` | AdminCustomerNotesRead |
| POST | `api/Customer/{id}/notes` | AdminCustomers |

### Analytics — AdminAnalytics

| Method | Path |
|--------|------|
| GET | `api/Analytics/revenue` |
| GET | `api/Analytics/top-products` |
| GET | `api/Analytics/affiliate-performance` |

### Admin only — AdminOnly

All `api/AdminSetting/*` routes (users, invite, health checks).

## Demo accounts

Seed script: `docs/scripts/seed-demo-admin-users.ps1` (or `.sql`).

| Email | Password | Role |
|-------|----------|------|
| admin@muuqwear.com | admin123 | admin |
| ops@muuqwear.com | ops123 | operations_manager |
| support@muuqwear.com | support123 | support_team |
| merch@muuqwear.com | merch123 | merchandising |
| creative@muuqwear.com | creative123 | content_team |
| tech@muuqwear.com | tech123 | technology_systems |

## curl examples

Replace `BASE` and use the `accessToken` from login.

### Login (operations manager)

```bash
curl -s -X POST "$BASE/api/Auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email":"ops@muuqwear.com","password":"ops123"}'
```

Expected: `"role":"operations_manager"` and JWT with `"app_role":"operations_manager"`.

### Allowed — ops → orders

```bash
curl -s -o /dev/null -w "%{http_code}" \
  -H "Authorization: Bearer $TOKEN" \
  "$BASE/api/Order/admin?page=1&pageSize=20"
# 200
```

### Denied — ops → product mutations

```bash
curl -s -w "\n%{http_code}\n" -X POST \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{}' \
  "$BASE/api/Product/add"
# 403 + success:false
```

### Public — storefront catalog (no auth)

```bash
curl -s -o /dev/null -w "%{http_code}" \
  "$BASE/api/Product/all?page=1&pageSize=20"
# 200
```

### Allowed — support → customer notes feed

```bash
curl -s -o /dev/null -w "%{http_code}" \
  -H "Authorization: Bearer $SUPPORT_TOKEN" \
  "$BASE/api/Customer?page=1&pageSize=20"
# 200
```

### Denied — support → orders

```bash
curl -s -o /dev/null -w "%{http_code}" \
  -H "Authorization: Bearer $SUPPORT_TOKEN" \
  "$BASE/api/Order/admin"
# 403
```

### Allowed — tech → system overview

```bash
curl -s -o /dev/null -w "%{http_code}" \
  -H "Authorization: Bearer $TECH_TOKEN" \
  "$BASE/api/AdminSystem/overview"
# 200
```

Automated smoke test: `docs/scripts/rbac-smoke-test.ps1`

## Out of scope (v1)

- Button-level permissions within a page
- Multi-role users
- Refund API routes (not implemented in API yet)
