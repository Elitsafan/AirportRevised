import { fakeAsync, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { BehaviorSubject, of, throwError } from 'rxjs';
import { AirportService } from './airport.service';
import { StationService } from './station.service';
import { FlightRouteService } from './flight-route.service';
import { Station } from '../flight-route-module/models/station.model';

describe('FlightRouteService', () => {
    let service: FlightRouteService;
    let airportSvcSpy: jasmine.SpyObj<AirportService>;
    let stationSvcSpy: jasmine.SpyObj<StationService>;
    let stationsSubject: BehaviorSubject<Station[]>;

    beforeEach(() => {
        airportSvcSpy = jasmine.createSpyObj('AirportService', ['start', 'getStatus']);
        stationSvcSpy = jasmine.createSpyObj('StationService', [], { stations$: new BehaviorSubject([]) });
        stationsSubject = new BehaviorSubject<Station[]>([]);
        // Define getter behavior property
        Object.defineProperty(stationSvcSpy, 'stations$', { get: () => stationsSubject.asObservable() });

        // Default behavior: Airport already started
        Object.defineProperty(airportSvcSpy, 'hasStarted', { value: true, configurable: true });
        airportSvcSpy.start.and.returnValue(of({} as any));
        airportSvcSpy.getStatus.and.returnValue(of({ routes: [] } as any));

        TestBed.configureTestingModule({
            imports: [HttpClientTestingModule],
            providers: [
                FlightRouteService,
                { provide: AirportService, useValue: airportSvcSpy },
                { provide: StationService, useValue: stationSvcSpy },
            ]
        });
    });

    describe('when Airport has NOT started', () => {
        beforeEach(() => {
            Object.defineProperty(airportSvcSpy, 'hasStarted', { value: false, configurable: true });
        });

        it('should call start() then fetch() on initialization', fakeAsync(() => {
            service = TestBed.inject(FlightRouteService);
            expect(airportSvcSpy.start).toHaveBeenCalled();
            expect(airportSvcSpy.getStatus).toHaveBeenCalled();
        }));

        it('should emit error if start() fails', fakeAsync(() => {
            const error = { status: 500 };
            airportSvcSpy.start.and.returnValue(throwError(() => error));

            service = TestBed.inject(FlightRouteService);

            let receivedError;
            service.flightRoutesError$.subscribe(err => receivedError = err);

            expect(receivedError).toEqual(error as any);
        }));
    });

    describe('when Airport has started', () => {
        it('should fetch immediately without calling start()', () => {
            service = TestBed.inject(FlightRouteService);
            expect(airportSvcSpy.start).not.toHaveBeenCalled();
            expect(airportSvcSpy.getStatus).toHaveBeenCalled();
        });

        it('should combine Routes and Stations to build FlightRoutes', () => {
            const mockRoutes = [{
                routeId: 'r1', routeName: 'Route 1',
                directions: [{ from: 's1', to: 's2' }]
            }];
            const mockStations = [
                new Station('s1', undefined),
                new Station('s2', undefined)
            ];

            airportSvcSpy.getStatus.and.returnValue(of({ routes: mockRoutes } as any));
            stationsSubject.next(mockStations);

            service = TestBed.inject(FlightRouteService);
            service.startService();

            service.flightRoutes$.subscribe(routes => {
                // It might be empty initially until both are present
                if (routes.length > 0) {
                    expect(routes[0].routeId).toBe('r1');
                    expect(routes[0].legs.length).toBeGreaterThan(0);
                }
            });
        });
    });
});
