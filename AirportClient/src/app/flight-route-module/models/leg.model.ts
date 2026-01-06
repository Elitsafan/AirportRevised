import { Station } from "./station.model";

export class Leg {
  constructor(
    public stations: Station[]
  ) { }
}
