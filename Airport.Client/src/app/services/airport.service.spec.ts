import { fakeAsync, TestBed } from '@angular/core/testing';
import {
  HttpClientTestingModule,
  HttpTestingController,
} from "@angular/common/http/testing";
import { AirportService } from './airport.service';
import { environment } from '../../environments/environment.development';
import { IAirport } from '../interfaces/iairport.interface';
import { FlightSummary } from '../flight-summary-module/models/flight-summary.model';

describe('AirportService', () => {
  let service: AirportService;
  let httpTestingController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [AirportService]
    });
    service = TestBed.inject(AirportService);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  // Should be created
  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  // Should start the airport
  it('should start the airport', fakeAsync(() => {
    const expectedResponse = {
      content: "Started",
      contentType: "application/json",
      statusCode: 200
    };
    service.start()
      .subscribe(res => {
        expect(res).toEqual(expectedResponse)
      })
    const req = httpTestingController.expectOne(environment.remoteUrl + environment.startEP);
    expect(req.request.method).toEqual("GET");
    req.flush(expectedResponse);
  }));

  // Should get response the airport already started
  it('should get response the airport already started', fakeAsync(() => {
    const expectedResponse = {
      content: "Already started",
      contentType: "application/json",
      statusCode: 200
    };
    service.start().subscribe();
    httpTestingController
      .expectOne(environment.remoteUrl + environment.startEP)
      .flush({});

    service.start()
      .subscribe(res => {
        expect(res).toEqual(expectedResponse)
      });
    let reqAgain = httpTestingController.expectOne(environment.remoteUrl + environment.startEP);
    reqAgain.flush(expectedResponse);

    service.start()
      .subscribe(res => {
        expect(res).toEqual(expectedResponse)
      });
    reqAgain = httpTestingController.expectOne(environment.remoteUrl + environment.startEP);
    reqAgain.flush(expectedResponse);
  }));

  // Should get the status of the airport
  it('should get the status of the airport', fakeAsync(() => {
    const expectedResponse: IAirport = {
      stations: [],
      landings: [],
      departures: [],
      routes: []
    };
    service.getStatus()
      .subscribe({
        next: res => expect(res).toEqual(expectedResponse),
        error: error => console.log(error)
      })
    const req = httpTestingController.expectOne(environment.remoteUrl + environment.statusEP);
    expect(req.request.method).toEqual("GET");
    req.flush({ statusCode: 200, value: expectedResponse });
  }));

  // Should get 503 status code for getStatus request when the airport did not get started
  it('should get 503 status code for getStatus request when the airport did not get started', fakeAsync(() => {
    const expectedResponse = {
      statusCode: 503
    };
    service.getStatus()
      .subscribe({
        next: () => { },
        error: error => expect(error).toEqual(expectedResponse)
      })
    const req = httpTestingController.expectOne(environment.remoteUrl + environment.statusEP);
    expect(req.request.method).toEqual("GET");
    req.flush(expectedResponse);
  }));

  // Should get the summary of the airport
  it('should get the summary of the airport', fakeAsync(() => {
    const expectedResponse: FlightSummary[] = [];
    service.getSummary()
      .subscribe({
        next: (res) => expect(res).toEqual(expectedResponse),
        error: error => expect(error).toEqual(expectedResponse)
      })
    const req = httpTestingController.expectOne(environment.remoteUrl + environment.summaryEP);
    expect(req.request.method).toEqual("GET");
    req.flush({ statusCode: 200, value: expectedResponse });
  }));

  // Should get 503 status code for getSummary request when the airport did not get started
  it('should get 503 status code for getSummary request when the airport did not get started', fakeAsync(() => {
    const expectedResponse = {
      statusCode: 503
    };
    service.getSummary()
      .subscribe({
        next: () => { },
        error: error => {
          expect(error).toEqual(expectedResponse)
        }
      })
    const req = httpTestingController.expectOne(environment.remoteUrl + environment.summaryEP);
    expect(req.request.method).toEqual("GET");
    req.flush(expectedResponse);
  }));

  afterEach(() => httpTestingController.verify());
});
