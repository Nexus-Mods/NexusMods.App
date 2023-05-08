# This script extracts all "*.nupkg" files and parses the ".nuspec" file.
# It looks at the dependencies of that package and alerts if some dependencies
# of this organization are not available.

$Organization = "NexusMods"

$nupkgs = Get-ChildItem -Path "*" -Recurse -Include "*.nupkg"

$allPackages = [System.Collections.Generic.HashSet[string]]::new()
$allDependencies = [System.Collections.Generic.HashSet[string]]::new()

foreach ($item in $nupkgs)
{
    $packageName = [System.IO.Path]::GetFileName(
            [System.IO.Path]::GetDirectoryName(
                    [System.IO.Path]::GetDirectoryName(
                            [System.IO.Path]::GetDirectoryName($item))))

    $allPackages.Add($packageName)
    $extractedPath = [System.IO.Path]::Combine([System.IO.Path]::GetDirectoryName($item.FullName), "extracted")

    Expand-Archive -Path $item.FullName -DestinationPath $extractedPath -Force

    $nuspecFilePath = [System.IO.Path]::Combine($extractedPath, $packageName + ".nuspec")

    if (![System.IO.File]::Exists($nuspecFilePath)) {
        echo "File $nuspecFilePath does not exist!"
        exit 1
    }

    # Select-Xml doesn't want to work for whatever reason
    #$dependencies = Select-Xml -Path $nuspecFilePath -XPath "//dependency"

    $xml = ([xml](Get-Content -Path $nuspecFilePath))
    $dependencies = $xml.ChildNodes[1].ChildNodes.dependencies.group.dependency.id

    foreach ($dep in $dependencies) {
        if (!$dep.StartsWith($Organization)) {
            continue
        }

        $allDependencies.Add($dep)
    }
}

$allDependencies.ExceptWith($allPackages)

foreach ($missing in $allDependencies) {
    echo "The following package wasn't packed: $missing"
}

if ($allDependencies.Count -ne 0) {
    exit 1
} else {
    echo "All packages are correct"
}
