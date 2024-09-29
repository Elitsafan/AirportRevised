import { Injectable } from '@angular/core';
import * as signalR from "@microsoft/signalr"
import { BehaviorSubject, Observable } from 'rxjs';
import { environment } from '../../environments/environment.development';

@Injectable({
  providedIn: 'root'
})
export class SignalrService {
  private stationOccupiedSubject: BehaviorSubject<any>;
  private stationClearedSubject: BehaviorSubject<any>;
  private flightRunDoneSubject: BehaviorSubject<any>;
  private hubConnection: signalR.HubConnection | undefined;
  #stationOccupiedData$?: Observable<any>;
  #stationClearedData$?: Observable<any>;
  #flightRunDoneData$: Observable<any>;

  constructor() {
    this.stationOccupiedSubject = new BehaviorSubject<any>(null!);
    this.stationClearedSubject = new BehaviorSubject<any>(null!);
    this.flightRunDoneSubject = new BehaviorSubject<any>(null!);
    this.#stationOccupiedData$ = this.stationOccupiedSubject.asObservable();
    this.#stationClearedData$ = this.stationClearedSubject.asObservable();
    this.#flightRunDoneData$ = this.flightRunDoneSubject.asObservable();
  }

  get stationOccupiedData$(): Observable<any> | undefined {
    return this.#stationOccupiedData$;
  }

  get stationClearedData$(): Observable<any> | undefined {
    return this.#stationClearedData$;
  }

  get flightRunDoneData$(): Observable<any> {
    return this.#flightRunDoneData$;
  }

  startConnection = async () => {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.remoteUrl}${environment.airportHubEP}`)
      .build();
    await this.hubConnection
      .start()
      .then(() => {
        console.log('Connection started');
        this.addStationOccupiedListener(data => this.stationOccupiedSubject.next(JSON.parse(data)))
        this.addStationClearedListener(data => this.stationClearedSubject.next(JSON.parse(data)))
        this.addFlightRunDoneListener(data => this.flightRunDoneSubject.next(JSON.parse(data)))
      })
      .catch(err => console.log('Error while starting connection: ' + err));
  }

  // Adds a listener to station changed event
  addStationOccupiedListener(listener: (...args: any[]) => any) {
    if (!this.hubConnection)
      throw new Error("Connection didn't start yet")
    this.hubConnection?.on(environment.stationOccupiedAsync, listener);
  }

  // Adds a listener to station changed event
  addStationClearedListener(listener: (...args: any[]) => any) {
    if (!this.hubConnection)
      throw new Error("Connection didn't start yet")
    this.hubConnection?.on(environment.stationClearedAsync, listener);
  }

  // Adds a listener to flight run done event
  addFlightRunDoneListener(listener: (...args: any[]) => any) {
    if (!this.hubConnection)
      throw new Error("Connection didn't start yet")
    this.hubConnection?.on(environment.flightRunDone, listener);
  }
}
