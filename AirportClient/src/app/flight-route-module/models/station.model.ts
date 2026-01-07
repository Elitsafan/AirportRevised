import { IFlight } from "../../interfaces/iflight.interface";

export class Station {
  constructor(
    public stationId: string,
    public flight?: IFlight) { }
}
