import { Injectable, OnDestroy } from '@angular/core';
import { Subscription, catchError } from 'rxjs';
import { environment } from '../../environments/environment';
import { FlightType } from '../types/flight.type';
import { SignalrService } from './signalr.service';

@Injectable({
  providedIn: 'root'
})
export class ColorService implements OnDestroy {

  private landingColorIndex: number;
  private departureColorIndex: number;
  private landingColors: string[];
  private departureColors: string[];
  private dictionary: Map<string, string>;
  private flightRunDoneSubscription?: Subscription;

  constructor(private signalrSvc: SignalrService) {
    // Subscribes to flight run done observable
    this.flightRunDoneSubscription = this.signalrSvc.flightRunDoneData$
      .pipe(
        catchError(err => {
          throw err
        }))
      .subscribe({
        next: (flightId: string) => this.dictionary?.delete(flightId),
        error: (error) => console.log(error)
      });
    this.dictionary = new Map();
    // Sets indice
    this.landingColorIndex = 0;
    this.departureColorIndex = 0;
    // Sets colors
    this.landingColors = environment.landingColors;
    this.departureColors = environment.departureColors;
  }

  ngOnDestroy(): void {
    this.flightRunDoneSubscription?.unsubscribe();
  }

  getColor(flightId: string, flightType: FlightType) {
    const value = this.dictionary.get(flightId);
    if (value)
      return value;
    const color = flightType === "Departure"
      ? this.getNextDepartureColor()
      : this.getNextLandingColor();
    this.dictionary.set(flightId, color);
    return color;
  }

  private getNextLandingColor() {
    return this.landingColors[this.landingColorIndex++ % this.landingColors.length];
  }

  private getNextDepartureColor() {
    return this.departureColors[this.departureColorIndex++ % this.departureColors.length]
  }
}
