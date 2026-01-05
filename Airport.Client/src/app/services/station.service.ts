import { BehaviorSubject, Observable, Subscription, map } from "rxjs";
import { IStationChangedData } from "../interfaces/istation-changed-data.interface";
import { Injectable, OnDestroy } from "@angular/core";
import { IFlight } from "../interfaces/iflight.interface";
import { Station } from "../flight-route-module/models/station.model";
import { Flight } from "../flight-module/models/flight.model.ts";
import { AirportService } from "./airport.service";
import { SignalrService } from "./signalr.service";
import { ColorService } from "./color.service";

@Injectable({
  providedIn: 'root'
})

export class StationService implements OnDestroy {
  private stations: Station[];
  private stationOccupiedSubscription?: Subscription;
  private stationClearedSubscription?: Subscription;
  private stationsSubject = new BehaviorSubject<Station[]>([]);
  stations$?: Observable<Station[]>;

  constructor(
    private airportSvc: AirportService,
    private signalRSvc: SignalrService,
    private colorSvc: ColorService) {
    this.stations = [];
    this.stations$ = this.stationsSubject.asObservable();
  }

  public startService(): void {
    if (this.airportSvc.hasStarted) {
      this.handleStationsSubscription();
      this.fetch();
    } else {
      this.airportSvc.start()
        .subscribe({
          next: () => {
            this.handleStationsSubscription();
            this.fetch();
          },
          error: (error) => console.error(error)
        });
    }
  }

  ngOnDestroy(): void {
    this.stationOccupiedSubscription?.unsubscribe();
    this.stationClearedSubscription?.unsubscribe();
  }

  private flightResolver(flight?: IFlight, stationId?: string) {
    return flight
      ? new Flight(
        flight.flightId,
        flight.flightType,
        this.colorSvc.getColor(flight.flightId, flight.flightType),
        stationId)
      : undefined;
  }

  private fetch(): void {
    this.airportSvc.getStatus()
      .pipe(map(status => status.stations))
      .subscribe({
        next: stations => {
          this.stations = stations.map(station => new Station(station.stationId));
          this.stationsSubject.next(this.stations);
        },
        error: error => console.error(error)
      });
  }

  private handleStationsSubscription() {
    this.stationOccupiedSubscription = this.signalRSvc.stationOccupiedData$
      ?.subscribe(data => this.stationSubscriptionEventHandler(data))
    this.stationClearedSubscription = this.signalRSvc.stationClearedData$
      ?.subscribe(data => this.stationSubscriptionEventHandler(data))
  }

  private stationSubscriptionEventHandler(data: IStationChangedData) {
    if (data && this.stations?.length) {
      // Update flights only, don't recreate the array
      data.forEach((changedStation, i) => {
        if (i < this.stations!.length) {
          this.stations![i].flight = this.flightResolver(
            changedStation.flight,
            changedStation.stationId
          );
        }
      });
      // Notify subscribers of the changes without creating a new array reference
      this.stationsSubject.next(this.stations);
    }
  }
}
