# Setup script for local development
Write-Host "Setting up Airport Management System..."

# Check prerequisites
$prerequisites = @{
    "dotnet" = "8.0"
    "node" = "16.0"
    "npm" = "8.0"
    "docker" = "20.0"
}

# Install frontend dependencies
Write-Host "Installing frontend dependencies..."
Set-Location ./Airport.Client
npm install
Set-Location ..

# Setup database
Write-Host "Setting up MongoDB..."
docker run -d -p 27017:27017 --name airport-mongo mongo:latest

# Build the solution
Write-Host "Building the solution..."
dotnet build

# Run backend API
Write-Host "Starting backend API..."
Start-Process -FilePath "dotnet" -ArgumentList "run --project Airport.Web --launch-profile localScript"

# Run simulator
Write-Host "Starting simulator..."
Start-Process -FilePath "dotnet" -ArgumentList "run --project Airport.Simulator --launch-profile localScript"

# Start Angular client in development mode
Write-Host "Starting Angular client (Development)..."
Set-Location ./Airport.Client
Start-Process powershell -ArgumentList "-NoExit", "-Command", "npm start"
Set-Location ..