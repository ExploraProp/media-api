<#
.SYNOPSIS
    Manual check of Media Result/Error Problem Details (code + en/es titles).

.EXAMPLE
    ./test-error-i18n.ps1
    ./test-error-i18n.ps1 -BaseUrl http://localhost:7005
    ./test-error-i18n.ps1 -BaseUrl http://localhost:62463
    ./test-error-i18n.ps1 -Token $env:INMO_ACCESS_TOKEN
#>
param(
    [string]$BaseUrl,
    [string]$Token,
    [string]$PrivateMediaId,
    [string]$PublicMediaId
)

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$script:PassCount = 0
$script:FailCount = 0

if ($PSVersionTable.PSVersion.Major -ge 6) {
    $script:InvokeSplat = @{ SkipCertificateCheck = $true; SkipHttpErrorCheck = $true }
}
else {
    $script:InvokeSplat = @{}
}

function Get-JsonProperty {
    param($Object, [string]$Name)
    if ($null -eq $Object) { return $null }
    $prop = $Object.PSObject.Properties[$Name]
    if ($prop) { return $prop.Value }
    return $null
}

function Get-FieldErrors {
    param($Json)
    $errors = Get-JsonProperty $Json "errors"
    if ($null -eq $errors) { return @() }
    return @($errors)
}

function Find-FieldError {
    param($Json, [string[]]$Code)
    foreach ($item in (Get-FieldErrors $Json)) {
        $itemCode = Get-JsonProperty $item "code"
        if ($Code -contains $itemCode) { return $item }
    }
    return $null
}

function Invoke-MediaApi {
    param(
        [string]$Path,
        [string]$Method = "GET",
        [string]$Language = "en",
        [hashtable]$Form = $null,
        [switch]$WithAuth
    )

    $uri = "$script:BaseUrl$Path"
    $headers = @{
        "Accept"          = "application/json"
        "Accept-Language" = $Language
    }
    if ($WithAuth -and $Token) {
        $headers["Authorization"] = "Bearer $Token"
    }

    $params = @{
        Uri             = $uri
        Method          = $Method
        Headers         = $headers
        UseBasicParsing = $true
    } + $script:InvokeSplat

    if ($null -ne $Form) {
        $params.Form = $Form
    }

    try {
        $response = Invoke-WebRequest @params
    }
    catch {
        $http = $_.Exception.Response
        if (-not $http) { throw }
        $reader = New-Object System.IO.StreamReader($http.GetResponseStream())
        $raw = $reader.ReadToEnd()
        $reader.Dispose()
        return [pscustomobject]@{
            Status = [int]$http.StatusCode
            Json   = if ($raw) { $raw | ConvertFrom-Json } else { $null }
        }
    }

    $raw = $response.Content
    $json = $null
    if ($raw) {
        try { $json = $raw | ConvertFrom-Json } catch { $json = $null }
    }

    return [pscustomobject]@{
        Status = [int]$response.StatusCode
        Json   = $json
    }
}

function Assert-LocalizedPair {
    param(
        [string]$Name,
        $En,
        $Es,
        [int]$Status,
        [string[]]$Code,
        [string]$TitleEn
    )

    $enCode = Get-JsonProperty $En.Json "code"
    $enTitle = Get-JsonProperty $En.Json "title"
    $enField = $null
    $esField = $null
    if ($Status -eq 422) {
        $enField = Find-FieldError $En.Json $Code
        $esField = Find-FieldError $Es.Json $Code
        $enFieldTitle = Get-JsonProperty $enField "title"
        $okEn = ($En.Status -eq 422) -and ($enCode -eq "Validation.Failed") -and
            ($enTitle -eq "Validation failed") -and ($null -ne $enField) -and ($enFieldTitle -eq $TitleEn)
    }
    else {
        $okEn = ($En.Status -eq $Status) -and ($Code -contains $enCode) -and ($enTitle -eq $TitleEn)
    }
    if ($okEn) { $script:PassCount++; Write-Host "  PASS  $Name (en)" -ForegroundColor Green }
    else {
        $script:FailCount++
        $enFieldCode = Get-JsonProperty $enField "code"
        $enFieldTitle = Get-JsonProperty $enField "title"
        Write-Host "  FAIL  $Name (en) status=$($En.Status) code=$enCode title=$enTitle field=$enFieldCode/$enFieldTitle" -ForegroundColor Red
    }

    $esCode = Get-JsonProperty $Es.Json "code"
    $esTitle = Get-JsonProperty $Es.Json "title"
    if ($Status -eq 422) {
        $esFieldTitle = Get-JsonProperty $esField "title"
        $enFieldTitle = Get-JsonProperty $enField "title"
        $okEs = ($Es.Status -eq 422) -and ($esCode -eq "Validation.Failed") -and
            ($null -ne $esField) -and
            -not [string]::IsNullOrWhiteSpace($esTitle) -and ($esTitle -ne $enTitle) -and
            -not [string]::IsNullOrWhiteSpace($esFieldTitle) -and ($esFieldTitle -ne $enFieldTitle)
    }
    else {
        $okEs = ($Es.Status -eq $Status) -and ($Code -contains $esCode) -and
            -not [string]::IsNullOrWhiteSpace($esTitle) -and ($esTitle -ne $enTitle)
    }
    if ($okEs) { $script:PassCount++; Write-Host "  PASS  $Name (es)" -ForegroundColor Green }
    else {
        $script:FailCount++
        $esFieldCode = Get-JsonProperty $esField "code"
        $esFieldTitle = Get-JsonProperty $esField "title"
        Write-Host "  FAIL  $Name (es) status=$($Es.Status) code=$esCode title=$esTitle field=$esFieldCode/$esFieldTitle" -ForegroundColor Red
    }
}

