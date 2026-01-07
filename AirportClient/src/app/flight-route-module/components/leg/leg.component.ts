import { Component, Input } from '@angular/core';
import { Station } from '../../models/station.model';
import { FlightType } from '../../../types/flight.type';

@Component({
  selector: 'leg',
  templateUrl: './leg.component.html',
  styleUrls: ['./leg.component.scss']
})
export class LegComponent {
  @Input() stations?: Station[];
  @Input() flightType?: FlightType;

  trackByStationId(index: number, station: Station): string {
    return station.stationId;
  }
}
