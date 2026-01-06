# Airport Client - Architecture & Development Guide

## Overview

Angular 16 application providing real-time airport visualization with SignalR integration and robust error handling.

## Architecture

### Data Flow

```
SignalR (real-time) ──┐
                      ├──> Services ──> Components ──> UI
HTTP (polling)     ───┘
```

**Services extend `BaseAirportDataService`:**
- Emit data via `BehaviorSubject` (immediate last value to new subscribers)
- Emit errors via `ReplaySubject` (buffer last error for late subscribers)
- Auto-initialize on construction

### Error Handling Strategy

**Error Propagation Chain:**

```
1. HTTP Failures → Service.errorSubject → Component.errorMessage
2. SignalR Disconnect → SignalrService.connectionError$ → StationService.errorSubject → FlightRouteService.errorSubject → Component.errorMessage
```

**Key Pattern:**
- Components **never clear errors** on data emission (prevents stale data from hiding errors)
- Errors only clear on explicit retry/refresh

### Real-Time Updates

**SignalR Events:**
- `StationOccupiedAsync` - Flight arrives at station
- `StationClearedAsync` - Flight leaves station
- `FlightRunDone` - Flight completes journey

**Connection Loss Handling:**
- `.onclose()` fires when server stops
- `connectionError$` emits to subscribers
- `StationService` → `FlightRouteService` → UI shows error

## Environment Configuration

**`src/environments/environment.ts`:**

```typescript
http: {
  retryCount: 5,        // HTTP retry attempts
  retryDelayMs: 5000    // Delay between retries (ms)
},
flightRefreshMinutes: 7  // Minutes of flight data to fetch on refresh
```

**Production vs Development:**
- `environment.ts` → Production (Azure)
- `environment.development.ts` → Local (`localhost:5005`)

## Development Workflow

### Running Tests

```bash
# Watch mode (dev)
npm test

# Single run (CI)
npm test -- --watch=false --browsers=ChromeHeadless
```

**Test Coverage:**
- Services: `AirportService`, `FlightService`, `StationService`, `SignalrService`, `FlightRouteService`, `FlightSummaryService`
- Components: `FlightsIndexComponent`, `FlightRouteListComponent`, `FlightSummaryListComponent`, `LoadingDisplayComponent`

### Building

```bash
# Development
ng serve

# Production
ng build --configuration production
```

### Deployment

**GitHub Actions CI/CD:**
1. Push to `development` branch
2. Tests run (`npm test -- --watch=false --browsers=ChromeHeadless`)
3. If tests pass → Build Angular app
4. If build succeeds → Deploy to Azure Static Web Apps

## Module Structure

```
app/
├── flight-module/          # Landing/Departure flights view
├── flight-route-module/    # Stations and routes visualization
├── flight-summary-module/  # Paginated flight history
├── shared-module/          # Pagination, common utilities
├── loading-module/         # Loading/error states
└── services/               # Data services
```

## Common Patterns

### Service Initialization

All data services:
1. Extend `BaseAirportDataService<T>`
2. Call `this.initialize()` in constructor
3. Implement `fetchData()` for HTTP calls
4. Emit to `dataSubject.next()` on success
5. Emit to `errorSubject.next()` on failure

### Component Error Handling

```typescript
ngOnInit() {
  this.loading = true;

  // Subscribe to errors first
  this.service.error$.subscribe(err => {
    this.loading = false;
    this.errorMessage = "Unable to fetch...";
  });

  // Then subscribe to data (DON'T clear errorMessage here!)
  this.service.data$.subscribe(data => {
    this.data = data;
    this.loading = false;
  });
}

onRetry() {
  this.loading = true;
  this.errorMessage = null;  // Only clear on explicit retry
  this.service.updateData();
}
```

## Troubleshooting

### SignalR Connection Issues
- Check browser console for "Connection started"
- Verify backend is running on `localhost:5005` (dev) or Azure URL (prod)

### Tests Failing
- Ensure Chrome is installed (Karma requires it)
- Check `connectionError$` is mocked in service specs

### Build Errors
- Run `npm install` if dependencies changed
- Clear `.angular/cache` if seeing stale build artifacts
