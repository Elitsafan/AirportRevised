import { Injectable, OnDestroy } from "@angular/core";
import { Observable, Subscription } from "rxjs";
import { AirportService } from "./airport.service";
import { ColorService } from "./color.service";
import { Flight } from "../flight-module/models/flight.model.ts";
import { HttpParams } from "@angular/common/http";
import { IFlight } from "../interfaces/iflight.interface";
import { BaseAirportDataService } from "./base-airport-data.service";

@Injectable({
  providedIn: 'root'
})
export class FlightService extends BaseAirportDataService<Flight[]> implements OnDestroy {
  private flights: Flight[] = [];
  private statusSubscription?: Subscription;

  constructor(
    airportSvc: AirportService,
    private colorSvc: ColorService
  ) {
    super(airportSvc, []);
  }

  get flights$(): Observable<Flight[]> {
    return this.data$;
  }

  get flightRoutesError$() {
    return this.error$;
  }

  ngOnDestroy(): void {
    this.statusSubscription?.unsubscribe();
  }

  updateFlights(params?: HttpParams) {
    this.fetchData(params);
  }

  protected fetchData(params?: HttpParams) {
    this.airportSvc.getFlights(params)
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
          this.dataSubject.next(this.flights);
        },
        error: err => {
            console.log(err);
            this.errorSubject.next(err);
        }
      });
  }
}
