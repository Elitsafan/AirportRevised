import { ComponentFixture, TestBed } from '@angular/core/testing';

import { StationSummaryListComponent } from './station-summary-list.component';

describe('StationSummaryListComponent', () => {
  let component: StationSummaryListComponent;
  let fixture: ComponentFixture<StationSummaryListComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ StationSummaryListComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(StationSummaryListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
