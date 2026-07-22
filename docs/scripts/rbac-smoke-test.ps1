# RBAC smoke test - logs in each demo staff account and checks allowed/denied endpoints.
# Usage: .\docs\scripts\rbac-smoke-test.ps1 [-BaseUrl http://localhost:5xxx]

param(
    [string]$BaseUrl = "http://localhost:5000"
)

$ErrorActionPreference = "Stop"

$accounts = @(
    @{
        Email = "ops@muuqwear.com"; Password = "ops123"; Role = "operations_manager"
        Allowed = @("/api/Order/admin?page=1&pageSize=5", "/api/Affiliate/admin/stats")
        Denied  = @("/api/AdminSystem/overview", "POST:/api/Product/add")
    },
    @{
        Email = "support@muuqwear.com"; Password = "support123"; Role = "support_team"
        Allowed = @("/api/Help/admin/tickets?page=1", "/api/Customer?page=1&pageSize=5")
        Denied  = @("/api/Order/admin", "POST:/api/Product/add")
    },
    @{
        Email = "merch@muuqwear.com"; Password = "merch123"; Role = "merchandising"
        Allowed = @("POST:/api/Product/add")
        Denied  = @("/api/Order/admin", "/api/Content/JournalArticles")
    },
    @{
        Email = "creative@muuqwear.com"; Password = "creative123"; Role = "content_team"
        Allowed = @("/api/Content/JournalArticles")
        Denied  = @("/api/Order/admin", "POST:/api/Product/add")
    },
    @{
        Email = "tech@muuqwear.com"; Password = "tech123"; Role = "technology_systems"
        Allowed = @("/api/AdminSystem/overview", "/api/AdminSystem/jobs")
        Denied  = @("/api/Order/admin", "POST:/api/Product/add")
    }
)

$publicCatalogPaths = @(
    "/api/Product/all?page=1&pageSize=5",
    "/api/Product/home"
)

function Get-AccessToken {
    param([string]$Email, [string]$Password)
    $body = @{ email = $Email; password = $Password } | ConvertTo-Json
    $resp = $null
    for ($attempt = 1; $attempt -le 3; $attempt++) {
        try {
            $resp = Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/Auth/login" -ContentType "application/json" -Body $body
            break
        }
        catch {
            if ($attempt -eq 3) { throw }
            Start-Sleep -Seconds 2
        }
    }
    if (-not $resp.success) { throw "Login failed for $Email : $($resp.message)" }
    if ($resp.data.role -ne $accounts.Where({ $_.Email -eq $Email }).Role) {
        throw "Role mismatch for $Email - expected $($accounts.Where({ $_.Email -eq $Email }).Role) got $($resp.data.role)"
    }
    return $resp.data.accessToken
}

function Invoke-ApiStatus {
    param(
        [string]$Path,
        [string]$Token = $null,
        [string]$Method = "GET",
        [string]$Body = $null
    )

    $headers = @{}
    if ($Token) { $headers["Authorization"] = "Bearer $Token" }

    try {
        $params = @{
            Uri             = "$BaseUrl$Path"
            Method          = $Method
            Headers         = $headers
            UseBasicParsing = $true
        }
        if ($Body) {
            $params["ContentType"] = "application/json"
            $params["Body"] = $Body
        }
        $r = Invoke-WebRequest @params
        return [int]$r.StatusCode
    }
    catch {
        if ($_.Exception.Response) { return [int]$_.Exception.Response.StatusCode }
        throw
    }
}

function Test-PathSpec {
    param([string]$Spec)
    if ($Spec -match '^POST:(.+)$') {
        return @{ Method = "POST"; Path = $Matches[1]; Body = "{}" }
    }
    return @{ Method = "GET"; Path = $Spec; Body = $null }
}

$failures = 0

Write-Host "`n=== Anonymous storefront catalog ===" -ForegroundColor Cyan
foreach ($path in $publicCatalogPaths) {
    $code = Invoke-ApiStatus -Path $path
    if ($code -ge 200 -and $code -lt 300) {
        Write-Host "  PUBLIC OK $code $path" -ForegroundColor Green
    }
    else {
        Write-Host "  PUBLIC FAIL $code $path (expected 2xx)" -ForegroundColor Red
        $failures++
    }
}

foreach ($acct in $accounts) {
    Write-Host "`n=== $($acct.Email) ($($acct.Role)) ===" -ForegroundColor Cyan
    $token = Get-AccessToken -Email $acct.Email -Password $acct.Password
    Write-Host "  Login OK, role verified"

    foreach ($spec in $acct.Allowed) {
        $req = Test-PathSpec -Spec $spec
        $code = Invoke-ApiStatus -Token $token -Path $req.Path -Method $req.Method -Body $req.Body
        if ($req.Method -eq "POST") {
            if ($code -ne 403) {
                Write-Host "  ALLOW OK $code $spec (auth passed; not 403)" -ForegroundColor Green
            }
            else {
                Write-Host "  ALLOW FAIL $code $spec (expected not 403)" -ForegroundColor Red
                $failures++
            }
        }
        elseif ($code -ge 200 -and $code -lt 300) {
            Write-Host "  ALLOW OK $code $spec" -ForegroundColor Green
        }
        else {
            Write-Host "  ALLOW FAIL $code $spec (expected 2xx)" -ForegroundColor Red
            $failures++
        }
    }

    foreach ($spec in $acct.Denied) {
        $req = Test-PathSpec -Spec $spec
        $code = Invoke-ApiStatus -Token $token -Path $req.Path -Method $req.Method -Body $req.Body
        if ($code -eq 403) {
            Write-Host "  DENY OK 403 $spec" -ForegroundColor Green
        }
        else {
            Write-Host "  DENY FAIL $code $spec (expected 403)" -ForegroundColor Red
            $failures++
        }
    }
}

if ($failures -gt 0) {
    Write-Host "`n$failures check(s) failed." -ForegroundColor Red
    exit 1
}

Write-Host "`nAll RBAC smoke checks passed." -ForegroundColor Green
