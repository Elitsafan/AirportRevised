import { HttpParams } from '@angular/common/http';
import { Injectable, OnDestroy } from '@angular/core';
import { BehaviorSubject, Subject, Subscription } from 'rxjs';
import { AirportService } from './airport.service';
import { IAirportSummaryResponse } from '../interfaces/iairport-summary-response.interface';
import { BaseAirportDataService } from './base-airport-data.service';

@Injectable({
  providedIn: 'root'
})
export class FlightSummaryService extends BaseAirportDataService<IAirportSummaryResponse | null> implements OnDestroy {
  private summarySubscription?: Subscription;

  constructor(airportSvc: AirportService) {
    super(airportSvc, null);
  }

  ngOnDestroy(): void {
    this.summarySubscription?.unsubscribe();
  }

  update(params: HttpParams) {
    this.fetchData(params);
  }

  get summary$() {
    return this.data$;
  }

  get flightRoutesError$() {
    return this.error$;
  }

  // Gets flights summaries
  protected fetchData(params?: HttpParams) {
    if(!params) params = new HttpParams();
    this.airportSvc.getSummary(params)
      .subscribe({
        next: (data) => this.dataSubject.next(data),
        error: err => {
          console.log(err);
          this.errorSubject.next(err);
        }
      });
  }
}
