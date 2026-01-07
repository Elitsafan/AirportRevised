import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FlightSummaryComponent } from './components/flight-summary/flight-summary.component';
import { FlightSummaryListComponent } from './components/flight-summary-list/flight-summary-list.component';
import { StationSummaryComponent } from './components/station-summary/station-summary.component';
import { StationSummaryListComponent } from './components/station-summary-list/station-summary-list.component';
import { LoadingModule } from '../loading-module/loading.module';
import { SharedModule } from '../shared-module/shared.module';

@NgModule({
  declarations: [
    FlightSummaryComponent,
    FlightSummaryListComponent,
    StationSummaryComponent,
    StationSummaryListComponent
  ],
  imports: [
    CommonModule,
    SharedModule,
    LoadingModule
  ],
  exports: [
    FlightSummaryListComponent
  ]
})
export class FlightSummaryModule { }
