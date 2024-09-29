import { HttpParams } from '@angular/common/http';
import { Injectable, OnDestroy } from '@angular/core';
import { Subject, Subscription } from 'rxjs';
import { AirportService } from './airport.service';
import { IAirportSummaryResponse } from '../interfaces/iairport-summary-response.interface';

@Injectable({
  providedIn: 'root'
})
export class FlightSummaryService implements OnDestroy {
  private summarySubject = new Subject<IAirportSummaryResponse>();
  private flightRoutesErrorSubject = new Subject<any>();
  private summarySubscription?: Subscription;

  constructor(private airportSvc: AirportService) {
    if (!this.airportSvc.hasStarted)
      this.airportSvc.start().subscribe({
        next: _ => { },
        error: err => this.flightRoutesErrorSubject.next(err)
      });
  }

  ngOnDestroy(): void {
    this.summarySubscription?.unsubscribe();
  }

  update(params: HttpParams) {
    this.fetch(params);
  }

  get summary$() {
    return this.summarySubject.asObservable();
  }

  get flightRoutesError$() {
    return this.flightRoutesErrorSubject.asObservable();
  }

  // Gets flights summaries
  private fetch(params: HttpParams) {
    this.airportSvc.getSummary(params)
      .subscribe({
        next: (data) => this.summarySubject.next(data),
        error: err => console.log(err)
      });
  }
}
