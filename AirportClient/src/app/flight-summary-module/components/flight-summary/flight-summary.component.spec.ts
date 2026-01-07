import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FlightSummaryModule } from '../../flight-summary.module';

import { FlightSummaryComponent } from './flight-summary.component';

describe('FlightSummaryComponent', () => {
  let component: FlightSummaryComponent;
  let fixture: ComponentFixture<FlightSummaryComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FlightSummaryModule]
    })
      .compileComponents();

    fixture = TestBed.createComponent(FlightSummaryComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
