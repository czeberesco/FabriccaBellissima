[CmdletBinding()]
param(
    [ValidateSet('All', 'EditMode', 'PlayMode')]
    [string]$TestPlatform = 'All',

    [string]$UnityEditor = $env:UNITY_EDITOR
)

$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$projectPath = (Resolve-Path -LiteralPath (Join-Path $repositoryRoot 'FabriccaBellissima')).Path
$versionFile = Join-Path $projectPath 'ProjectSettings/ProjectVersion.txt'
$versionMatch = Select-String -LiteralPath $versionFile -Pattern '^m_EditorVersion:\s*(.+)$'
if (-not $versionMatch) {
    throw "Could not read the required Unity version from $versionFile"
}

$unityVersion = $versionMatch.Matches[0].Groups[1].Value.Trim()

function Resolve-UnityEditor {
    param(
        [string]$RequestedEditor,
        [string]$RequiredVersion
    )

    if ($RequestedEditor) {
        $resolvedEditor = (Resolve-Path -LiteralPath $RequestedEditor -ErrorAction Stop).Path
        return $resolvedEditor
    }

    $userProfilePath = [Environment]::GetFolderPath('UserProfile')
    $candidates = [System.Collections.Generic.List[string]]::new()

    if ($env:OS -eq 'Windows_NT') {
        if ($env:ProgramFiles) {
            $candidates.Add((Join-Path $env:ProgramFiles "Unity/Hub/Editor/$RequiredVersion/Editor/Unity.exe"))
        }
        if (${env:ProgramFiles(x86)}) {
            $candidates.Add((Join-Path ${env:ProgramFiles(x86)} "Unity/Hub/Editor/$RequiredVersion/Editor/Unity.exe"))
        }
    }
    elseif ($IsMacOS) {
        $candidates.Add("/Applications/Unity/Hub/Editor/$RequiredVersion/Unity.app/Contents/MacOS/Unity")
        $candidates.Add((Join-Path $userProfilePath "Applications/Unity/Hub/Editor/$RequiredVersion/Unity.app/Contents/MacOS/Unity"))
    }
    else {
        $candidates.Add((Join-Path $userProfilePath "Unity/Hub/Editor/$RequiredVersion/Editor/Unity"))
        $candidates.Add("/opt/unity/editors/$RequiredVersion/Editor/Unity")
    }

    foreach ($candidate in $candidates) {
        if (Test-Path -LiteralPath $candidate -PathType Leaf) {
            return (Resolve-Path -LiteralPath $candidate).Path
        }
    }

    throw "Unity $RequiredVersion was not found in a standard Unity Hub location. Set UNITY_EDITOR or pass -UnityEditor with the editor executable path."
}

function Invoke-UnityTests {
    param(
        [string]$EditorPath,
        [string]$Mode,
        [string]$UnityProjectPath,
        [string]$OutputDirectory
    )

    $modeName = $Mode.ToLowerInvariant()
    $arguments = @(
        '-batchmode',
        '-nographics',
        '-projectPath', $UnityProjectPath,
        '-runTests',
        '-testPlatform', $Mode,
        '-testResults', (Join-Path $OutputDirectory "$modeName-results.xml"),
        '-logFile', (Join-Path $OutputDirectory "$modeName.log")
    )

    Write-Host "Running $Mode tests with Unity $unityVersion..."
    if ($env:OS -eq 'Windows_NT') {
        $processArguments = $arguments | ForEach-Object {
            if ($_ -match '\s') { '"' + $_.Replace('"', '\"') + '"' } else { $_ }
        }
        $process = Start-Process -FilePath $EditorPath -ArgumentList $processArguments `
            -Wait -PassThru -WindowStyle Hidden
        $exitCode = $process.ExitCode
    }
    else {
        & $EditorPath @arguments
        $exitCode = $LASTEXITCODE
    }

    if ($exitCode -ne 0) {
        throw "$Mode tests failed with Unity exit code $exitCode. See $(Join-Path $OutputDirectory "$modeName.log")"
    }
}

$resolvedUnityEditor = Resolve-UnityEditor -RequestedEditor $UnityEditor -RequiredVersion $unityVersion
$resultsDirectory = Join-Path $repositoryRoot 'TestResults'
New-Item -ItemType Directory -Path $resultsDirectory -Force | Out-Null

$projectLock = Join-Path $projectPath 'Temp/UnityLockfile'
if (Test-Path -LiteralPath $projectLock) {
    throw "The Unity project is already open. Close that editor before running automated tests: $projectPath"
}

$platforms = if ($TestPlatform -eq 'All') { @('EditMode', 'PlayMode') } else { @($TestPlatform) }
foreach ($platform in $platforms) {
    Invoke-UnityTests -EditorPath $resolvedUnityEditor -Mode $platform `
        -UnityProjectPath $projectPath -OutputDirectory $resultsDirectory
}

Write-Host "All requested tests passed. Results: $resultsDirectory"
