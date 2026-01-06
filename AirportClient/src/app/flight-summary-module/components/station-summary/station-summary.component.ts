import { Component, Input } from '@angular/core';
import { StationSummary } from '../../models/station-summary.model';

@Component({
  selector: 'station-summary',
  templateUrl: './station-summary.component.html',
  styleUrls: ['./station-summary.component.scss']
})
export class StationSummaryComponent {
  @Input() station?: StationSummary;
}
