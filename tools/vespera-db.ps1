<#
.SYNOPSIS
Manage the Vespera dev database Docker container.

.DESCRIPTION
Thin wrapper around the docker CLI for the same container Vespera.Api's Docker provisioning
strategy manages (default name: vespera-db-dev, label com.vespera.managed=true). Defaults match
Vespera.Api's appsettings.Development.json — override with parameters if you've changed those.

.EXAMPLE
tools/vespera-db.ps1 up
tools/vespera-db.ps1 status
tools/vespera-db.ps1 down
#>
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidateSet("up", "down", "reset", "status", "logs")]
    [string]$Command,

    [string]$ContainerName = "vespera-db-dev",
    [string]$VolumeName = "vespera-db-dev-data",
    [string]$Image = "postgres:16-alpine",
    [int]$Port = 55432,
    [string]$Username = "postgres",
    [string]$Password = "postgres"
)

$ErrorActionPreference = "Stop"

function Invoke-Up {
    $existing = docker ps -a --filter "name=^/$ContainerName`$" --format "{{.ID}}"
    if ($existing) {
        Write-Host "Starting existing container '$ContainerName'..."
        docker start $ContainerName | Out-Null
    }
    else {
        Write-Host "Creating container '$ContainerName' on port $Port..."
        docker run -d `
            --name $ContainerName `
            --label "com.vespera.managed=true" `
            -e "POSTGRES_USER=$Username" `
            -e "POSTGRES_PASSWORD=$Password" `
            -p "${Port}:5432" `
            -v "${VolumeName}:/var/lib/postgresql/data" `
            $Image | Out-Null
    }
    Write-Host "Vespera dev database is up on port $Port."
}

function Invoke-Down {
    Write-Host "Stopping container '$ContainerName'..."
    docker stop $ContainerName 2>$null | Out-Null
}

function Invoke-Reset {
    Write-Host "Removing container '$ContainerName' and volume '$VolumeName'..."
    docker rm -f $ContainerName 2>$null | Out-Null
    docker volume rm $VolumeName 2>$null | Out-Null
    Invoke-Up
}

function Invoke-Status {
    docker ps -a --filter "name=^/$ContainerName`$" --format "table {{.Names}}`t{{.Status}}`t{{.Ports}}"
}

function Invoke-Logs {
    docker logs -f $ContainerName
}

switch ($Command) {
    "up" { Invoke-Up }
    "down" { Invoke-Down }
    "reset" { Invoke-Reset }
    "status" { Invoke-Status }
    "logs" { Invoke-Logs }
}
