import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';
import { catchError, last, map, retry, tap } from 'rxjs';
import { IAirportSummaryResponse } from '../interfaces/iairport-summary-response.interface';
import { IPagination } from '../interfaces/ipagination-interface';
import { IAirportStatus } from '../interfaces/iairport-status.interface';
import { IFlight } from '../interfaces/iflight.interface';

@Injectable({
  providedIn: 'root'
})
export class AirportService {
  #hasStarted: boolean;

  constructor(private http: HttpClient) {
    this.#hasStarted = false;
  }

  get hasStarted() {
    return this.#hasStarted;
  }

  // Gets the current status of airport
  getStatus() {
    return this.http.get<IAirportStatus>(
      `${environment.remoteUrl}${environment.statusEP}`,
      { observe: 'body' })
      .pipe(
        retry({
          count: 5,
          delay: 5000,
          resetOnSuccess: true
        }),
        catchError(err => {
          throw err
        }));
  }

  // Gets all flights
  getFlights(params?: HttpParams) {
    return this.http.get<IFlight[]>(
      `${environment.remoteUrl}${environment.flightsEP}`,
      { observe: 'body', params })
      .pipe(
        retry({
          count: 5,
          delay: 5000,
          resetOnSuccess: true
        }),
        catchError(err => {
          throw err
        }));
  }

  // Gets information of all the flights
  getSummary(params: HttpParams) {
    return this.http.get(
      `${environment.remoteUrl}${environment.summaryEP}`,
      { observe: 'response', params })
      .pipe(
        map(data => {
          const headers = data.headers;
          const pagination: IPagination = JSON.parse(headers.get('x-pagination')!);
          return {
            flightsSummary: data.body!,
            pagination
          } as IAirportSummaryResponse
        }),
        catchError(err => {
          throw err
        }));
  }

  // Starts the airport
  start() {
    return this.http.get(
      `${environment.remoteUrl}${environment.startEP}`,
      { observe: 'response', responseType: 'text' })
      .pipe(
        retry({
          count: 5,
          delay: 5000,
          resetOnSuccess: true
        }),
        last(),
        tap(res => {
          if (!res.ok)
            return;
          this.#hasStarted = true;
        }),
        catchError(err => {
          throw err
        }));
  }
}
