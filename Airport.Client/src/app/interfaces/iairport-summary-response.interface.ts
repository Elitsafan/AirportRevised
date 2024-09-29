import { FlightSummary } from "../flight-summary-module/models/flight-summary.model";
import { IPagination } from "./ipagination-interface";

export interface IAirportSummaryResponse {
  pagination: IPagination;
  flightsSummary: FlightSummary[];
}
