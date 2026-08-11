import { Injectable } from '@angular/core';
import * as signalR from "@microsoft/signalr"
import { BehaviorSubject, Observable, ReplaySubject } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})

export class SignalrService {
  private flightRunStartedSubject: BehaviorSubject<any>;
  private stationClearedSubject: BehaviorSubject<any>;
  private flightRunDoneSubject: BehaviorSubject<any>;
  private connectionErrorSubject = new ReplaySubject<any>(1);
  private hubConnection: signalR.HubConnection | undefined;
  #flightRunStartedData$?: Observable<any>;
  #stationClearedData$?: Observable<any>;
  #flightRunDoneData$: Observable<any>;
  #connectionError$: Observable<any>;

  constructor() {
    this.flightRunStartedSubject = new BehaviorSubject<any>(null!);
    this.stationClearedSubject = new BehaviorSubject<any>(null!);
    this.flightRunDoneSubject = new BehaviorSubject<any>(null!);
    this.#flightRunStartedData$ = this.flightRunStartedSubject.asObservable();
    this.#stationClearedData$ = this.stationClearedSubject.asObservable();
    this.#flightRunDoneData$ = this.flightRunDoneSubject.asObservable();
    this.#connectionError$ = this.connectionErrorSubject.asObservable();
  }

  get flightRunStartedData$(): Observable<any> | undefined {
    return this.#flightRunStartedData$;
  }

  get stationClearedData$(): Observable<any> | undefined {
    return this.#stationClearedData$;
  }

  get flightRunDoneData$(): Observable<any> {
    return this.#flightRunDoneData$;
  }

  get connectionError$(): Observable<any> {
    return this.#connectionError$;
  }

  startConnection = async () => {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.remoteUrl}${environment.airportHubEP}`)
      .build();

    // Handle connection close/disconnect
    this.hubConnection.onclose((error) => {
      console.error('SignalR connection closed', error);
      this.connectionErrorSubject.next(
        error || new Error('Connection to server lost')
      );
    });

    await this.hubConnection
      .start()
      .then(() => {
        console.log('Connection started');
        this.addFlightRunStartedListener(data => this.flightRunStartedSubject.next(JSON.parse(data)));
        this.addStationClearedListener(data => this.stationClearedSubject.next(JSON.parse(data)));
        this.addFlightRunDoneListener(data => this.flightRunDoneSubject.next(JSON.parse(data)));
      })
      .catch(err => console.log('Error while starting connection: ' + err));
  }

  // Adds a listener to station occupied event
  addFlightRunStartedListener(listener: (...args: any[]) => any) {
    if (!this.hubConnection)
      throw new Error("Connection didn't start yet")
    this.hubConnection?.on(environment.flightRunStarted, listener);
  }

  // Adds a listener to station cleared event
  addStationClearedListener(listener: (...args: any[]) => any) {
    if (!this.hubConnection)
      throw new Error("Connection didn't start yet")
    this.hubConnection?.on(environment.stationCleared, listener);
  }

  // Adds a listener to flight run done event
  addFlightRunDoneListener(listener: (...args: any[]) => any) {
    if (!this.hubConnection)
      throw new Error("Connection didn't start yet")
    this.hubConnection?.on(environment.flightRunDone, listener);
  }
}
