import { Component, OnDestroy, OnInit } from '@angular/core';
import { FlightRouteService } from '../../../services/flight-route.service';
import { Observable, Subscription } from 'rxjs';
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
  loading: boolean;

  constructor(
    private stationSvc: StationService,
    private flightRouteSvc: FlightRouteService) {
    this.flightRoutes$ = this.flightRouteSvc.flightRoutes$;
    this.loading = false;
  }

  ngOnDestroy(): void {
    this.flightRoutesSubscription?.unsubscribe();
    this.flightRoutesErrorSubscription?.unsubscribe();
  }

  public ngOnInit(): void {
    this.flightRoutesErrorSubscription = this.flightRouteSvc.flightRoutesError$
      .subscribe(_ => this.loading = false);
    this.loading = true;
    this.stationSvc.startService();
    this.flightRouteSvc.startService();
    this.flightRoutes$.subscribe({
      next: _ => this.loading = false,
      error: err => {
        this.loading = false;
        console.log(err);
      }
    });
  }
}
