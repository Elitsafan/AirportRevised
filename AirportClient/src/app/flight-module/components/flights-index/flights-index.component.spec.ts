import { HttpClientTestingModule } from '@angular/common/http/testing';
import { DebugElement } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { FlightService } from '../../../services/flight.service';
import { FlightModule } from '../../flight.module';
import { FlightsIndexComponent } from './flights-index.component';

describe('FlightsIndexComponent', () => {
  let component: FlightsIndexComponent;
  let fixture: ComponentFixture<FlightsIndexComponent>;
  let el: DebugElement;
  let flightSvc: jasmine.SpyObj<FlightService>;

  beforeEach(async () => {
    const flightSvcSpy = jasmine.createSpyObj('FlightService', ['updateFlights'], {
      flights$: of([]),
      flightRoutesError$: of(null)
    });

    await TestBed.configureTestingModule({
      imports: [HttpClientTestingModule, FlightModule],
      providers: [
        { provide: FlightService, useValue: flightSvcSpy }
      ]
    })
      .compileComponents();

    fixture = TestBed.createComponent(FlightsIndexComponent);
    flightSvc = TestBed.inject(FlightService) as jasmine.SpyObj<FlightService>;
    el = fixture.debugElement;
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
