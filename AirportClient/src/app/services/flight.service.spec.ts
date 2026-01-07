import { fakeAsync, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { of, throwError } from 'rxjs';
import { AirportService } from './airport.service';
import { ColorService } from './color.service';
import { FlightService } from './flight.service';

describe('FlightService', () => {
    let service: FlightService;
    let airportSvcSpy: jasmine.SpyObj<AirportService>;
    let colorSvcSpy: jasmine.SpyObj<ColorService>;

    beforeEach(() => {
        airportSvcSpy = jasmine.createSpyObj('AirportService', ['start', 'getFlights']);
        colorSvcSpy = jasmine.createSpyObj('ColorService', ['getColor']);

        // Default behavior: Airport already started
        Object.defineProperty(airportSvcSpy, 'hasStarted', { value: true, configurable: true });
        airportSvcSpy.start.and.returnValue(of({} as any));
        airportSvcSpy.getFlights.and.returnValue(of([]));

        TestBed.configureTestingModule({
            imports: [HttpClientTestingModule],
            providers: [
                FlightService,
                { provide: AirportService, useValue: airportSvcSpy },
                { provide: ColorService, useValue: colorSvcSpy },
            ]
        });
    });

    describe('when Airport has NOT started', () => {
        beforeEach(() => {
            // Redefine property specifically for this suite
            Object.defineProperty(airportSvcSpy, 'hasStarted', { value: false, configurable: true });
        });

        it('should call start() then fetch() on initialization', fakeAsync(() => {
            airportSvcSpy.start.and.returnValue(of({} as any));
            airportSvcSpy.getFlights.and.returnValue(of([]));

            service = TestBed.inject(FlightService); // Trigger constructor

            expect(airportSvcSpy.start).toHaveBeenCalled();
            expect(airportSvcSpy.getFlights).toHaveBeenCalled();
        }));

        it('should emit error if start() fails', () => {
            const error = { status: 500 };
            airportSvcSpy.start.and.returnValue(throwError(() => error));

            service = TestBed.inject(FlightService);

            let receivedError;
            service.flightRoutesError$.subscribe(err => receivedError = err);

            expect(receivedError).toEqual(error as any);
        });
    });

    describe('when Airport has started', () => {
        beforeEach(() => {
            Object.defineProperty(airportSvcSpy, 'hasStarted', { value: true, configurable: true });
        });

        it('should fetch immediately without calling start()', () => {
            service = TestBed.inject(FlightService);
            expect(airportSvcSpy.start).not.toHaveBeenCalled();
            expect(airportSvcSpy.getFlights).toHaveBeenCalled();
        });

        it('should emit data via flights$', () => {
            const mockFlights = [{ flightId: 'f1', flightType: 'Landing' }];
            airportSvcSpy.getFlights.and.returnValue(of(mockFlights as any));

            service = TestBed.inject(FlightService);

            service.flights$.subscribe(flights => {
                expect(flights.length).toBe(1);
                expect(flights[0].flightId).toBe('f1');
            });
        });

        it('should emit error via flightRoutesError$ if fetch fails', () => {
            const error = { status: 503 };
            airportSvcSpy.getFlights.and.returnValue(throwError(() => error));

            service = TestBed.inject(FlightService);

            let receivedError;
            service.flightRoutesError$.subscribe(err => receivedError = err);

            expect(receivedError).toEqual(error as any);
        });
    });
});
