import { HttpClientTestingModule } from '@angular/common/http/testing';
import { fakeAsync, flush, flushMicrotasks, TestBed, waitForAsync } from '@angular/core/testing';
import { count, of } from 'rxjs';
import { AirportService } from './airport.service';
import { ColorService } from './color.service';
import { SignalrService } from './signalr.service';
import { IAirport } from '../interfaces/iairport.interface'

import { StationService } from './station.service';
import { IAirportStartResponse } from '../interfaces/iairport-start-response.interface';
import { Station } from '../flight-route-module/models/station.model';

xdescribe('StationService', () => {
  let service: StationService;
  //let signalrSvc: any;
  let airportSvc: any;
  let signalrSvc: any;
  let colorSvc: any;

  let airportSvcSpy: any;
  let signalrSvcSpy: any;
  let colorSvcSpy: any;

  beforeEach(() => {
    airportSvcSpy = jasmine.createSpyObj('AirportService', ['start', 'getStatus']);
    signalrSvcSpy = jasmine.createSpyObj('SignalrService', ['stationChangedData$']);
    colorSvcSpy = jasmine.createSpyObj('ColorService', ['']);

    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        StationService,
        { provide: AirportService, useValue: airportSvcSpy },
        { provide: SignalrService, useValue: signalrSvcSpy },
        { provide: ColorService, useValue: colorSvcSpy },
      ]
    })

    service = TestBed.inject(StationService);
    signalrSvc = TestBed.inject(SignalrService);
    airportSvc = TestBed.inject(AirportService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  xit('should start the station service', fakeAsync(() => {
    let airportStartResponse: IAirportStartResponse = {
      content: "Started",
      statusCode: 200,
      contentType: "application/json"
    }
    let stations: any | undefined = [
      new Station("123"),
      new Station("456"),
      new Station("789"),
    ];
    /////////////////
    // No expectations
    ///////////////
    //signalrSvcSpy.stationChangedData$.subscribe((res: any) => console.log(res))
    signalrSvcSpy.stationChangedData$.and.returnValue(of(stations))
    airportSvcSpy.start.and.returnValue(of(airportStartResponse));
    service.stations$?.subscribe((res) => {
      console.log(res)
      expect(res).toEqual(stations);
    })
    service
      .startService()
      .then(() => {
        console.log('promise')
      });
    flush();
  }));

  xit('should fetch the airport status', fakeAsync(() => {
    //const airportSvc: any = TestBed.inject(AirportService);
    //const airportData: IAirport = {
    //  departures: [],
    //  landings: [],
    //  routes: [],
    //  stations: [
    //    new Station("123"),
    //    new Station("456"),
    //    new Station("789"),
    //  ]
    //};
    let counter = 10
    //service
    //  .fetch()
    //  .then(() => {
    //    console.log(123)
    //    counter = 0
    //    //airportSvc.getStatus.and.returnValue(of(airportData));
    //  })
    //  .catch(error => {
    //    console.log(123)
    //    console.log(error)
    //  });
    flush();
    expect(counter).toBe(0)
    //service.stations$?.subscribe((res) => {
    //  console.log(res)
    //  //expect(res).toEqual(stations);
    //});
    //flush();
  }));
});
