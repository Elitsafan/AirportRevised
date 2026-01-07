import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { FlightRouteListComponent } from './flight-route-list.component';
import { LoadingModule } from 'src/app/loading-module/loading.module';

describe('FlightRouteListComponent', () => {
  let component: FlightRouteListComponent;
  let fixture: ComponentFixture<FlightRouteListComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HttpClientTestingModule, LoadingModule],
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
