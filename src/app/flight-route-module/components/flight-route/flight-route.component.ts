import { Component, Input } from '@angular/core';
import { Leg } from '../../models/leg.model';
import { RouteName } from '../../../types/route-name.type';

@Component({
  selector: 'flight-route',
  templateUrl: './flight-route.component.html',
  styleUrls: ['./flight-route.component.scss']
})
export class FlightRouteComponent {
  @Input() legs?: Leg[] | null;
  @Input() routeName?: RouteName;
}
