param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$Name
)

$ErrorActionPreference = 'Stop'

$root = $PSScriptRoot

function Get-SolutionFile {
    $sln = Get-ChildItem -Path $root -Filter '*.sln' -File | Select-Object -First 1
    if ($sln) {
        return $sln.FullName
    }

    $slnx = Get-ChildItem -Path $root -Filter '*.slnx' -File | Select-Object -First 1
    if ($slnx) {
        return $slnx.FullName
    }

    throw 'No .sln or .slnx file found in repository root.'
}

function Get-AppSettingsPath {
    $apiDirs = Get-ChildItem -Path $root -Directory | Where-Object { $_.Name -like '*API' }
    foreach ($apiDir in $apiDirs) {
        $path = Join-Path $apiDir.FullName 'appsettings.json'
        if (Test-Path $path) {
            return $path
        }
    }

    throw 'No appsettings.json found under an *API project folder.'
}

function Get-ConfigureServicesPath {
    $paths = Get-ChildItem -Path $root -Recurse -Filter 'ConfigureServices.cs' -File |
        Where-Object { $_.Directory.Name -like '*Infrastructure' -and $_.FullName -notlike '*\obj\*' }

    $path = $paths | Select-Object -First 1
    if (-not $path) {
        throw 'ConfigureServices.cs not found under an Infrastructure project.'
    }

    return $path.FullName
}

function Get-ProjectPath {
    param(
        [string]$Pattern
    )

    $project = Get-ChildItem -Path $root -Recurse -Filter $Pattern -File |
        Where-Object { $_.FullName -notlike '*\obj\*' -and $_.FullName -notlike '*\bin\*' } |
        Select-Object -First 1

    if (-not $project) {
        throw "Project not found for pattern: $Pattern"
    }

    return $project.FullName
}

function Get-EfPackageVersion {
    param(
        [string]$PackageId
    )

    $projects = @(
        (Get-ProjectPath '*Infrastructure*.csproj'),
        (Get-ProjectPath '*API*.csproj')
    )

    foreach ($projectPath in $projects) {
        [xml]$doc = Get-Content $projectPath
        $node = $doc.Project.ItemGroup.PackageReference | Where-Object { $_.Include -eq $PackageId } | Select-Object -First 1
        if ($node -and $node.Version) {
            return [string]$node.Version
        }
    }

    return '10.0.7'
}

function Ensure-MigrationProject {
    param(
        [string]$SolutionPath,
        [string]$Domain,
        [string]$ProjectSuffix,
        [string]$ProviderPackageId,
        [string]$InfrastructureProjectPath,
        [string]$ApiProjectPath
    )

    $projectName = "$Domain.Migrations.$ProjectSuffix"
    $projectDir = Join-Path $root $projectName
    $projectPath = Join-Path $projectDir "$projectName.csproj"

    if (-not (Test-Path $projectPath)) {
        New-Item -ItemType Directory -Path $projectDir -Force | Out-Null
        dotnet new classlib -n $projectName -o $projectDir -f net10.0 --force | Out-Null

        $classFile = Join-Path $projectDir 'Class1.cs'
        if (Test-Path $classFile) {
            Remove-Item $classFile -Force
        }

        $efDesignVersion = Get-EfPackageVersion 'Microsoft.EntityFrameworkCore.Design'
        $providerVersion = Get-EfPackageVersion $ProviderPackageId

        $relativeInfra = [System.IO.Path]::GetRelativePath($projectDir, $InfrastructureProjectPath) -replace '\\', '/'

        @"
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <ProjectReference Include="$relativeInfra" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="$efDesignVersion">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="$ProviderPackageId" Version="$providerVersion" />
  </ItemGroup>
</Project>
"@ | Set-Content -Path $projectPath -Encoding UTF8

        dotnet sln $SolutionPath add $projectPath --solution-folder Migrations | Out-Null
    }

    dotnet add $ApiProjectPath reference $projectPath | Out-Null

    return $projectPath
}

function Ensure-ApiDesignPackage {
    param(
        [string]$ApiProjectPath
    )

    [xml]$doc = Get-Content $ApiProjectPath
    $existing = $doc.Project.ItemGroup.PackageReference | Where-Object { $_.Include -eq 'Microsoft.EntityFrameworkCore.Design' } | Select-Object -First 1
    if ($existing) {
        return
    }

    $version = Get-EfPackageVersion 'Microsoft.EntityFrameworkCore.Design'
    dotnet add $ApiProjectPath package Microsoft.EntityFrameworkCore.Design --version $version | Out-Null
}

