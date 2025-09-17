# Airport Management System

An interactive airport management system demonstrating real-time flight control and monitoring.

---

## Table of Contents

- [Features](#features)
- [Project Structure](#project-structure)
- [Quick Start](#quick-start)
  - [Local Installation](#local-installation)
  	- [Automated Script](#automated-script)
    - [Manual Commands](#manual-commands)
  - [Docker Installation](#docker-installation)
  - [Cloud Deployment](#cloud-deployment)
- [Usage](#usage)
- [Configuration](#configuration)
- [Testing](#testing)
- [Troubleshooting](#troubleshooting)

---

## Features

- Real-time flight tracking and airport visualization
- SignalR-powered live updates between backend and frontend
- MongoDB-based persistence
- Angular frontend (v16+)
- .NET 7 backend with clean, layered architecture
- Comprehensive unit and integration tests
- Simulator for generating demo flight data

---

## Project Structure

```
Airport.Web/           # ASP.NET Core backend (API, SignalR, DI, etc.)
Airport.Client/        # Angular frontend (UI, services, components)
Airport.Domain/        # Domain logic (entities, business rules)
Airport.Models/        # Shared models and DTOs
Airport.Services/      # Application services (business logic)
Airport.Persistence/   # Data access (MongoDB repositories)
Airport.Simulator/     # Flight and airport simulation
docs/                  # Documentation
scripts/               # Setup and utility scripts
docker-compose.yml     # Multi-container orchestration
Dockerfile             # Backend Docker build
Dockerfile.client      # Frontend Docker build
```

---

## Quick Start

### Local Installation

For local installation you can either run the automated script or manually:

#### Automated Script

- Prerequisites: [.NET 7 SDK](https://dotnet.microsoft.com/download), [Node.js 16+](https://nodejs.org/), [npm](https://www.npmjs.com/), [Docker](https://www.docker.com/)
- Open PowerShell and run:
```sh
.\local-setup.ps1
```
- The script will:
  - Install frontend dependencies
  - Start a MongoDB container
  - Build and run the backend
  - Start the Angular client

#### Manual Commands

- Prerequisites: [.NET 7 SDK](https://dotnet.microsoft.com/download), [Node.js 16+](https://nodejs.org/), [npm](https://www.npmjs.com/), [MongoDB](https://www.mongodb.com/try/download/community)
- Clone the repository:
```sh
git clone https://github.com/Elitsafan/AirportRevised.git
cd AirportRevised
```
- Install frontend dependencies:
```sh
cd Airport.Client
npm install
cd ..
```
- Start MongoDB (if not already running):
```sh
docker run -d -p 27017:27017 --name airport-mongo mongo:latest
```
- Build and run backend:
```sh
dotnet build
dotnet run --project Airport.Web
```
- Start the frontend:
```sh
cd Airport.Client
npm start
```
- Access the app at [http://localhost:4200](http://localhost:4200)

### Docker Installation

- Ensure [Docker](https://www.docker.com/) is installed.
- Run:
```sh
docker-compose up --build
```
- Frontend: [http://localhost:4200](http://localhost:4200)
- Backend API: [http://localhost:5000](http://localhost:5000)

### Cloud Deployment

- Deploy backend to Azure App Service or AWS Elastic Beanstalk.
- Deploy frontend to Azure Static Web Apps, AWS S3, or Netlify.
- Use managed MongoDB (e.g., MongoDB Atlas).
- See `.github/workflows/azure-deploy.yml` for CI/CD example.

---

## Usage

- **Frontend:**
- Open [http://localhost:4200](http://localhost:4200)
- View real-time flight and airport status
- Interact with the UI to simulate flights

- **API:**
- Swagger UI available at [http://localhost:5000/swagger](http://localhost:5000/swagger)
- REST endpoints for airport, flights, and simulation

- **Simulator:**
- Run the simulator to generate demo data:
```sh
dotnet run --project Airport.Simulator
```

---

## Configuration

- **Backend:**
- `Airport.Web/appsettings.json` for API and database settings
- Set `ConnectionStrings:Default` for MongoDB connection

- **Frontend:**
- `Airport.Client/src/environments/environment.ts` for API base URL

- **Docker:**
- Edit `docker-compose.yml` for port and environment variable overrides

---

## Testing

- **Backend:**
- Run all tests:
```sh
dotnet test
```
- Test projects: `Airport.Services.Tests`, `Airport.Presentation.Tests`

- **Frontend:**
- Unit tests:
```sh
npm test
```
- E2E tests (if implemented):
```sh
npm run e2e
```

---

## Troubleshooting

- **MongoDB connection issues:**
- Ensure MongoDB is running and accessible at the configured address.
- **Port conflicts:**
- Change ports in `docker-compose.yml` or `launchSettings.json`.
- **CORS errors:**
- Check CORS settings in `Airport.Web/Program.cs`.

---

## Contact

For questions or demo requests, contact [elitsafan@gmail.com](mailto:elitsafan@gmail.com).
