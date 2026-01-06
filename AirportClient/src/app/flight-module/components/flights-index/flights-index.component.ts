import { Component, OnDestroy, OnInit } from '@angular/core';
import { Subscription } from 'rxjs';
import { FlightService } from '../../../services/flight.service';
import { FlightType } from '../../../types/flight.type';
import { IFlight } from '../../../interfaces/iflight.interface';
import { HttpParams } from '@angular/common/http';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'flights-index',
  templateUrl: './flights-index.component.html',
  styleUrls: ['./flights-index.component.scss']
})
export class FlightsIndexComponent implements OnInit, OnDestroy {
  private flightRoutesErrorSubscription?: Subscription;
  private flightsSubscription?: Subscription;
  flights: IFlight[];
  loading: boolean;
  departure: FlightType;
  landing: FlightType;

  errorMessage: string | null = null;

  constructor(private flightSvc: FlightService) {
    this.flights = [];
    this.loading = false;
    this.departure = "Departure";
    this.landing = "Landing";
  }

  ngOnInit(): void {
    this.loading = true;
    this.flightRoutesErrorSubscription = this.flightSvc.flightRoutesError$
      .subscribe(err => {
        this.loading = false;
        this.errorMessage = "Unable to fetch flights. Please try again.";
      });
    this.flightsSubscription = this.flightSvc.flights$.subscribe(flights => {
      this.flights = flights;
      this.loading = false;
    })
  }

  ngOnDestroy(): void {
    this.flightsSubscription?.unsubscribe();
    this.flightRoutesErrorSubscription?.unsubscribe();
  }

  onRefresh(event?: Event) {
    this.loading = true;
    this.errorMessage = null;
    this.flightSvc.updateFlights(new HttpParams().set("minutesPassed", environment.flightRefreshMinutes));
  }
}
