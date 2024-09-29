import { Component, Input } from '@angular/core';
import { IFlight } from '../../../interfaces/iflight.interface';

@Component({
  selector: 'flight',
  templateUrl: './flight.component.html',
  styleUrls: ['./flight.component.scss']
})
export class FlightComponent {
  @Input() flight?: IFlight;
  @Input() hideFlightType?: boolean;
  @Input() index?: number;
}
