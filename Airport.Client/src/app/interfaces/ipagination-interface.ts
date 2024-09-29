export interface IPagination {
  currentPage: number;
  hasNext: boolean;
  hasPrevious: boolean;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  landingsCount: number;
  departuresCount: number;
}
