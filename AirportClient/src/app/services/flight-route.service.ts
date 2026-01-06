import { Injectable, OnDestroy } from '@angular/core';
import { Observable, Subscription, last, map } from 'rxjs';
import { AirportService } from './airport.service';
import { IRoute } from '../interfaces/iroute.interface';
import { StationService } from './station.service';
import { FlightRoute } from '../flight-route-module/models/flight-route.model';
import { Station } from '../flight-route-module/models/station.model';
import { Leg } from '../flight-route-module/models/leg.model';
import { BaseAirportDataService } from './base-airport-data.service';

@Injectable({
  providedIn: 'root'
})

export class FlightRouteService extends BaseAirportDataService<FlightRoute[]> implements OnDestroy {
  private flightRoutes: FlightRoute[];
  private stationSvcSubscription?: Subscription;
  private stations: Station[];

  constructor(
    airportSvc: AirportService,
    private stationSvc: StationService) {
    super(airportSvc, []);
    this.flightRoutes = [];
    this.stations = [];
    this.initialize();
  }

  private rawRoutes: IRoute[] = [];

  private buildFlightRoutes() {
    if (!this.rawRoutes || this.rawRoutes.length === 0 || this.stations.length === 0) {
      this.flightRoutes = [];
    } else {
      this.flightRoutes = this.rawRoutes.map(route => {
        // Find station objects in the current stations array
        const routeStations = [...new Set(route.directions.flatMap(d => [d.from, d.to]))]
          .map(stationId => this.stations.find(s => s.stationId === stationId)!)
          // Filter out undefined if a station is missing (safety check)
          .filter(s => !!s);

        const legs = [this.getNextLeg(route, routeStations)];
        routeStations.forEach((station, i, arr) => {
          const result = this.getNextLeg(route, arr, station);
          if (result.stations.length && legs
            .flatMap(leg => leg.stations)
            .every(station => !result.stations.includes(station)))
            legs.push(result)
        });
        return new FlightRoute(route.routeId, route.routeName, legs);
      });
    }
    this.dataSubject.next(this.flightRoutes);
  }

  public startService(): void {
    this.handleStationsSubscription();
  }

  get flightRoutes$(): Observable<FlightRoute[]> {
    return this.data$;
  }

  get flightRoutesError$() {
    return this.error$;
  }

  ngOnDestroy(): void {
    this.stationSvcSubscription?.unsubscribe();
  }

  protected fetchData(): void {
    this.airportSvc.getStatus()
      .pipe(map(status => status.routes), last())
      .subscribe({
        next: (routes: IRoute[]) => {
          this.rawRoutes = routes || [];
          this.buildFlightRoutes();
        },
        error: err => {
          console.error(err);
          this.errorSubject.next(err);
        }
      })
  }

  private getNextLeg(route: IRoute, stations: Station[], station?: Station): Leg {
    if (!station) {
      const tos = [...new Set(route.directions.map(d => d.to))];
      return new Leg(stations.filter(s => !tos.includes(s.stationId)));
    }
    return new Leg([...new Set(stations.filter(
      s => route.directions.find(d => d.from === station.stationId && d.to === s.stationId)))]);
  }

  private handleStationsSubscription() {
    this.stationSvcSubscription = this.stationSvc.stations$?.subscribe({
      next: stations => {
        this.stations = stations || [];
        this.buildFlightRoutes();
      },
      error: err => console.log(err)
    });

    // Also listen for station errors
    this.stationSvc.stationsError$?.subscribe(err => {
      // Propagate error to our error stream
      this.errorSubject.next(err);
    });
  }
}
