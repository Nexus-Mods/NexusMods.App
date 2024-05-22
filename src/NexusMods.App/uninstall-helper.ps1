param (
    [Parameter(Mandatory=$true)][string]$FilesToDeletePath,
    [Parameter(Mandatory=$true)][string]$DirectoriesToDeletePath
)

function DeleteFile {
    param ([string]$FilePath)

    if (Test-Path $FilePath) {
        Remove-Item $FilePath -Force -ErrorAction SilentlyContinue
    }
}

function DeleteDirectory {
    param ([string]$DirectoryPath)

    if (Test-Path $DirectoryPath) {
        Remove-Item $DirectoryPath -Recurse -Force -ErrorAction SilentlyContinue
    }
}

# Kill the App Client and Server
Stop-Process -Name "NexusMods.App" -Force -ErrorAction SilentlyContinue

# Note(Sewer) Ensure the process handles are freed, just in case.
Start-Sleep -Seconds 1

# Read the list of files to delete from the text file
$FilesToDelete = Get-Content $FilesToDeletePath

# Delete each file
foreach ($File in $FilesToDelete) {
    DeleteFile -FilePath $File
}

# Read the list of directories to delete from the text file
$DirectoriesToDelete = Get-Content $DirectoriesToDeletePath

# Delete each directory
foreach ($Directory in $DirectoriesToDelete) {
    DeleteDirectory -DirectoryPath $Directory
}
