import { fakeAsync, TestBed, tick } from '@angular/core/testing';
import {
  HttpClientTestingModule,
  HttpTestingController,
} from "@angular/common/http/testing";
import { AirportService } from './airport.service';
import { environment } from '../../environments/environment.development';
import { IAirportStatus } from '../interfaces/iairport-status.interface';
import { HttpErrorResponse } from '@angular/common/http';

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

  afterEach(() => httpTestingController.verify());

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('start()', () => {
    it('should start the airport successfully', fakeAsync(() => {
      const expectedResponse = { statusCode: 200, content: "Started", contentType: "application/json" };

      service.start().subscribe(res => {
        // The actual service returns the full HttpResponse, but let's check basic success or body
        // Looking at service implementation, it returns `http.get(... {observe: 'response'})`
        expect(res.body).toEqual(expectedResponse.content);
      });

      const req = httpTestingController.expectOne(environment.remoteUrl + environment.startEP);
      expect(req.request.method).toEqual("GET");
      req.flush(expectedResponse.content, { status: 200, statusText: 'OK' });
    }));

    it('should retry 5 times on failure then fail', fakeAsync(() => {
      service.start().subscribe({
        next: () => fail('Should have failed'),
        error: (error: HttpErrorResponse) => {
          expect(error.status).toBe(500);
        }
      });

      // Initial request + 5 retries = 6 requests
      for (let i = 0; i < 6; i++) {
        const req = httpTestingController.expectOne(environment.remoteUrl + environment.startEP);
        req.flush('Error', { status: 500, statusText: 'Server Error' });
        tick(5000); // Wait for retry delay
      }
    }));
  });

  describe('getStatus()', () => {
    it('should get status successfully', () => {
      const mockStatus: IAirportStatus = { stations: [], routes: [] };

      service.getStatus().subscribe(res => {
        expect(res).toEqual(mockStatus);
      });

      const req = httpTestingController.expectOne(environment.remoteUrl + environment.statusEP);
      req.flush(mockStatus);
    });

    it('should propagate error after retries', fakeAsync(() => {
      const errorSpy = jasmine.createSpy('errorSpy');

      service.getStatus().subscribe({
        next: () => fail('Should have failed'),
        error: errorSpy
      });

      // Initial request + 5 retries = 6 requests
      for (let i = 0; i < 6; i++) {
        const req = httpTestingController.expectOne(environment.remoteUrl + environment.statusEP);
        req.flush('Service Unavailable', { status: 503, statusText: 'Service Unavailable' });
        tick(5000);
      }

      expect(errorSpy).toHaveBeenCalled();
      const errorArgs = errorSpy.calls.first().args[0];
      expect(errorArgs.status).toBe(503);
    }));
  });
});
