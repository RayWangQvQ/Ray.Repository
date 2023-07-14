# Set strict mode
Set-StrictMode -Version Latest

# Change directory to parent
Set-Location ..

# Create bin/nupkgs folder if not exists
New-Item -ItemType Directory -Path ./bin/nupkgs -Force

# Remove all files in bin/nupkgs folder
Remove-Item -Path ./bin/nupkgs/* -Recurse -Force

# Pack the project and output to bin/nupkgs folder
dotnet pack ./src/Ray.Repository/Ray.Repository.csproj --output bin/nupkgs
dotnet pack ./src/Ray.Repository.EntityFramework/Ray.Repository.EntityFramework.csproj --output bin/nupkgs