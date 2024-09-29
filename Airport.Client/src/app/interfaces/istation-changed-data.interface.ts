import { IFlight } from "./iflight.interface";

export interface IStationChanged {
  stationId: string;
  flight?: IFlight;
}

export interface IStationChangedData extends Array<IStationChanged> {}
