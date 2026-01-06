import { HttpParams } from '@angular/common/http';
import { BehaviorSubject, Observable, ReplaySubject } from 'rxjs';
import { AirportService } from './airport.service';

export abstract class BaseAirportDataService<T> {
  protected dataSubject: BehaviorSubject<T>;
  protected errorSubject: ReplaySubject<any>;

  protected constructor(
    protected airportSvc: AirportService,
    initialValue: T
  ) {
    this.dataSubject = new BehaviorSubject<T>(initialValue);
    this.errorSubject = new ReplaySubject<any>(1);
  }

  get data$(): Observable<T> {
    return this.dataSubject.asObservable();
  }

  get error$(): Observable<any> {
    return this.errorSubject.asObservable();
  }

  protected initialize(): void {
    if (!this.airportSvc.hasStarted) {
      this.airportSvc.start().subscribe({
        next: () => this.fetchData(),
        error: err => this.errorSubject.next(err)
      });
    } else {
      this.fetchData();
    }
  }

  // Abstract method that concrete services must implement
  // Optional params arg can be ignored by services that don't need it for initial fetch
  protected abstract fetchData(params?: HttpParams): void;
}
