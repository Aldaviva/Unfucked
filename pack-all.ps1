$binPath = [System.IO.Path]::GetFullPath("$env:USERPROFILE/.nuget/local/")
New-Item -Type Directory -Force "~/.nuget/local" > $null

dotnet restore Unfucked --source "https://api.nuget.org/v3/index.json" --source $binPath -p:Configuration=Release
dotnet pack Unfucked --configuration Release --output $binPath --no-restore
dotnet restore --source "https://api.nuget.org/v3/index.json" --source $binPath -p:Configuration=Release
dotnet pack --configuration Release --output $binPath --no-restore

Remove-Item -Path "~\.nuget\packages\unfucked*" -Recurse
echo "Packed libraries into $binPath"
echo "Remember to run `dotnet restore --force-evaluate` in dependent projects"

# To push all packages, run (in ~/.nuget/local):
# Get-ChildItem "*.nupkg" | % { dotnet nuget push $_.FullName }