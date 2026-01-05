import { Component, Input } from '@angular/core';
import { Station } from '../../models/station.model';
import { FlightType } from '../../../types/flight.type';
import { StationIdMapperService } from '../../../services/station-id-mapper.service';

@Component({
  selector: 'station',
  templateUrl: './station.component.html',
  styleUrls: ['./station.component.scss']
})
export class StationComponent {
  @Input() station?: Station;
  @Input() flightType?: FlightType;

  constructor(private stationIdMapper: StationIdMapperService) {}

  getDisplayId(): number {
    return this.station ? this.stationIdMapper.getSequentialId(this.station.stationId) : 0;
  }
}
