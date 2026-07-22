# Seeds demo admin/staff accounts via Supabase Auth Admin + PostgREST.
# Requires Supabase service role key (user-secrets: SupaBase:ServiceRoleKey).
# Usage: .\docs\scripts\seed-demo-admin-users.ps1

$ErrorActionPreference = "Stop"

$projectRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$apiProject = Join-Path $projectRoot "MuuqWearApi\MuuqWear.API\MuuqWear.API.csproj"

$supabaseUrl = "https://yvooccmbtokzfnqibyig.supabase.co"
$serviceRoleKey = dotnet user-secrets list --project $apiProject 2>$null |
    Where-Object { $_ -match "^SupaBase:ServiceRoleKey\s*=\s*(.+)$" } |
    ForEach-Object { $matches[1].Trim() }

if (-not $serviceRoleKey) {
    $serviceRoleKey = dotnet user-secrets list --project $apiProject 2>$null |
        Where-Object { $_ -match "^Supabase:ServiceRoleKey\s*=\s*(.+)$" } |
        ForEach-Object { $matches[1].Trim() }
}

if (-not $serviceRoleKey) {
    throw "Supabase service role key not found in user-secrets (SupaBase:ServiceRoleKey or Supabase:ServiceRoleKey)."
}

$authHeaders = @{
    apikey         = $serviceRoleKey
    Authorization  = "Bearer $serviceRoleKey"
    "Content-Type" = "application/json"
    "User-Agent"   = "MuuqWear-Seed/1.0"
}

$restHeaders = @{
    apikey         = $serviceRoleKey
    Authorization  = "Bearer $serviceRoleKey"
    "Content-Type" = "application/json"
    Prefer         = "resolution=merge-duplicates"
    "Accept-Profile" = "MuuqWear"
    "Content-Profile" = "MuuqWear"
    "User-Agent"   = "MuuqWear-Seed/1.0"
}

$accounts = @(
    @{ Id = "b0000001-0000-4000-8000-000000000001"; Email = "admin@muuqwear.com";    Password = "admin123";    Name = "Admin";                Role = "admin" },
    @{ Id = "b0000001-0000-4000-8000-000000000002"; Email = "ops@muuqwear.com";      Password = "ops123";      Name = "Operations Manager";   Role = "operations_manager" },
    @{ Id = "b0000001-0000-4000-8000-000000000003"; Email = "support@muuqwear.com";  Password = "support123";  Name = "Customer Support";     Role = "support_team" },
    @{ Id = "b0000001-0000-4000-8000-000000000004"; Email = "merch@muuqwear.com";    Password = "merch123";    Name = "Merchandising";        Role = "merchandising" },
    @{ Id = "b0000001-0000-4000-8000-000000000005"; Email = "creative@muuqwear.com"; Password = "creative123"; Name = "Creative & Content";   Role = "content_team" },
    @{ Id = "b0000001-0000-4000-8000-000000000006"; Email = "tech@muuqwear.com";     Password = "tech123";     Name = "Technology & Systems"; Role = "technology_systems" }
)

function Get-AdminUserByEmail {
    param([string]$Email)
    $uri = "$supabaseUrl/auth/v1/admin/users?page=1&per_page=200"
    $users = Invoke-RestMethod -Method Get -Uri $uri -Headers $authHeaders
    return $users.users | Where-Object { $_.email -eq $Email } | Select-Object -First 1
}

foreach ($acct in $accounts) {
    Write-Host "Seeding $($acct.Email) ($($acct.Role))..."

    $createBody = @{
        id            = $acct.Id
        email         = $acct.Email
        password      = $acct.Password
        email_confirm = $true
        user_metadata = @{ full_name = $acct.Name }
    } | ConvertTo-Json -Depth 5

    $userId = $acct.Id

    try {
        $created = Invoke-RestMethod -Method Post -Uri "$supabaseUrl/auth/v1/admin/users" -Headers $authHeaders -Body $createBody
        $userId = $created.id
        Write-Host "  Created auth user $userId"
    }
    catch {
        $statusCode = $null
        if ($_.Exception.Response) {
            $statusCode = [int]$_.Exception.Response.StatusCode
        }

        if ($statusCode -eq 422) {
            $existing = Get-AdminUserByEmail -Email $acct.Email
            if (-not $existing) { throw $_ }
            $userId = $existing.id
        }
        else {
            throw $_
        }

        $updateBody = @{
            password      = $acct.Password
            email_confirm = $true
            user_metadata = @{ full_name = $acct.Name }
        } | ConvertTo-Json -Depth 5

        Invoke-RestMethod -Method Put -Uri "$supabaseUrl/auth/v1/admin/users/$userId" -Headers $authHeaders -Body $updateBody | Out-Null
        Write-Host "  Updated existing auth user $userId"
    }

    $profileBody = @{
        id         = $userId
        full_name  = $acct.Name
        email      = $acct.Email
        role       = $acct.Role
        is_deleted = $false
        created_at = (Get-Date).ToUniversalTime().ToString("o")
    } | ConvertTo-Json -Depth 5

    Invoke-RestMethod -Method Post -Uri "$supabaseUrl/rest/v1/profiles?on_conflict=id" -Headers $restHeaders -Body $profileBody | Out-Null
    Write-Host "  Upserted profile"
}

Write-Host "`nDone. Demo accounts are ready."
