[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$resolver = Join-Path $PSScriptRoot 'Resolve-Version.ps1'

$cases = @(
    @{ Base = '0.1.16'; Bump = 'Patch'; Channel = 'Stable'; Expected = '0.1.17' },
    @{ Base = '0.1.16'; Bump = 'Minor'; Channel = 'Stable'; Expected = '0.2.0' },
    @{ Base = '0.1.16'; Bump = 'Major'; Channel = 'Stable'; Expected = '1.0.0' },
    @{ Base = '1.2.3'; Bump = 'None'; Channel = 'Local'; Expected = '1.2.3-local' },
    @{ Base = '1.2.3'; Bump = 'Minor'; Channel = 'Dev'; Run = '42'; Sha = 'ABCDEF0123456789'; Expected = '1.3.0-dev.42.abcdef0' },
    @{ Base = '1.2.3'; Bump = 'Patch'; Channel = 'Rc'; Run = '7'; Sha = '0123456789abcdef'; Expected = '1.2.4-rc.7.0123456' }
)

foreach ($case in $cases) {
    $arguments = @{
        BaseVersion = $case.Base
        Bump = $case.Bump
        Channel = $case.Channel
    }

    if ($case.Run) {
        $arguments.RunNumber = $case.Run
        $arguments.CommitSha = $case.Sha
    }

    $actual = & $resolver @arguments
    if ($actual -cne $case.Expected) {
        throw "Expected '$($case.Expected)' but got '$actual'."
    }
}

$failedAsExpected = $false
try {
    & $resolver -BaseVersion '01.2.3' | Out-Null
}
catch {
    $failedAsExpected = $true
}

if (-not $failedAsExpected) {
    throw 'An invalid base version did not fail.'
}

Write-Host "Validated $($cases.Count) version cases and invalid-input handling."
