[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$PackageDirectory,

    [Parameter(Mandatory)]
    [string]$ExpectedVersion
)

$ErrorActionPreference = 'Stop'

$expectedIds = @(
    'GenericRecognition.Workbench.Abstractions',
    'GenericRecognition.Workbench.Infrastructure',
    'GenericRecognition.Workbench.Wpf'
)

$packages = @(Get-ChildItem -LiteralPath $PackageDirectory -Filter '*.nupkg' |
    Where-Object { $_.Name -notlike '*.symbols.nupkg' })
$symbols = @(Get-ChildItem -LiteralPath $PackageDirectory -Filter '*.snupkg')

if ($packages.Count -ne $expectedIds.Count) {
    throw "Expected $($expectedIds.Count) .nupkg files but found $($packages.Count)."
}

if ($symbols.Count -ne $expectedIds.Count) {
    throw "Expected $($expectedIds.Count) .snupkg files but found $($symbols.Count)."
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
$seenIds = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)

foreach ($package in $packages) {
    $archive = [System.IO.Compression.ZipFile]::OpenRead($package.FullName)
    try {
        $nuspecEntry = @($archive.Entries | Where-Object FullName -like '*.nuspec')
        if ($nuspecEntry.Count -ne 1) {
            throw "Package '$($package.Name)' must contain exactly one nuspec."
        }

        $reader = [System.IO.StreamReader]::new($nuspecEntry[0].Open())
        try {
            [xml]$nuspec = $reader.ReadToEnd()
        }
        finally {
            $reader.Dispose()
        }

        $metadata = $nuspec.package.metadata
        $id = [string]$metadata.id
        $version = [string]$metadata.version

        if ($id -notin $expectedIds) {
            throw "Unexpected package ID '$id' in '$($package.Name)'."
        }

        if (-not $seenIds.Add($id)) {
            throw "Package ID '$id' was produced more than once."
        }

        if ($version -cne $ExpectedVersion) {
            throw "Package '$id' has version '$version'; expected '$ExpectedVersion'."
        }

        if ($metadata.license.InnerText -cne 'MIT' -or $metadata.license.GetAttribute('type') -cne 'expression') {
            throw "Package '$id' must use the MIT license expression."
        }

        if ($metadata.repository.GetAttribute('url') -cne 'https://github.com/p-o-ke-nae/GenericRecognitionWorkbench.git') {
            throw "Package '$id' has an unexpected repository URL."
        }

        $readme = [string]$metadata.readme
        if ([string]::IsNullOrWhiteSpace($readme) -or -not ($archive.Entries.FullName -contains $readme)) {
            throw "Package '$id' does not contain its declared README '$readme'."
        }
    }
    finally {
        $archive.Dispose()
    }
}

Write-Host "Validated packages for version ${ExpectedVersion}: $($expectedIds -join ', ')"
