param([string]$OutputDirectory = 'artifacts/release-au19.4')
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$root = Split-Path $PSScriptRoot -Parent
$output = [IO.Path]::GetFullPath((Join-Path $root $OutputDirectory))
New-Item -ItemType Directory -Path $output -Force | Out-Null
$inputs = @(
    @{ Name='loader-x64.zip'; Url='https://builds.bepinex.dev/projects/bepinex_be/735/BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.735%2B5fef357.zip'; Hash='BADEF8112853A00939A0DF6CA143BC0A4E3DC02BD4D21B873302731BFA0E4DF4' },
    @{ Name='translations.zip'; Url='https://github.com/Lotus-AU/Languages/archive/1cb89d1fe013407cc13e472e1472e95ac8ad3056.zip'; Hash='26494DFB79D1303E9D15A533707E6F230D2EB09981FABF04C6ACED9972EEDE70' }
)
foreach ($inputArchive in $inputs) {
    $archivePath = Join-Path $output $inputArchive.Name
    if (!(Test-Path $archivePath)) { Invoke-WebRequest $inputArchive.Url -OutFile $archivePath }
    if ((Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash -ne $inputArchive.Hash) { throw "Unexpected archive checksum: $archivePath" }
}
$stage = Join-Path $output 'Epic-x64'
if (Test-Path $stage) { throw 'Staging directory already exists; use a fresh output directory.' }
New-Item -ItemType Directory -Path $stage -Force | Out-Null
$archive = [IO.Compression.ZipFile]::OpenRead((Join-Path $output 'loader-x64.zip'))
try {
 foreach ($entry in $archive.Entries) {
  $name = $entry.FullName.Replace('\','/')
  if (!$entry.Name) { continue }
  if (!($name.StartsWith('dotnet/') -or $name.StartsWith('BepInEx/core/') -or $name -in @('winhttp.dll','doorstop_config.ini','.doorstop_version','BepInEx/config/BepInEx.cfg'))) { continue }
  $destination = [IO.Path]::GetFullPath((Join-Path $stage $name))
  if (!$destination.StartsWith($stage + [IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe archive path' }
  New-Item -ItemType Directory -Path (Split-Path $destination -Parent) -Force | Out-Null
  [IO.Compression.ZipFileExtensions]::ExtractToFile($entry,$destination)
 }
} finally { $archive.Dispose() }
$languageDirectory = Join-Path $stage 'Languages/Lotus'
New-Item -ItemType Directory -Path $languageDirectory -Force | Out-Null
$archive = [IO.Compression.ZipFile]::OpenRead((Join-Path $output 'translations.zip'))
try {
 foreach ($entry in $archive.Entries) {
  if ($entry.Name -notlike 'lang_*.yaml' -or $entry.Name -like 'lang_Template*') { continue }
  [IO.Compression.ZipFileExtensions]::ExtractToFile($entry,(Join-Path $languageDirectory $entry.Name))
 }
} finally { $archive.Dispose() }
$plugins = Join-Path $stage 'BepInEx/plugins'
New-Item -ItemType Directory -Path $plugins -Force | Out-Null
Copy-Item "$root/bin/Release/net6.0/Lotus.dll" $plugins
Copy-Item "$root/.dependencies/VentFramework-Continued/bin/Release/net6.0/VentFrameworkContinued.dll" $plugins
Copy-Item "$plugins/Lotus.dll","$plugins/VentFrameworkContinued.dll" $output
Copy-Item "$root/docs/RELEASE-au19.4.md" "$stage/INSTALL.md"
Copy-Item "$root/docs/DEPENDENCY_AUDIT.md" $stage
Copy-Item "$root/LICENSE" "$stage/LICENSE-Lotus.txt"
Copy-Item "$root/.dependencies/VentFramework-Continued/LICENSE" "$stage/LICENSE-VentFramework.txt"
$zip = Join-Path $output 'Lotus-1.7.3-au19.4-Epic-x64.zip'
[IO.Compression.ZipFile]::CreateFromDirectory($stage,$zip)
$releaseFiles = @($zip,(Join-Path $output 'Lotus.dll'),(Join-Path $output 'VentFrameworkContinued.dll'))
$releaseFiles | ForEach-Object { $hash=Get-FileHash -LiteralPath $_ -Algorithm SHA256; $hash.Hash.ToLowerInvariant() + '  ' + [IO.Path]::GetFileName($_) } | Set-Content (Join-Path $output 'SHA256SUMS.txt')
$releaseFiles | Get-Item | Select-Object Name,Length
