import { RouteName } from "../../types/route-name.type";
import { Leg } from "./leg.model";

export class FlightRoute {
  constructor(
    public routeId: string,
    public name: RouteName,
    public legs: Leg[]
  ) { }
}
