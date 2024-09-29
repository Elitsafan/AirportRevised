import { Component, Input } from '@angular/core';
import { Station } from '../../models/station.model';
import { FlightType } from '../../../types/flight.type';

@Component({
  selector: 'station',
  templateUrl: './station.component.html',
  styleUrls: ['./station.component.scss']
})
export class StationComponent {
  @Input() station?: Station;
  @Input() flightType?: FlightType;
}
