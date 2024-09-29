import { Component, Input, OnDestroy, OnInit } from '@angular/core';
import { IPagination } from '../../../interfaces/ipagination-interface';
import { ActivatedRoute, Router } from '@angular/router';
import { Observable, Subscription } from 'rxjs';

@Component({
  selector: 'pagination',
  templateUrl: './pagination.component.html',
  styleUrls: ['./pagination.component.scss']
})
export class PaginationComponent implements OnInit, OnDestroy {
  private paginationSubscription?: Subscription;
  #pageSizes: number[] = [5, 10, 25, 50];
  @Input() pagination$?: Observable<IPagination | null>;
  pageSizes: number[];
  visiblePages: number[];
  visibleRangeLength = 5;
  pagination?: IPagination | null;

  constructor(
    private route: ActivatedRoute,
    private router: Router) {
    this.visiblePages = [];
    this.pageSizes = [];
  }

  ngOnInit(): void {
    this.paginationSubscription = this.pagination$
      ?.subscribe(pagination => {
        this.pagination = pagination;
        this.pageSizes = this.#pageSizes.filter(this.isPageSizeValid);
        this.updateVisiblePages();
      });
  }

  ngOnDestroy(): void {
    this.paginationSubscription?.unsubscribe();
  }

  selectPageSize(pageSize: string): void {
    this.router.navigate(
      ['./'], {
      relativeTo: this.route,
      queryParams: {
        pageSize,
        pageNumber: 1
      },
    });
  }

  private isPageSizeValid = (size: number) => this.pagination?.currentPage! * size <= this.pagination?.totalCount! || (this.pagination?.totalCount! > 0 && this.pagination?.currentPage === this.pagination?.totalPages);
  

  private updateVisiblePages(): void {
    const length = Math.min(this.pagination!.totalPages, this.visibleRangeLength);
    const startIndex = Math.max(
      Math.min(
        this.pagination!.currentPage - Math.ceil(length / 2),
        this.pagination!.totalPages - length
      ),
      0
    );

    this.visiblePages = Array.from(
      new Array(length).keys(),
      (item) => item + startIndex + 1
    );
  }
}
