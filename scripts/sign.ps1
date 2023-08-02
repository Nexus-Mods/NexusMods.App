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

# https://github.com/actions/runner-images/blob/main/images/win/Windows2022-Readme.md#installed-windows-sdks
$rootDirectory = "C:\Program Files (x86)\Windows Kits\10\bin\";
$sdkDirectory = Get-ChildItem -Path $rootDirectory -Name | Where-Object { $_ -like "10*" } | Sort-Object -Descending | Select-Object -First 1
Write-Host "Sdk Directory: $sdkDirectory"

$signToolPath = [System.IO.Path]::Combine($rootDirectory, $sdkDirectory, "x64", "signtool.exe")
Write-Host "signtool path: $signToolPath"

if (Test-Path $signToolPath -PathType Leaf) {
    Write-Host "Found signtool.exe at: $signToolPath";
} else {
    Write-Error "Singing tool at $signToolPath doesn't exist!";
    exit 1;
}

Write-Host "Signing $executableToSign";

& $signToolPath sign /f "$env:SigningCertificate" /p "$env:SigningCertificatePassword" /td sha256 /fd sha256 /tr "$env:TimestampServer" $executableToSign
$exitCode = $LASTEXITCODE

if ($exitCode -eq 0) {
    Write-Host "Signing completed"
} else {
    Write-Error "Signing failed with code $exitCode"
    exit $exitCode
}
