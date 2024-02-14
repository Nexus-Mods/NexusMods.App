Set-StrictMode -Version 'Latest'
$ErrorActionPreference = "Stop"
$ProgressPreference = 'SilentlyContinue' #'Continue

$rootDir = Resolve-Path "."
$downloadUrl = "https://www.ssl.com/download/codesigntool-for-windows/"
$downloadedFile = Join-Path $rootDir "CodeSignTool.zip"
$extractFolder = Join-Path $rootDir "CodeSignTool"

Write-Host "rootDir $rootDir"
Write-Host "downloadedFile $downloadedFile"
Write-Host "extractFolder $extractFolder"

# Remove extracted folder if exists, just in case (mainly used locally)
if (Test-Path $extractFolder) {
    Remove-Item -Path $extractFolder -Recurse -Force
}

# Download (if it doesn't exist)
if (!(Test-Path $downloadedFile -PathType Leaf)) {
    Invoke-WebRequest -OutFile $downloadedFile $downloadUrl
}

# Extract
Expand-Archive -Path $downloadedFile -DestinationPath $extractFolder -Force

# need to check for a nested single folder as 1.2.7 was packaged without this, all previous versions were not.
$folderCount = @(Get-ChildItem $extractFolder -Directory ).Count;

# if we have a single folder, then assume we have a nested folder that we need to fix
If ($folderCount -eq 1) {

    # get nested folder path, there is only 1 at this point
    $nestedFolderPath = (Get-ChildItem $extractFolder -Directory | Select-Object FullName)[0].FullName

    Write-Host "nestedFolderPath $nestedFolderPath"

    # move all child items from this nested folder to it's parent
    Get-ChildItem -Path $nestedFolderPath -Recurse | Move-Item -Destination $extractFolder

    # remove nested folder to keep it clean
    Remove-Item -Path $nestedFolderPath -Force
}

echo "codeSignToolDir=$extractFolder" >> $env:GITHUB_OUTPUT
