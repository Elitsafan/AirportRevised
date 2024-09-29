import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LegComponent } from './leg.component';

describe('LegComponent', () => {
  let component: LegComponent;
  let fixture: ComponentFixture<LegComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [LegComponent]
    });
    fixture = TestBed.createComponent(LegComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
