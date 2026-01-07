import { Component, OnInit, OnDestroy } from '@angular/core';
import { Observable, Subscription } from 'rxjs';
import { FlightRouteService } from '../../../services/flight-route.service';
import { StationService } from '../../../services/station.service';
import { FlightRoute } from '../../models/flight-route.model';

@Component({
  selector: 'flight-route-list',
  templateUrl: './flight-route-list.component.html',
  styleUrls: ['./flight-route-list.component.scss']
})
export class FlightRouteListComponent implements OnInit, OnDestroy {
  private flightRoutesSubscription?: Subscription;
  private flightRoutesErrorSubscription?: Subscription;

  flightRoutes$: Observable<FlightRoute[]>;
  routes: FlightRoute[] = [];
  loading: boolean = false;
  errorMessage: string | null = null;

  constructor(
    private stationSvc: StationService,
    private flightRouteSvc: FlightRouteService) {
    this.flightRoutes$ = this.flightRouteSvc.flightRoutes$;
  }

  ngOnInit(): void {
    this.flightRoutesErrorSubscription = this.flightRouteSvc.flightRoutesError$
      .subscribe(err => {
        this.loading = false;
        this.errorMessage = "Unable to fetch routes. Please try again.";
      });
    this.loading = true;
    this.stationSvc.startService();
    this.flightRouteSvc.startService();
    this.flightRoutesSubscription = this.flightRoutes$.subscribe({
      next: routes => {
        this.routes = routes;
        this.loading = false;
      },
      error: err => {
        this.loading = false;
        this.errorMessage = "Unable to fetch routes. Please try again.";
        console.error(err);
      }
    });
  }

  ngOnDestroy(): void {
    this.flightRoutesSubscription?.unsubscribe();
    this.flightRoutesErrorSubscription?.unsubscribe();
  }

  onRetry() {
    this.loading = true;
    this.errorMessage = null;
    this.stationSvc.startService();
    this.flightRouteSvc.startService();
  }
}
