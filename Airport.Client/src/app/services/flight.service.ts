import { Injectable, OnDestroy } from "@angular/core";
import { Observable, Subject, Subscription } from "rxjs";
import { AirportService } from "./airport.service";
import { ColorService } from "./color.service";
import { Flight } from "../flight-module/models/flight.model.ts";
import { HttpParams } from "@angular/common/http";
import { IFlight } from "../interfaces/iflight.interface";

@Injectable({
  providedIn: 'root'
})

export class FlightService implements OnDestroy {
  private flights: Flight[];
  private statusSubscription?: Subscription;
  private flightRoutesErrorSubject = new Subject<any>();
  private flightsSubject: Subject<Flight[]>;

  constructor(
    private airportSvc: AirportService,
    private colorSvc: ColorService
  ) {
    this.flights = [];
    this.flightsSubject = new Subject<Flight[]>();
    if (!this.airportSvc.hasStarted)
      this.airportSvc.start()
        .subscribe({
          next: _ => { },
          error: err => this.flightRoutesErrorSubject.next(err)
        });
  }

  get flights$(): Observable<Flight[]> {
    return this.flightsSubject.asObservable();
  }

  get flightRoutesError$() {
    return this.flightRoutesErrorSubject.asObservable();
  }

  ngOnDestroy(): void {
    this.statusSubscription?.unsubscribe();
  }

  updateFlights(params?: HttpParams) {
    this.fetch(params);
  }

  private fetch(params?: HttpParams) {
    return this.airportSvc.getFlights(params)
      .subscribe({
        next: (flights: IFlight[]) => {
          const newFlights = flights.map(flight => new Flight(
            flight.flightId!,
            flight.flightType,
            this.colorSvc.getColor(
              flight.flightId,
              flight.flightType)));
          this.flights = [...newFlights];
          // Triggers initial flights
          this.flightsSubject.next(this.flights);
        },
        error: err => console.log(err)
      });
  }
}
