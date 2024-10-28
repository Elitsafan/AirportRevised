import { Injectable, OnDestroy } from '@angular/core';
import { Observable, Subject, Subscription, last, map } from 'rxjs';
import { AirportService } from './airport.service';
import { IRoute } from '../interfaces/iroute.interface';
import { StationService } from './station.service';
import { FlightRoute } from '../flight-route-module/models/flight-route.model';
import { Station } from '../flight-route-module/models/station.model';
import { Leg } from '../flight-route-module/models/leg.model';

@Injectable({
  providedIn: 'root'
})

export class FlightRouteService implements OnDestroy {
  private flightRoutes: FlightRoute[];
  private flightRoutesSubject: Subject<FlightRoute[]>;
  private flightRoutesErrorSubject: Subject<any>;
  private stationSvcSubscription?: Subscription;
  private stations: Station[];
  flightRoutes$: Observable<FlightRoute[]>;

  constructor(
    private airportSvc: AirportService,
    private stationSvc: StationService) {
    this.flightRoutesSubject = new Subject<FlightRoute[]>();
    this.flightRoutesErrorSubject = new Subject<any>();
    this.flightRoutes = [];
    this.stations = [];
    this.flightRoutes$ = this.flightRoutesSubject.asObservable();
  }

  public startService(): void {
    if (this.airportSvc.hasStarted) {
      this.handleStationsSubscription();
      this.fetch();
    }
    else
      this.airportSvc.start()
        .subscribe({
          next: () => {
            this.handleStationsSubscription();
            this.fetch();
          },
          error: err => this.flightRoutesErrorSubject.next(err)
        });
  }

  get flightRoutesError$() {
    return this.flightRoutesErrorSubject.asObservable();
  }

  ngOnDestroy(): void {
    this.stationSvcSubscription?.unsubscribe();
  }

  private fetch(): void {
    this.airportSvc.getStatus()
      .pipe(map(status => status.routes), last())
      .subscribe({
        next: (routes: IRoute[]) => {
          //console.log(routes)
          this.flightRoutes = routes.map(route => {
            const routeStations = [...new Set(route.directions.flatMap(d => [d.from, d.to]))]
              .map(stationId => this.stations.find(s => s.stationId === stationId)!);
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
          this.flightRoutesSubject.next(this.flightRoutes);
        },
        error: err => console.error(err)
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
      next: stations => this.stations = stations,
      error: err => console.log(err)
    });
  }
}
