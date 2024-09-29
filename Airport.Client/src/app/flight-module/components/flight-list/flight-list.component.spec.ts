import { DebugElement } from '@angular/core';
import { ComponentFixture, fakeAsync, flush, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { of } from 'rxjs';
import { IFlight } from '../../../interfaces/iflight.interface';
import { Flight } from '../../models/flight.model.ts';

import { FlightListComponent } from './flight-list.component';

describe('FlightListComponent', () => {
  let component: FlightListComponent;
  let fixture: ComponentFixture<FlightListComponent>;
  let el: DebugElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ FlightListComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(FlightListComponent);
    component = fixture.componentInstance;
    el = fixture.debugElement;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should resolve the flights list', fakeAsync(() => {
    let flights: IFlight[] = [
      new Flight('123', '1', 'Departure', 'red')
    ]
    component.flights$ = of(flights);
    fixture.detectChanges();
    flush();
    const flightElements = el.queryAll(By.css('flight'));
    console.log(flightElements)
    //expect(flightElements.length).toBe(1);
    //expect(flightElements[0].nativeElement)
  }))
});
