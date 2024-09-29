import { HttpClientTestingModule } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AirportService } from './airport.service';

import { FlightSummaryService } from './flight-summary.service';

describe('FlightSummaryService', () => {
  let service: FlightSummaryService;
  let airportSvcSpy: any;

  beforeEach(() => {
    //airportSvcSpy = jasmine.createSpyObj('AirportService', ['']);
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        FlightSummaryService,
        //{ provider: AirportService, useValue: airportSvcSpy }
      ]
    });
    service = TestBed.inject(FlightSummaryService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
