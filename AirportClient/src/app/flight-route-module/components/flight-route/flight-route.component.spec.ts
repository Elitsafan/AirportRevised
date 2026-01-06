import { ComponentFixture, TestBed } from '@angular/core/testing';

import { FlightRouteComponent } from './flight-route.component';

describe('FlightRouteComponent', () => {
  let component: FlightRouteComponent;
  let fixture: ComponentFixture<FlightRouteComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [FlightRouteComponent]
    });
    fixture = TestBed.createComponent(FlightRouteComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
