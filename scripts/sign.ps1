if ($env:SignExecutable -ne "true") {
    Write-Host $env:SignExecutable
    Write-Host "Signing the executable has been disabled."
    exit 0
}

$searchDirectory = $args[0];
if ($searchDirectory) {
    Write-Host "Using search directory $searchDirectory"
    $executableToSign = Get-ChildItem -Path $searchDirectory | Where-Object { $_.Extension -eq ".exe" } | Select-Object -First 1
} else {
    $executableToSign = [System.IO.Path]::Combine($env:BUILD_APP_BIN, $env:APP_BASE_NAME + ".exe")
}

Write-Host $executableToSign

if (Test-Path $executableToSign -PathType Leaf) {
    Write-Host "Signing $executableToSign";
} else {
    Write-Error "File $executableToSign doesn't exist!";
    exit 1;
}

$codeSignToolDir = $env:CodeSignToolDir
if (Test-Path $codeSignToolDir -PathType Container) {
    Write-Host "CodeSignTool directory $codeSignToolDir";
} else {
    Write-Error "CodeSignTool directory $codeSignToolDir doesn't exist!";
    exit 1;
}

$codeSignToolPath = Join-Path $codeSignToolDir "CodeSignTool"

# CodeSignTool requires user interaction to confirm an overwrite of the original file.
# We circumvent this by setting the output directory to some temp directory and replacing
# the original file with the newly signed file.

$tmpDir = Join-Path $(Resolve-Path .) "tmp"
if (Test-Path $tmpDir -PathType Container) {
    Remove-Item -Path $tmpDir -Recurse -Force
}

New-Item -Path $tmpDir -Type Directory

$inputFile = $executableToSign
$outputFile = Join-Path $tmpDir $(Get-Item $inputFile).Name

& $codeSignToolPath sign -input_file_path="$inputFile" -output_dir_path="$tmpDir" -username="$env:ES_USERNAME" -password="$env:ES_PASSWORD" -credential_id="$env:ES_CREDENTIAL_ID" -totp_secret="$env:ES_TOTP_SECRET"
$exitCode = $LASTEXITCODE

if ($exitCode -eq 0) {
    Write-Host "Signing completed"
} else {
    Write-Error "Signing failed with code $exitCode"
    exit $exitCode
}

Write-Host "Moving $outputFile to $inputFile"
Move-File -Path $outputFile -Destination $inputFile -Force

Remove-Item -Path $tmpDir -Recurse -Force
