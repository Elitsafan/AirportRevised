import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { FlightComponent } from './flight.component';

describe('FlightComponent', () => {
  let component: FlightComponent;
  let fixture: ComponentFixture<FlightComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [FlightComponent]
    })
      .compileComponents()
      .then(() => {
        fixture = TestBed.createComponent(FlightComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
      });

  }));

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
