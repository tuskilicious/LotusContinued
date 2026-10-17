param([string]$Dotnet = 'dotnet')
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$dependency = Join-Path $root '.dependencies/VentFramework-Continued'
$pin = '998678ca83415b8a22adcd294e7e5a9a37630907'
if (!(Test-Path "$dependency/.git")) {
    git clone https://github.com/Lotus-AU/VentFramework-Continued.git $dependency
    if ($LASTEXITCODE) { throw 'Dependency clone failed' }
    git -C $dependency checkout --detach $pin
    if ($LASTEXITCODE) { throw 'Dependency checkout failed' }
}
if ((git -C $dependency rev-parse HEAD) -ne $pin) { throw 'Unexpected dependency revision' }
$patch = Join-Path $root 'patches/ventframework-v19.patch'
git -C $dependency apply --reverse --check $patch 2>$null
if ($LASTEXITCODE) {
    git -C $dependency apply --check $patch
    if ($LASTEXITCODE) { throw 'Dependency differs from expected source' }
    git -C $dependency apply $patch
    if ($LASTEXITCODE) { throw 'Dependency patch failed' }
}
& $Dotnet build "$dependency/VentFrameworkContinued.csproj" -c Release
if ($LASTEXITCODE) { throw 'Framework build failed' }
& $Dotnet build "$root/Lotus.csproj" -c Release
if ($LASTEXITCODE) { throw 'Lotus build failed' }
& $Dotnet run --project "$root/tests/NetworkRegression/NetworkRegression.csproj" -c Release
if ($LASTEXITCODE) { throw 'Regression checks failed' }
