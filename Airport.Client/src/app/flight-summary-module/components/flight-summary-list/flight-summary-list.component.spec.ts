import { HttpClientTestingModule } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { FlightSummaryListComponent } from './flight-summary-list.component';

describe('FlightSummaryListComponent', () => {
  let component: FlightSummaryListComponent;
  let fixture: ComponentFixture<FlightSummaryListComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      declarations: [ FlightSummaryListComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(FlightSummaryListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
