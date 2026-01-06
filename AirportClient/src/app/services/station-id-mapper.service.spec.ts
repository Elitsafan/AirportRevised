import { TestBed } from '@angular/core/testing';
import { StationIdMapperService } from './station-id-mapper.service';

describe('StationIdMapperService', () => {
  let service: StationIdMapperService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(StationIdMapperService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