function Invoke-EfMigrationAdd {
    param(
        [string]$MigrationName,
        [string]$DatabaseType,
        [string]$MigrationProjectPath,
        [string]$StartupProjectPath,
        [string]$ContextName
    )

    $previousDatabaseType = $env:DatabaseType
    $env:DatabaseType = $DatabaseType

    try {
        dotnet ef migrations add $MigrationName `
            --project $MigrationProjectPath `
            --startup-project $StartupProjectPath `
            --context $ContextName `
            --output-dir Migrations
    }
    finally {
        if ($null -ne $previousDatabaseType) {
            $env:DatabaseType = $previousDatabaseType
        }
        else {
            Remove-Item Env:DatabaseType -ErrorAction SilentlyContinue
        }
    }
}

$solutionPath = Get-SolutionFile
$appsettingsPath = Get-AppSettingsPath
$configureServicesPath = Get-ConfigureServicesPath
$configureServicesContent = Get-Content $configureServicesPath -Raw

$appsettings = Get-Content $appsettingsPath -Raw | ConvertFrom-Json
$domain = $appsettings.BiAppOptions.Domain

if ([string]::IsNullOrWhiteSpace($domain)) {
    throw 'BiAppOptions:Domain is missing in appsettings.json.'
}

if ($configureServicesContent -notmatch 'AddDatabase<(\w+),') {
    throw 'AddDatabase<...> not found in ConfigureServices.cs.'
}

$mainDbContext = $Matches[1]
$hasBolt = $configureServicesContent -match 'AddBolt<BoltDbContext'

$apiProjectPath = Get-ProjectPath '*API*.csproj'
$infrastructureProjectPath = Get-ProjectPath '*Infrastructure*.csproj'

Ensure-ApiDesignPackage -ApiProjectPath $apiProjectPath

$postgresProject = Ensure-MigrationProject `
    -SolutionPath $solutionPath `
    -Domain $domain `
    -ProjectSuffix 'Postgres' `
    -ProviderPackageId 'Npgsql.EntityFrameworkCore.PostgreSQL' `
    -InfrastructureProjectPath $infrastructureProjectPath `
    -ApiProjectPath $apiProjectPath

$mssqlProject = Ensure-MigrationProject `
    -SolutionPath $solutionPath `
    -Domain $domain `
    -ProjectSuffix 'Mssql' `
    -ProviderPackageId 'Microsoft.EntityFrameworkCore.SqlServer' `
    -InfrastructureProjectPath $infrastructureProjectPath `
    -ApiProjectPath $apiProjectPath

Write-Host "Adding main migration '$Name' for PostgreSQL..."
Invoke-EfMigrationAdd -MigrationName $Name -DatabaseType 'PostgreSQL' -MigrationProjectPath $postgresProject -StartupProjectPath $apiProjectPath -ContextName $mainDbContext

Write-Host "Adding main migration '$Name' for MSSQL..."
Invoke-EfMigrationAdd -MigrationName $Name -DatabaseType 'MSSQL' -MigrationProjectPath $mssqlProject -StartupProjectPath $apiProjectPath -ContextName $mainDbContext

if ($hasBolt) {
    $postgresBoltProject = Ensure-MigrationProject `
        -SolutionPath $solutionPath `
        -Domain $domain `
        -ProjectSuffix 'Postgres.Bolt' `
        -ProviderPackageId 'Npgsql.EntityFrameworkCore.PostgreSQL' `
        -InfrastructureProjectPath $infrastructureProjectPath `
        -ApiProjectPath $apiProjectPath

    $mssqlBoltProject = Ensure-MigrationProject `
        -SolutionPath $solutionPath `
        -Domain $domain `
        -ProjectSuffix 'Mssql.Bolt' `
        -ProviderPackageId 'Microsoft.EntityFrameworkCore.SqlServer' `
        -InfrastructureProjectPath $infrastructureProjectPath `
        -ApiProjectPath $apiProjectPath

    Write-Host "Adding Bolt migration '$Name' for PostgreSQL..."
    Invoke-EfMigrationAdd -MigrationName $Name -DatabaseType 'PostgreSQL' -MigrationProjectPath $postgresBoltProject -StartupProjectPath $apiProjectPath -ContextName 'BoltDbContext'

    Write-Host "Adding Bolt migration '$Name' for MSSQL..."
    Invoke-EfMigrationAdd -MigrationName $Name -DatabaseType 'MSSQL' -MigrationProjectPath $mssqlBoltProject -StartupProjectPath $apiProjectPath -ContextName 'BoltDbContext'
}

Write-Host "Done. Migration '$Name' created."
