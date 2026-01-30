cls

# ---------------------------------------
# Repo root (script location)
# ---------------------------------------
$rootPath = Split-Path -Parent $MyInvocation.MyCommand.Definition
Set-Location $rootPath

Write-Host "Running format and build..."
Write-Host "Repo directory: $rootPath"

# ---------------------------------------
# SLN / SLNX check
# ---------------------------------------
$solutions = Get-ChildItem -Path $rootPath -Filter "*.sln*" -File -ErrorAction SilentlyContinue
if (-not $solutions) {
    Write-Host "ERROR: No .sln or .slnx file found."
    exit 1
}

$solution = $solutions | Select-Object -First 1
if ($solutions.Count -gt 1) {
    Write-Host "Multiple solutions found; using the first one: $($solution.Name)"
}
$solutionPath = $solution.FullName
Write-Host "Using solution: $($solution.Name)"

# ---------------------------------------
# DOTNET FORMAT
# ---------------------------------------
try {
    dotnet format $solutionPath whitespace --verbosity minimal
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: dotnet format failed."
        exit 1
    }
    Write-Host "dotnet format succeeded."

    Write-Host "`nStarting dotnet format style..."
    Write-Host "Removing unused usings and sorting usings..."
    $styleOutput = dotnet format $solutionPath style --verbosity minimal --diagnostics IDE0005 --no-restore 2>&1
    $styleExitCode = $LASTEXITCODE

    if ($styleExitCode -ne 0) {
        Write-Host "ERROR: dotnet format style failed." -ForegroundColor Red
        foreach ($line in $styleOutput) {
            Write-Host $line
        }
        exit 1
    }

    Write-Host "dotnet format style succeeded (usings updated)."
}
catch {
    Write-Host "ERROR: dotnet format exception."
    Write-Host $_
    exit 1
}

# ---------------------------------------
# EOF BLANK LINE CLEANUP
# ---------------------------------------
Write-Host "`nCleaning EOF blank lines..."

$extensions = @("*.cs", "*.csproj", "*.json")

Get-ChildItem -Recurse -Include $extensions `
    -Exclude bin,obj |
Where-Object { $_.FullName -notmatch "\\bin\\|\\obj\\" } |
ForEach-Object {

    $path = $_.FullName
    $content = [System.IO.File]::ReadAllText($path)

    if ($null -ne $content) {
        $fixed = $content -replace "(\r?\n)+$", "`r`n"

        if ($fixed -ne $content) {
            [System.IO.File]::WriteAllText(
                $path,
                $fixed,
                [System.Text.UTF8Encoding]::new($false)
            )
            Write-Host "EOF fixed: $path"
        }
    }
}

Write-Host "EOF cleanup completed."

# ---------------------------------------
# DOTNET BUILD
# ---------------------------------------
Write-Host "`nStarting dotnet build..."

dotnet build $solutionPath --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "`nBUILD ERROR!"
    Write-Host "Fix build errors first."
    exit 1
}

Write-Host "`nBuild succeeded."

# ---------------------------------------
# ALL OK
# ---------------------------------------
Write-Host "`nFormat and build completed successfully."
exit 0