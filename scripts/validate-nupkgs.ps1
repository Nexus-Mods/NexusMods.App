# This script extracts all "*.nupkg" files and parses the ".nuspec" file.
# It looks at the dependencies of that package and alerts if a dependency is
# a local project in the current solution that isn't being packaged and
# pushed to NuGet. Such dependencies would break the current package and
# prevent consumers from using them.

$nupkgs = Get-ChildItem -Path "*" -Recurse -Include "*.nupkg"
$projects = Get-ChildItem -Path "*" -Recurse -Include "*.csproj"

$allProjects = [System.Collections.Generic.HashSet[string]]::new()
$allPackages = [System.Collections.Generic.HashSet[string]]::new()
$allDependencies = [System.Collections.Generic.HashSet[string]]::new()

# find all projects in the current solution
foreach ($item in $projects)
{
    $projectName = [System.IO.Path]::GetFileName([System.IO.Path]::GetDirectoryName($item));
    if ($allProjects.Add($projectName)) { }
}

# find all dependencies
foreach ($item in $nupkgs)
{
    $packageName = [System.IO.Path]::GetFileName(
            [System.IO.Path]::GetDirectoryName(
                    [System.IO.Path]::GetDirectoryName(
                            [System.IO.Path]::GetDirectoryName($item))))

    if ($allPackages.Add($packageName)) { }
    $extractedPath = [System.IO.Path]::Combine([System.IO.Path]::GetDirectoryName($item.FullName), "extracted")

    Expand-Archive -Path $item.FullName -DestinationPath $extractedPath -Force

    $nuspecFilePath = [System.IO.Path]::Combine($extractedPath, $packageName + ".nuspec")

    if (![System.IO.File]::Exists($nuspecFilePath)) {
        Write-Error "File $nuspecFilePath does not exist!"
        exit 1
    }

    # Select-Xml doesn't want to work for whatever reason
    #$dependencies = Select-Xml -Path $nuspecFilePath -XPath "//dependency"

    $xml = ([xml](Get-Content -Path $nuspecFilePath))
    $dependencies = $xml.ChildNodes[1].ChildNodes.dependencies.group.dependency.id

    foreach ($dep in $dependencies) {
        if ($allDependencies.Add($dep)) { }
    }
}

# only look at dependencies where the package is a local project
$allDependencies.IntersectWith($allProjects)

# remove all dependencies that are valid, meaning they are already being packaged
$allDependencies.ExceptWith($allPackages)

# the remaining items in allDependencies are packages which come from the local solution
# but haven't been packaged
foreach ($missing in $allDependencies) {
    Write-Error "The following project is a dependency that isn't packaged: $missing"
}

if ($allDependencies.Count -ne 0) {
    exit 1
} else {
    Write-Output "All packages are correct"
}
