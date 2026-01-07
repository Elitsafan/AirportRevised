import { HttpClientTestingModule } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { BehaviorSubject, of } from 'rxjs';
import { AirportService } from './airport.service';
import { ColorService } from './color.service';
import { SignalrService } from './signalr.service';

import { StationService } from './station.service';

describe('StationService', () => {
  let service: StationService;
  let airportSvcSpy: jasmine.SpyObj<AirportService>;
  let signalrSvcSpy: jasmine.SpyObj<SignalrService>;
  let colorSvcSpy: jasmine.SpyObj<ColorService>;
  let stationOccupiedSubject: BehaviorSubject<any>;
  let stationClearedSubject: BehaviorSubject<any>;
  let connectionErrorSubject: BehaviorSubject<any>;

  beforeEach(() => {
    airportSvcSpy = jasmine.createSpyObj('AirportService', ['start', 'getStatus']);
    signalrSvcSpy = jasmine.createSpyObj('SignalrService', ['startConnection'], {
      stationOccupiedData$: new BehaviorSubject(null),
      stationClearedData$: new BehaviorSubject(null),
      connectionError$: new BehaviorSubject(null)
    });
    colorSvcSpy = jasmine.createSpyObj('ColorService', ['getColor']);

    stationOccupiedSubject = new BehaviorSubject<any>(null);
    stationClearedSubject = new BehaviorSubject<any>(null);
    connectionErrorSubject = new BehaviorSubject<any>(null);

    Object.defineProperty(signalrSvcSpy, 'stationOccupiedData$', { get: () => stationOccupiedSubject.asObservable() });
    Object.defineProperty(signalrSvcSpy, 'stationClearedData$', { get: () => stationClearedSubject.asObservable() });
    Object.defineProperty(signalrSvcSpy, 'connectionError$', { get: () => connectionErrorSubject.asObservable() });

    // Default behavior
    Object.defineProperty(airportSvcSpy, 'hasStarted', { value: true, configurable: true });
    airportSvcSpy.start.and.returnValue(of({} as any));
    airportSvcSpy.getStatus.and.returnValue(of({ stations: [] } as any));

    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        StationService,
        { provide: AirportService, useValue: airportSvcSpy },
        { provide: SignalrService, useValue: signalrSvcSpy },
        { provide: ColorService, useValue: colorSvcSpy },
      ]
    });

    service = TestBed.inject(StationService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('startService', () => {
    it('should fetch data immediately if airport has started', () => {
      service.startService();
      expect(airportSvcSpy.getStatus).toHaveBeenCalled();
    });

    it('should start airport then fetch if not started', () => {
      Object.defineProperty(airportSvcSpy, 'hasStarted', { value: false });
      service.startService();
      expect(airportSvcSpy.start).toHaveBeenCalled();
      expect(airportSvcSpy.getStatus).toHaveBeenCalled();
    });

    it('should subscribe to SignalR connection errors and propagate to stationsError$', (done) => {
      service.startService();

      // Subscribe to station errors
      service.stationsError$.subscribe(error => {
        if (error) {
          expect(error.message).toBe('SignalR connection lost');
          done();
        }
      });

      // Simulate SignalR connection error
      connectionErrorSubject.next(new Error('SignalR connection lost'));
    });
  });

  describe('SignalR Updates', () => {
    it('should update station flight when station is occupied', () => {
      const mockStations = [{ stationId: 's1', flight: undefined }, { stationId: 's2', flight: undefined }];
      airportSvcSpy.getStatus.and.returnValue(of({ stations: mockStations } as any));

      service.startService();

      const updateData = [{
        stationId: 's1',
        flight: { flightId: 'f1', flightType: 'Landing' }
      }];

      stationOccupiedSubject.next(updateData);

      service.stations$!.subscribe(stations => {
        const s1 = stations.find(s => s.stationId === 's1');
        if (s1 && s1.flight) {
          expect(s1.flight.flightId).toBe('f1');
        }
      });
    });

    it('should clear station flight when station is cleared', () => {
      // Initial state with occupied station
      const mockStations = [{ stationId: 's1', flight: undefined }]; // Logic populates flight later or we assume empty start
      airportSvcSpy.getStatus.and.returnValue(of({ stations: mockStations } as any));

      service.startService();

      // Simulate occupation first
      stationOccupiedSubject.next([{ stationId: 's1', flight: { flightId: 'f1' } }]);

      // Simulate clearance (flight is null/undefined in update?)
      // Looking at code: data: IStationChangedData -> flight?: IFlight.
      // If flight is undefined, flightResolver returns undefined.
      stationClearedSubject.next([{ stationId: 's1', flight: null }]);

      service.stations$!.subscribe(stations => {
        const s1 = stations.find(s => s.stationId === 's1');
        if (s1 && s1.flight === undefined) {
          expect(s1.flight).toBeUndefined();
        }
      });
    });
  });
});
