import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class StationIdMapperService {
  private idMap = new Map<string, number>();
  private counter = 1;

  getSequentialId(stationId: string): number {
    if (!this.idMap.has(stationId)) {
      this.idMap.set(stationId, this.counter++);
    }
    return this.idMap.get(stationId)!;
  }

  reset(): void {
    this.idMap.clear();
    this.counter = 1;
  }
}
