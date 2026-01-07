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
Set-Location ./AirportClient
npm install
Set-Location ..

# Setup database
Write-Host "Setting up MongoDB..."
if (!(docker ps -a --filter "name=airport-mongo" --format '{{.Names}}')) {
    docker run -d -p 27017:27017 --name airport-mongo mongo:latest
} else {
    docker start airport-mongo
}

# Build the solution
Write-Host "Building the solution..."
dotnet build

# Launch Terminal with 3 tabs
if (Get-Command wt.exe -ErrorAction SilentlyContinue) {
    Write-Host "Opening Backend, Simulator, and Client in Windows Terminal tabs..."

    wt.exe `
      -w 0 nt --title "Backend API" -d "$PSScriptRoot" powershell -NoExit -Command "dotnet run --project Airport.Web --launch-profile localScript" `; `
      nt --title "Simulator" -d "$PSScriptRoot" powershell -NoExit -Command "dotnet run --project Airport.Simulator --launch-profile localScript" `; `
      nt --title "Angular Client" -d "$PSScriptRoot/AirportClient" powershell -NoExit -Command "npm start"
} else {
    Write-Warning "Windows Terminal (wt.exe) not found. Falling back to separate windows."

    Start-Process powershell -ArgumentList "-NoExit", "-Command", "dotnet run --project Airport.Web --launch-profile localScript"
    Start-Process powershell -ArgumentList "-NoExit", "-Command", "dotnet run --project Airport.Simulator --launch-profile localScript"
    Set-Location ./AirportClient
    Start-Process powershell -ArgumentList "-NoExit", "-Command", "npm start"
    Set-Location ..
}

Write-Host "Setup script execution finished."