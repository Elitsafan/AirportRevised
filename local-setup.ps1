# Setup script for local development
Write-Host "Setting up Airport Management System..."

# Check prerequisites
$prerequisites = @{
    "dotnet" = "7.0"
    "node" = "16.0"
    "npm" = "8.0"
    "docker" = "20.0"
}

# Install dependencies
Write-Host "Installing dependencies..."
Set-Location ./Airport.Client
npm install
Set-Location ..

# Setup database
Write-Host "Setting up MongoDB..."
docker run -d -p 27017:27017 --name airport-mongo mongo:latest

# Build and run
dotnet build
Start-Process -FilePath "dotnet" -ArgumentList "run --project Airport.Web"
Set-Location ./Airport.Client
Start-Process -FilePath "$env:APPDATA\npm\npm.cmd" -ArgumentList "start"