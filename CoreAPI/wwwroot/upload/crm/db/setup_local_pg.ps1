param(
  [string]$PostgresPassword = "YourStrongPassword123!",
  [string]$PostgresUser = "postgres",
  [string]$DatabaseName = "crm",
  [int]$PostgresPort = 6543,
  [int]$PostgresVersion = 17,
  [switch]$SkipInstall
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

function Write-Section {
  param([string]$Message)
  Write-Host "=== $Message ==="
}

function Find-PostgresCommand {
  param([string]$CommandName)

  $command = Get-Command $CommandName -ErrorAction SilentlyContinue
  if ($command) {
    return $command.Source
  }

  $commonRoots = @(
    "$env:ProgramFiles\PostgreSQL",
    "${env:ProgramFiles(x86)}\PostgreSQL"
  ) | Where-Object { $_ -and (Test-Path $_) }

  foreach ($root in $commonRoots) {
    $match = Get-ChildItem -Path $root -Directory -ErrorAction SilentlyContinue |
      Sort-Object Name -Descending |
      ForEach-Object {
        $candidate = Join-Path $_.FullName "bin\$CommandName.exe"
        if (Test-Path $candidate) { $candidate }
      } |
      Select-Object -First 1

    if ($match) {
      return $match
    }
  }

  return $null
}

function Install-PostgresIfNeeded {
  param([string]$PsqlPath)

  if ($PsqlPath) {
    return $PsqlPath
  }

  if ($SkipInstall) {
    throw "PostgreSQL is not installed and -SkipInstall was specified."
  }

  $winget = Get-Command winget -ErrorAction SilentlyContinue
  if (-not $winget) {
    throw "PostgreSQL is not installed and winget is unavailable. Install PostgreSQL manually, then rerun this script."
  }

  Write-Host "Installing PostgreSQL $PostgresVersion with winget..."
  & $winget.Source install -e --id "PostgreSQL.PostgreSQL.$PostgresVersion" --silent --accept-source-agreements --accept-package-agreements

  $psqlPathAfterInstall = Find-PostgresCommand -CommandName "psql"
  if (-not $psqlPathAfterInstall) {
    throw "PostgreSQL installation completed, but psql was not found on this machine. Install PostgreSQL manually or add its bin directory to PATH."
  }

  return $psqlPathAfterInstall
}

function Get-PostgresService {
  $services = Get-Service -Name "postgresql*" -ErrorAction SilentlyContinue |
    Sort-Object Status, Name -Descending

  if ($services) {
    return $services | Select-Object -First 1
  }

  return $null
}

function Wait-ForPostgres {
  param(
    [string]$PgIsReadyPath,
    [string]$PsqlPath,
    [string]$Username,
    [string]$Password,
    [int]$Port
  )

  $env:PGPASSWORD = $Password

  for ($attempt = 1; $attempt -le 30; $attempt++) {
    if ($PgIsReadyPath) {
      & $PgIsReadyPath -h localhost -p $Port -U $Username | Out-Null
      if ($LASTEXITCODE -eq 0) {
        return
      }
    }
    else {
      & $PsqlPath -h localhost -p $Port -U $Username -d postgres -c '\q' 2>$null | Out-Null
      if ($LASTEXITCODE -eq 0) {
        return
      }
    }

    Start-Sleep -Seconds 2
  }

  throw "PostgreSQL did not become ready in time. Verify the service is running and the password is correct."
}

function Invoke-PsqlFile {
  param(
    [string]$PsqlPath,
    [string]$Username,
    [int]$Port,
    [string]$Database,
    [string]$FilePath
  )

  if (-not (Test-Path $FilePath)) {
    throw "SQL file not found: $FilePath"
  }

  & $PsqlPath -h localhost -p $Port -U $Username -d $Database -f $FilePath
  if ($LASTEXITCODE -ne 0) {
    throw "Failed to execute SQL file: $FilePath"
  }
}

function Invoke-PsqlCommand {
  param(
    [string]$PsqlPath,
    [string]$Username,
    [int]$Port,
    [string]$Database,
    [string]$Sql
  )

  & $PsqlPath -h localhost -p $Port -U $Username -d $Database -v ON_ERROR_STOP=1 -c $Sql
  if ($LASTEXITCODE -ne 0) {
    throw "Failed to execute SQL command."
  }
}

function Quote-PgIdentifier {
  param([string]$Value)

  return '"' + $Value.Replace('"', '""') + '"'
}

function Quote-PgLiteral {
  param([string]$Value)

  return "'" + $Value.Replace("'", "''") + "'"
}

Write-Section "PostgreSQL Local Setup (Windows)"

$psqlPath = Find-PostgresCommand -CommandName "psql"
$psqlPath = Install-PostgresIfNeeded -PsqlPath $psqlPath
$pgBinDir = Split-Path -Parent $psqlPath
$env:PATH = "$pgBinDir;$env:PATH"

$pgIsReadyPath = Find-PostgresCommand -CommandName "pg_isready"

$service = Get-PostgresService
if ($service) {
  Write-Host "Starting PostgreSQL service: $($service.Name)..."
  if ($service.Status -ne "Running") {
    Start-Service -Name $service.Name
  }
}
else {
  Write-Host "No PostgreSQL Windows service was found. Continuing with local connection checks..."
}

Write-Host "Waiting for PostgreSQL to be ready..."
Wait-ForPostgres -PgIsReadyPath $pgIsReadyPath -PsqlPath $psqlPath -Username $PostgresUser -Password $PostgresPassword -Port $PostgresPort
Write-Host "PostgreSQL is ready."

$env:PGPASSWORD = $PostgresPassword

Write-Host "Setting up database..."
$quotedUser = Quote-PgIdentifier -Value $PostgresUser
$quotedDatabase = Quote-PgIdentifier -Value $DatabaseName
$quotedPassword = Quote-PgLiteral -Value $PostgresPassword

Invoke-PsqlCommand -PsqlPath $psqlPath -Username $PostgresUser -Port $PostgresPort -Database "postgres" -Sql "ALTER USER $quotedUser WITH PASSWORD $quotedPassword;"
Invoke-PsqlCommand -PsqlPath $psqlPath -Username $PostgresUser -Port $PostgresPort -Database "postgres" -Sql "DROP DATABASE IF EXISTS $quotedDatabase;"
Invoke-PsqlCommand -PsqlPath $psqlPath -Username $PostgresUser -Port $PostgresPort -Database "postgres" -Sql "CREATE DATABASE $quotedDatabase;"

Write-Host "Running schema script..."
Invoke-PsqlFile -PsqlPath $psqlPath -Username $PostgresUser -Port $PostgresPort -Database $DatabaseName -FilePath (Join-Path $scriptDir "202511221_schema_pg.sql")

Write-Host "Running seed script..."
Invoke-PsqlFile -PsqlPath $psqlPath -Username $PostgresUser -Port $PostgresPort -Database $DatabaseName -FilePath (Join-Path $scriptDir "202511222_seed_pg.sql")

Write-Section "Setup complete"
Write-Host "Database: $DatabaseName"
Write-Host "User: $PostgresUser"
Write-Host "Password: $PostgresPassword"
Write-Host "Connection: postgresql://$($PostgresUser):$($PostgresPassword)@localhost:$PostgresPort/$DatabaseName"
