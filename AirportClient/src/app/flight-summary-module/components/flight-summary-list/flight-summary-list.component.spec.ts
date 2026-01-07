import { HttpClientTestingModule } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { FlightSummaryListComponent } from './flight-summary-list.component';
import { LoadingModule } from 'src/app/loading-module/loading.module';
import { SharedModule } from 'src/app/shared-module/shared.module';

describe('FlightSummaryListComponent', () => {
  let component: FlightSummaryListComponent;
  let fixture: ComponentFixture<FlightSummaryListComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HttpClientTestingModule, RouterTestingModule, LoadingModule, SharedModule],
      declarations: [FlightSummaryListComponent]
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
