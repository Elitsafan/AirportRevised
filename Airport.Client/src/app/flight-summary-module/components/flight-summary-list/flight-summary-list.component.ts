import { Component, OnDestroy, OnInit } from '@angular/core';
import { BehaviorSubject, Observable, Subscription, tap } from 'rxjs';
import { FlightSummary } from '../../models/flight-summary.model';
import { FlightSummaryService } from '../../../services/flight-summary.service';
import { IPagination } from '../../../interfaces/ipagination-interface';
import { HttpParams } from '@angular/common/http';
import { ActivatedRoute } from '@angular/router';
import { IAirportSummaryResponse } from '../../../interfaces/iairport-summary-response.interface';

@Component({
  selector: 'flight-summary-list',
  templateUrl: './flight-summary-list.component.html',
  styleUrls: ['./flight-summary-list.component.scss']
})

export class FlightSummaryListComponent implements OnInit, OnDestroy {
  private summarySubscription?: Subscription;
  private queryParamsSubscription?: Subscription;
  private flightRoutesErrorSubscription?: Subscription;
  private paginationSubject: BehaviorSubject<IPagination | null>;
  loading: boolean;
  pagination$: Observable<IPagination | null>;
  flightsSummary?: FlightSummary[];
  pagination?: IPagination;
  currentFlights?: string;
  displayingLandings?: string;
  displayingDepartures?: string;
  landingsCount?: number;
  departuresCount?: number;

  constructor(
    private flightSummarySvc: FlightSummaryService,
    private route: ActivatedRoute) {
    this.paginationSubject = new BehaviorSubject<IPagination | null>(null);
    this.loading = false;
    this.pagination$ = this.paginationSubject.asObservable();
  }

  ngOnDestroy(): void {
    this.queryParamsSubscription?.unsubscribe();
    this.summarySubscription?.unsubscribe();
    this.flightRoutesErrorSubscription?.unsubscribe();
  }

  ngOnInit(): void {
    this.flightRoutesErrorSubscription = this.flightSummarySvc.flightRoutesError$
      .subscribe(_ => this.loading = false);
    this.queryParamsSubscription = this.route.queryParams
      .pipe(tap(_ => this.loading = true))
      .subscribe(params => {
        const newParams = new HttpParams()
          .set("pageNumber", params['pageNumber'] ?? 1)
          .set("pageSize", params['pageSize'] ?? 10)
        this.flightSummarySvc.update(newParams);
      });

    this.summarySubscription = this.flightSummarySvc.summary$
      .subscribe({
        next: (summary: IAirportSummaryResponse) => {
          this.flightsSummary = summary.flightsSummary;
          this.pagination = summary.pagination;
          this.paginationSubject.next(this.pagination);
          this.updateCountFlightType();
          this.updateDisplayCurrentFlights();
          this.updateDisplayFlightByType()
          this.loading = false;
        },
        error: err => console.log(err)
      });
  }

  trackByFlightId(index: number, flight: FlightSummary) {
    return flight.flightId;
  }

  private updateDisplayCurrentFlights() {
    let lowerBound = 1;
    let upperBound = 1;
    if (this.pagination!.currentPage === 0)
      lowerBound = upperBound = 0;
    else if (this.pagination!.currentPage === 1)
      upperBound = Math.min(this.pagination!.totalCount, this.pagination!.pageSize);
    else {
      lowerBound += (this.pagination!.currentPage - 1) * this.pagination!.pageSize;
      upperBound = Math.min(this.pagination!.currentPage * this.pagination!.pageSize, this.pagination!.totalCount)
    }
    this.currentFlights = `${lowerBound} - ${upperBound}`;
  }

  private updateCountFlightType() {
    this.departuresCount = this.flightsSummary?.filter(f => f.flightType === 'Departure').length;
    this.landingsCount = this.flightsSummary?.filter(f => f.flightType === 'Landing').length;
  }

  private updateDisplayFlightByType() {
    const isFirstPage = this.pagination?.currentPage === 1;
    const deltaDepartures = isFirstPage ? 1 : this.pagination?.departuresCount! - this.departuresCount!;
    const deltaLandings = isFirstPage ? 1 : this.pagination?.landingsCount! - this.landingsCount!;
    this.displayingDepartures = `${deltaDepartures} - ${this.pagination?.departuresCount}`;
    this.displayingLandings = `${deltaLandings} - ${this.pagination?.landingsCount}`;
  }
}
