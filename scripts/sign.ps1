if ($env:SignExecutable -ne "true") {
    Write-Host $env:SignExecutable
    Write-Host "Signing the executable has been disabled."
    exit 0
}

function TestFile {
    param (
        [string]$Path
    )

    if (Test-Path $Path -PathType Leaf) {
        Write-Host "File exists: $Path";
    } else {
        Write-Error "File doesn't exist: $Path";
        exit 1;
    }
}

function TestDirectory {
    param (
        [string]$Path
    )

    if (Test-Path $Path -PathType Container) {
        Write-Host "Directory exists: $Path";
    } else {
        Write-Error "Directory doesn't exist: $Path";
        exit 1;
    }
}

$searchDirectory = $args[0];
if ($searchDirectory) {
    Write-Host "Using search directory $searchDirectory"
    $executableToSign = Get-ChildItem -Path $searchDirectory | Where-Object { $_.Extension -eq ".exe" } | Select-Object -First 1
} else {
    $executableToSign = [System.IO.Path]::Combine($env:BUILD_APP_BIN, $env:APP_BASE_NAME + ".exe")
}

TestFile($executableToSign)

$codeSignToolDir = $env:CodeSignToolDir
TestDirectory($codeSignToolDir)

$javaPath = Join-Path -Path $codeSignToolDir -ChildPath "jdk-11.0.2" | Join-Path -ChildPath "bin" | Join-Path -ChildPath "java.exe"
TestFile($javaPath)

$jarPath = Join-Path -Path $codeSignToolDir -ChildPath "jar" | Join-Path -ChildPath "code_sign_tool-1.3.0.jar"
TestFile($jarPath)

# CodeSignTool requires user interaction to confirm an overwrite of the original file.
# We circumvent this by setting the output directory to some temp directory and replacing
# the original file with the newly signed file.

$tmpDir = Join-Path -Path $(Resolve-Path .) -ChildPath "tmp"
if (Test-Path $tmpDir -PathType Container) {
    Remove-Item -Path $tmpDir -Recurse -Force
}

New-Item -Path $tmpDir -Type Directory

$inputFile = "$executableToSign"
$outputFile = Join-Path -Path $tmpDir -ChildPath $(Get-Item $inputFile).Name

Write-Host "inputFile: $inputFile"
Write-Host "outputFile: $outputFile"
Write-Host "outputDir: $tmpDir"

TestFile($inputFile)
TestDirectory($tmpDir)

Set-Location $codeSignToolDir

& $javaPath -jar $jarPath sign -input_file_path="$inputFile" -output_dir_path="$tmpDir" -username="$env:ES_USERNAME" -password="$env:ES_PASSWORD" -credential_id="$env:ES_CREDENTIAL_ID" -totp_secret="$env:ES_TOTP_SECRET"
$exitCode = $LASTEXITCODE

if ($exitCode -eq 0) {
    Write-Host "Signing completed"
} else {
    Write-Error "Signing failed with code $exitCode"
    exit $exitCode
}

TestFile($outputFile)

Write-Host "Moving $outputFile to $inputFile"
Move-Item -Path $outputFile -Destination $inputFile -Force

Remove-Item -Path $tmpDir -Recurse -Force
