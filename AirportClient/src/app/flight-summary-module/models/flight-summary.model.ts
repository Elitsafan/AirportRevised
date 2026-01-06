import { FlightType } from "../../types/flight.type";
import { StationSummary } from "./station-summary.model";

export class FlightSummary {
  constructor(
    public flightId: string,
    public flightType: FlightType,
    public stations: StationSummary[]) { }
}
