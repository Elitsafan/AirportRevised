import { HttpParams } from '@angular/common/http';
import { BehaviorSubject, Observable, ReplaySubject } from 'rxjs';
import { AirportService } from './airport.service';

/**
 * Abstract base class for services that fetch and manage airport data.
 *
 * Uses BehaviorSubject for data (emits last value immediately to new subscribers)
 * and ReplaySubject for errors (buffers last error for late subscribers).
 *
 * Subclasses must implement fetchData() and call initialize() in constructor.
 */
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

  /**
   * Initializes the service by starting the airport if needed, then fetching data.
   * Call this in the constructor of subclasses.
   */
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

  /**
   * Fetches data from the API. Implement this in subclasses.
   * Emit results to dataSubject.next() and errors to errorSubject.next().
   *
   * @param params Optional HTTP parameters for the request
   */
  protected abstract fetchData(params?: HttpParams): void;
}
