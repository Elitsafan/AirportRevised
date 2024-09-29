import { Component, OnDestroy, OnInit } from '@angular/core';
import { Subscription } from 'rxjs';
import { FlightService } from '../../../services/flight.service';
import { FlightType } from '../../../types/flight.type';
import { IFlight } from '../../../interfaces/iflight.interface';
import { HttpParams } from '@angular/common/http';

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

  constructor(private flightSvc: FlightService) {
    this.flights = [];
    this.loading = false;
    this.departure = "Departure";
    this.landing = "Landing";
  }

  ngOnInit(): void {
    this.loading = true;
    this.flightRoutesErrorSubscription = this.flightSvc.flightRoutesError$
      .subscribe(_ => this.loading = false);
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
    this.flightSvc.updateFlights(new HttpParams().set("minutesPassed", 7));
  }
}
