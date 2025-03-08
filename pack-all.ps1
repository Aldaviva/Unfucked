$binPath = [System.IO.Path]::GetFullPath("$env:USERPROFILE/.nuget/local/")
New-Item -Type Directory -Force "~/.nuget/local" > $null

dotnet restore --source "https://api.nuget.org/v3/index.json" --source $binPath -p:Configuration=Release
dotnet pack Unfucked --configuration Release --output $binPath --no-restore
dotnet restore --source "https://api.nuget.org/v3/index.json" --source $binPath -p:Configuration=Release
dotnet pack --configuration Release --output $binPath --no-restore

Remove-Item -Path "~\.nuget\packages\unfucked*" -Recurse
echo "Packed libraries into $binPath"