$projectDir = Split-Path $script:MyInvocation.MyCommand.Path
$binPath = Join-Path $projectDir "bin"

dotnet restore --source "https://api.nuget.org/v3/index.json" --source $binPath -p:Configuration=Release
dotnet pack --configuration Release --output bin --no-restore