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
  loading: boolean = false;

  constructor(
    private stationSvc: StationService,
    private flightRouteSvc: FlightRouteService) {
    this.flightRoutes$ = this.flightRouteSvc.flightRoutes$;
  }

  ngOnInit(): void {
    this.flightRoutesErrorSubscription = this.flightRouteSvc.flightRoutesError$
      .subscribe(_ => this.loading = false);
    this.loading = true;
    this.stationSvc.startService();
    this.flightRouteSvc.startService();
    this.flightRoutes$.subscribe({
      next: _ => this.loading = false,
      error: err => {
        this.loading = false;
        console.error(err);
      }
    });
  }

  ngOnDestroy(): void {
    this.flightRoutesSubscription?.unsubscribe();
    this.flightRoutesErrorSubscription?.unsubscribe();
  }
}
