import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { fakeAsync, flush, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { IAirportStartResponse } from '../interfaces/iairport-start-response.interface';
import { AirportService } from './airport.service';
import { ColorService } from './color.service';

import { FlightService } from './flight.service';
import { SignalrService } from './signalr.service';

describe('FlightService', () => {
  let service: FlightService;
  let airportSvc: jasmine.SpyObj<AirportService>;
  let signalrSvc: any;
  let colorSvc: any;
  //let httpTestingController: HttpTestingController;

  beforeEach(() => {
    const airportSvcSpy = jasmine.createSpyObj('AirportService', ['start']);
    const signalrSvcSpy = jasmine.createSpyObj('SignalrService', ['']);
    const colorSvcSpy = jasmine.createSpyObj('ColorService', ['']);
    console.log(airportSvcSpy)

    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        FlightService,
        { provide: AirportService, useValue: airportSvcSpy },
        { provide: SignalrService, useValue: signalrSvcSpy },
        { provide: ColorService, useValue: colorSvcSpy },
      ]
    });
    service = TestBed.inject(FlightService);
    airportSvc = TestBed.inject(AirportService) as jasmine.SpyObj<AirportService>;

    //httpTestingController = TestBed.inject(HttpTestingController);
  });

  fit('should be created', fakeAsync(() => {
    const response: IAirportStartResponse = {
      content: "Started",
      contentType: "application/json",
      statusCode: 200
    };
    spyOn(airportSvc, 'start').and.returnValue(of(response));
    flush();
    expect(service).toBeTruthy();
  }));
});
