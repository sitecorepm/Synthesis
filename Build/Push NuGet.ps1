param(
    [Parameter(Mandatory=$true)] [string] $version,
    [Parameter(Mandatory=$true)] [string] $apiKey,
    [string] $source = "https://nexus.irt.drexel.edu/repository/nuget-releases/"
)
gci "versions\$version\*.nupkg" -exclude *.symbols.nupkg | % { dotnet nuget push $_.FullName --api-key $apiKey -s $source }