import { Component, Input } from '@angular/core';
import { IFlight } from '../../../interfaces/iflight.interface';
import { FlightType } from '../../../types/flight.type';
import { FlightService } from '../../../services/flight.service';

@Component({
  selector: 'flight-list',
  templateUrl: './flight-list.component.html',
  styleUrls: ['./flight-list.component.scss']
})
export class FlightListComponent {
  @Input() flights: IFlight[] | null;
  @Input() hideFlightType?: boolean;
  @Input() title?: string;
  @Input() flightType?: FlightType;

  constructor(private flightSvc: FlightService) {
    this.flights = null;
  }

  trackByFlightId(index: number, flight: IFlight) {
    return flight.flightId;
  }

  filter = (flight: IFlight) => flight.flightType === this.flightType
}
