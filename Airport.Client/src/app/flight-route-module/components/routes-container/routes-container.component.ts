import { Component, Input } from '@angular/core';
import { FlightRoute } from '../../models/flight-route.model';
import { RouteName } from '../../../types/route-name.type';

@Component({
  selector: 'routes-container',
  templateUrl: './routes-container.component.html',
  styleUrls: ['./routes-container.component.scss']
})
export class RoutesContainerComponent {
  @Input() routes?: FlightRoute[] | null;

  private filterRoutes(name: RouteName): FlightRoute[] {
    return this.routes?.filter(r => r.name === name) || [];
  }

  get landingRoutes(): FlightRoute[] {
    return this.filterRoutes('Landing');
  }

  get departureRoutes(): FlightRoute[] {
    return this.filterRoutes('Departure');
  }
}
