[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$BaseVersion,

    [ValidateSet('None', 'Patch', 'Minor', 'Major')]
    [string]$Bump = 'None',

    [ValidateSet('Stable', 'Local', 'Dev', 'Rc')]
    [string]$Channel = 'Stable',

    [string]$RunNumber,

    [string]$CommitSha
)

$ErrorActionPreference = 'Stop'

if ($BaseVersion -cnotmatch '^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)$') {
    throw "BaseVersion '$BaseVersion' must be a strict MAJOR.MINOR.PATCH version."
}

$major = [uint64]$Matches[1]
$minor = [uint64]$Matches[2]
$patch = [uint64]$Matches[3]

switch ($Bump) {
    'Major' {
        $major++
        $minor = 0
        $patch = 0
    }
    'Minor' {
        $minor++
        $patch = 0
    }
    'Patch' {
        $patch++
    }
}

$version = "$major.$minor.$patch"

switch ($Channel) {
    'Local' {
        $version = "$version-local"
    }
    { $_ -in @('Dev', 'Rc') } {
        if ($RunNumber -cnotmatch '^[1-9]\d*$') {
            throw "RunNumber is required for the $Channel channel and must be a positive integer."
        }

        if ($CommitSha -cnotmatch '^[0-9a-fA-F]{7,40}$') {
            throw "CommitSha is required for the $Channel channel and must contain 7 to 40 hexadecimal characters."
        }

        $label = $Channel.ToLowerInvariant()
        $shortSha = $CommitSha.Substring(0, 7).ToLowerInvariant()
        $version = "$version-$label.$RunNumber.$shortSha"
    }
}

$version
