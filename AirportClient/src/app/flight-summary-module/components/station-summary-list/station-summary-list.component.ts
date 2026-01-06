import { Component, Input } from '@angular/core';
import { StationSummary } from '../../models/station-summary.model';

@Component({
  selector: 'station-summary-list',
  templateUrl: './station-summary-list.component.html',
  styleUrls: ['./station-summary-list.component.scss']
})
export class StationSummaryListComponent {
  @Input() stations?: StationSummary[];

  trackByStationId(index: number, station: StationSummary) {
    return station.stationId;
  }
}