function Test-Health([string]$Url) {
    try {
        $params = @{
            Uri             = "$Url/health/live"
            Method          = "GET"
            UseBasicParsing = $true
            TimeoutSec      = 3
        } + $script:InvokeSplat
        $r = Invoke-WebRequest @params
        return ([int]$r.StatusCode) -ge 200 -and ([int]$r.StatusCode) -lt 300
    }
    catch {
        return $false
    }
}

if (-not $BaseUrl) {
    foreach ($candidate in @("http://localhost:7005", "http://localhost:62463", "https://localhost:62361")) {
        if (Test-Health $candidate) { $BaseUrl = $candidate; break }
    }
    if (-not $BaseUrl) {
        Write-Host "Media API not reachable on 7005/62463/62361. Pass -BaseUrl." -ForegroundColor Red
        exit 1
    }
}
else {
    $BaseUrl = $BaseUrl.TrimEnd("/")
    if (-not (Test-Health $BaseUrl)) {
        Write-Host "Media API not reachable at $BaseUrl/health/live" -ForegroundColor Red
        exit 1
    }
}

$script:BaseUrl = $BaseUrl.TrimEnd("/")
Write-Host "Media error i18n smoke @ $script:BaseUrl"

$missingId = [guid]::NewGuid().ToString()
Assert-LocalizedPair "media file not found" `
    (Invoke-MediaApi -Path "/api/v1/media/$missingId" -Language "en") `
    (Invoke-MediaApi -Path "/api/v1/media/$missingId" -Language "es") `
    404 @("MediaFile.NotFound") "Media file not found"

Assert-LocalizedPair "malformed media id" `
    (Invoke-MediaApi -Path "/api/v1/media/not-a-guid" -Language "en") `
    (Invoke-MediaApi -Path "/api/v1/media/not-a-guid" -Language "es") `
    422 @("MediaFileId.InvalidFormat") "Media file id is invalid"

if ($Token) {
    $temp = [System.IO.Path]::GetTempFileName()
    Set-Content -Path $temp -Value "not-an-image" -NoNewline
    $form = @{
        ownerType = "listing"
        ownerId   = [guid]::NewGuid().ToString()
        mediaType = "photo"
        file      = Get-Item $temp
    }
    Assert-LocalizedPair "unsupported content type" `
        (Invoke-MediaApi -Path "/api/v1/media/upload" -Method "POST" -Language "en" -Form $form -WithAuth) `
        (Invoke-MediaApi -Path "/api/v1/media/upload" -Method "POST" -Language "es" -Form $form -WithAuth) `
        422 @("MediaPolicy.AllowedContentTypes.Invalid") "File type is not allowed"
    Remove-Item $temp -ErrorAction SilentlyContinue
}
else {
    Write-Host "  SKIP  upload 422 policy codes (pass -Token)" -ForegroundColor Yellow
}

if ($PrivateMediaId) {
    Assert-LocalizedPair "private file public GET" `
        (Invoke-MediaApi -Path "/api/v1/media/$PrivateMediaId" -Language "en") `
        (Invoke-MediaApi -Path "/api/v1/media/$PrivateMediaId" -Language "es") `
        403 @("MediaFile.AccessLevel.Private") "File is private"
}
else {
    Write-Host "  SKIP  GET private 403 (pass -PrivateMediaId)" -ForegroundColor Yellow
}

if ($PublicMediaId) {
    Assert-LocalizedPair "public file signed URL" `
        (Invoke-MediaApi -Path "/api/v1/media/$PublicMediaId/signed-url" -Language "en") `
        (Invoke-MediaApi -Path "/api/v1/media/$PublicMediaId/signed-url" -Language "es") `
        403 @("MediaFile.AccessLevel.Public") "File is public"
}
else {
    Write-Host "  SKIP  signed-url public 403 (pass -PublicMediaId)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Passed: $script:PassCount  Failed: $script:FailCount"
if ($script:FailCount -gt 0) { exit 1 }
