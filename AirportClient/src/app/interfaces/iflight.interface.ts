import { FlightType } from "../types/flight.type";

export interface IFlight {
  flightId: string;
  routeId: string;
  flightType: FlightType;
  color: string; 
}
