import { Component, Input } from '@angular/core';
import { FlightSummary } from '../../models/flight-summary.model';

@Component({
  selector: 'tr[flight-summary]',
  template: `
    <th>{{flight?.flightId}}</th>
    <td>{{flight?.flightType}}</td>
    <td><station-summary-list [stations]="flight?.stations"></station-summary-list></td>
  `,
  styleUrls: ['./flight-summary.component.scss']
})
export class FlightSummaryComponent {
  @Input() flight?: FlightSummary;
}
