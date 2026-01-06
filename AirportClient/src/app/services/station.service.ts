import { BehaviorSubject, Observable, Subscription, map, ReplaySubject } from "rxjs";
import { IStationChangedData } from "../interfaces/istation-changed-data.interface";
import { Injectable, OnDestroy } from "@angular/core";
import { IFlight } from "../interfaces/iflight.interface";
import { Station } from "../flight-route-module/models/station.model";
import { Flight } from "../flight-module/models/flight.model";
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
  private connectionErrorSubscription?: Subscription;
  private stationsSubject = new BehaviorSubject<Station[]>([]);
  private errorSubject = new ReplaySubject<any>(1);
  stations$?: Observable<Station[]>;

  constructor(
    private airportSvc: AirportService,
    private signalRSvc: SignalrService,
    private colorSvc: ColorService) {
    this.stations = [];
    this.stations$ = this.stationsSubject.asObservable();
  }

  get stationsError$(): Observable<any> {
    return this.errorSubject.asObservable();
  }

  public startService(): void {
    /**
     * Subscribe to SignalR connection errors and propagate to components.
     * This ensures UI shows error messages when server disconnects.
     */
    if (!this.connectionErrorSubscription) {
      this.connectionErrorSubscription = this.signalRSvc.connectionError$
        .subscribe(error => {
          console.error('SignalR connection error:', error);
          this.errorSubject.next(error);
        });
    }

    if (this.airportSvc.hasStarted) {
      if (!this.stationOccupiedSubscription) {
        this.handleStationsSubscription();
      }
      this.fetch();
    } else {
      this.airportSvc.start()
        .subscribe({
          next: () => {
            if (!this.stationOccupiedSubscription) {
              this.handleStationsSubscription();
            }
            this.fetch();
          },
          error: (error) => {
            console.error(error);
            this.errorSubject.next(error);
          }
        });
    }
  }

  ngOnDestroy(): void {
    this.stationOccupiedSubscription?.unsubscribe();
    this.stationClearedSubscription?.unsubscribe();
    this.connectionErrorSubscription?.unsubscribe();
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
        error: error => {
          console.error(error);
          this.errorSubject.next(error);
        }
      });
  }

  private handleStationsSubscription() {
    this.stationOccupiedSubscription = this.signalRSvc.stationOccupiedData$
      ?.subscribe(data => this.stationSubscriptionEventHandler(data))
    this.stationClearedSubscription = this.signalRSvc.stationClearedData$
      ?.subscribe(data => this.stationSubscriptionEventHandler(data))
  }

  /**
   * Handles real-time station updates from SignalR.
   * Updates flight assignments for changed stations and notifies subscribers.
   *
   * @param data Array of station changes (occupied or cleared)
   */
  private stationSubscriptionEventHandler(data: IStationChangedData) {
    if (data && this.stations?.length) {
      // Update flights only, identifying the station by its ID
      data.forEach((changedStation) => {
        const stationToUpdate = this.stations.find(s => s.stationId === changedStation.stationId);
        if (stationToUpdate) {
          stationToUpdate.flight = this.flightResolver(
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
