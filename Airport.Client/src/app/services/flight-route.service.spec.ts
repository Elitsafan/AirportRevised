import { TestBed } from '@angular/core/testing';

import { FlightRouteService } from './flight-route.service';

describe('FlightRouteService', () => {
  let service: FlightRouteService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(FlightRouteService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
