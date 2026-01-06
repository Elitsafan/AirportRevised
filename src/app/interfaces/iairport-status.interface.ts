import { Station } from "../flight-route-module/models/station.model";
import { IRoute } from "./iroute.interface";

export interface IAirportStatus {
  stations: Station[];
  routes: IRoute[];
}
