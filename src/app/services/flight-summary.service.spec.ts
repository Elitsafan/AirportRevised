import { fakeAsync, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { of, throwError } from 'rxjs';
import { AirportService } from './airport.service';
import { FlightSummaryService } from './flight-summary.service';
import { IAirportSummaryResponse } from '../interfaces/iairport-summary-response.interface';

describe('FlightSummaryService', () => {
    let service: FlightSummaryService;
    let airportSvcSpy: jasmine.SpyObj<AirportService>;

    beforeEach(() => {
        airportSvcSpy = jasmine.createSpyObj('AirportService', ['start', 'getSummary']);

        // Default behavior: Airport already started
        Object.defineProperty(airportSvcSpy, 'hasStarted', { value: true, configurable: true });
        airportSvcSpy.start.and.returnValue(of({} as any));
        airportSvcSpy.getSummary.and.returnValue(of({} as any));

        TestBed.configureTestingModule({
            imports: [HttpClientTestingModule],
            providers: [
                FlightSummaryService,
                { provide: AirportService, useValue: airportSvcSpy },
            ]
        });
    });

    describe('when Airport has NOT started', () => {
        beforeEach(() => {
            // Re-configure property for this suite
            // We can use Object.getOwnPropertyDescriptor check or just overwrite if configurable
            Object.defineProperty(airportSvcSpy, 'hasStarted', { value: false, configurable: true });
        });

        it('should call start() then fetch() on initialization', fakeAsync(() => {
            airportSvcSpy.getSummary.and.returnValue(of({} as any));

            service = TestBed.inject(FlightSummaryService);

            expect(airportSvcSpy.start).toHaveBeenCalled();
            expect(airportSvcSpy.getSummary).toHaveBeenCalled();
        }));

        it('should emit error if start() fails', fakeAsync(() => {
            const error = { status: 500 };
            airportSvcSpy.start.and.returnValue(throwError(() => error));

            service = TestBed.inject(FlightSummaryService);

            let receivedError;
            service.flightRoutesError$.subscribe(err => receivedError = err);

            expect(receivedError).toEqual(error as any);
        }));
    });

    describe('when Airport has started', () => {
        beforeEach(() => {
            Object.defineProperty(airportSvcSpy, 'hasStarted', { value: true, configurable: true });
        });

        it('should fetch immediately without calling start()', () => {
            service = TestBed.inject(FlightSummaryService);
            expect(airportSvcSpy.start).not.toHaveBeenCalled();
            expect(airportSvcSpy.getSummary).toHaveBeenCalled();
        });

        it('should emit data via summary$', () => {
            const mockSummary: IAirportSummaryResponse = {
                flightsSummary: [],
                pagination: { currentPage: 1, totalPages: 1, totalCount: 0, pageSize: 10, hasNext: false, hasPrevious: false, landingsCount: 0, departuresCount: 0 }
            };
            airportSvcSpy.getSummary.and.returnValue(of(mockSummary));

            service = TestBed.inject(FlightSummaryService);

            service.summary$.subscribe(summary => {
                expect(summary).toEqual(mockSummary);
            });
        });

        it('should emit error via flightRoutesError$ if fetch fails', () => {
            const error = { status: 503 };
            airportSvcSpy.getSummary.and.returnValue(throwError(() => error));

            service = TestBed.inject(FlightSummaryService);

            let receivedError;
            service.flightRoutesError$.subscribe(err => receivedError = err);

            expect(receivedError).toEqual(error as any);
        });
    });
});
