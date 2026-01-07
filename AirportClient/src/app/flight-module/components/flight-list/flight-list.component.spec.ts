import { DebugElement } from '@angular/core';
import { ComponentFixture, fakeAsync, flush, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { FlightListComponent } from './flight-list.component';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { FlightModule } from '../../flight.module';
import { IFlight } from '../../../interfaces/iflight.interface';
import { Flight } from '../../models/flight.model';

describe('FlightListComponent', () => {
  let component: FlightListComponent;
  let fixture: ComponentFixture<FlightListComponent>;
  let el: DebugElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HttpClientTestingModule, FlightModule],
      declarations: [FlightListComponent]
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
    const mockFlights: IFlight[] = [
      new Flight('1', 'Departure', 'To NY', 'Green'),
      new Flight('2', 'Landing', 'From LA', 'Blue')
    ];

    // Test filtering if applicable, or just basic rendering
    component.flights = mockFlights;
    component.flightType = 'Departure';
    fixture.detectChanges();
    flush();

    const flightElements = el.queryAll(By.css('flight'));
    expect(flightElements.length).toBe(1);
  }));
});
