import { Direction } from "../flight-route-module/models/direction.model";
import { RouteName } from "../types/route-name.type";

export interface IRoute {
  routeId: string;
  directions: Direction[];
  routeName: RouteName;
}
