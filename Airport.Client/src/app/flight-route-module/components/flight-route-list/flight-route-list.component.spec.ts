import { ComponentFixture, TestBed } from '@angular/core/testing';

import { FlightRouteListComponent } from './flight-route-list.component';

describe('FlightRouteListComponent', () => {
  let component: FlightRouteListComponent;
  let fixture: ComponentFixture<FlightRouteListComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [FlightRouteListComponent]
    });
    fixture = TestBed.createComponent(FlightRouteListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
