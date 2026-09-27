# Password reset secrets (MuuqWearApi)

Do **not** commit real tokens. Configure via user-secrets or host environment variables.

```powershell
cd MuuqWearApi\MuuqWearApi\MuuqWear.API

dotnet user-secrets set "Postmark:ServerToken" "<POSTMARK_SERVER_API_TOKEN>"
dotnet user-secrets set "Postmark:FromEmail" "MuuqWear Ops <noreply@your-verified-domain.com>"
dotnet user-secrets set "Postmark:MessageStream" "outbound"

# Service role stays server-only (already used elsewhere)
dotnet user-secrets set "SupaBase:ServiceRoleKey" "<SUPABASE_SERVICE_ROLE_KEY>"
# also accepted:
# dotnet user-secrets set "Supabase:ServiceRoleKey" "<SUPABASE_SERVICE_ROLE_KEY>"
```

## Ops checklist
1. Verify sending domain in Postmark (DKIM / Return-Path).
2. Live Transactional server; stream = `outbound`.
3. Allow-list `https://{app}/auth/reset-password` (and local `http://localhost:5276/auth/reset-password`) in Supabase Authentication → URL Configuration.
4. Rotate any token that was pasted into chat.
5. Manual test: forgot-password for a real user → Postmark Activity shows outbound tagged `password-reset` → open link → set password → login.
