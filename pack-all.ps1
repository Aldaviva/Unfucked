dotnet restore --source "https://api.nuget.org/v3/index.json" --source ".\bin" -p:Configuration=Release
dotnet pack --configuration Release --output bin --no-restore