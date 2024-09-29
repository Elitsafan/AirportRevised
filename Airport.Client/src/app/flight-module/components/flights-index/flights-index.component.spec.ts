import { HttpClientTestingModule } from '@angular/common/http/testing';
import { DebugElement } from '@angular/core';
import { ComponentFixture, fakeAsync, flush, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { of } from 'rxjs';
import { FlightService } from '../../../services/flight.service';
import { FlightModule } from '../../flight.module';
import { Flight } from '../../models/flight.model.ts';

import { FlightsIndexComponent } from './flights-index.component';

describe('FlightsIndexComponent', () => {
  let component: FlightsIndexComponent;
  let fixture: ComponentFixture<FlightsIndexComponent>;
  let el: DebugElement;
  let flightSvc: jasmine.SpyObj<FlightService>;

  beforeEach(async () => {
    const flightSvcSpy = jasmine.createSpyObj('FlightService', {}, ['flights$']);
    await TestBed.configureTestingModule({
      imports: [HttpClientTestingModule, FlightModule],
      providers: [
        { provide: FlightService, useValue: flightSvcSpy }
      ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(FlightsIndexComponent);
    flightSvc = TestBed.inject(FlightService) as jasmine.SpyObj<FlightService>;
    //flightSvc = TestBed.inject(FlightService);
    el = fixture.debugElement;
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should create', fakeAsync(() => {
    let flights: Flight[] = [
      new Flight('123', '1', 'Departure', 'red')
    ]
    //spyOnProperty(flightSvc, 'flights$').and.returnValue(of(flights));
    //flightSvc.flights$?.subscribe((res: Flight[]) => console.log(res));
    //fixture.detectChanges();
    //flush();
    //const flightElements = el.queryAll(By.css('flight'));
    //console.log(flightElements)
    //expect(flightElements.length).toBe(1);
  }));

  afterEach(() => {
    //flightSvc.flights$.calls.reset();
  })
});
