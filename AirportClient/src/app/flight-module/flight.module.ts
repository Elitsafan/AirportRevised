import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FlightsIndexComponent } from './components/flights-index/flights-index.component';
import { FlightComponent } from './components/flight/flight.component';
import { FlightListComponent } from './components/flight-list/flight-list.component';
import { LoadingModule } from '../loading-module/loading.module';

@NgModule({
  declarations: [
    FlightComponent,
    FlightListComponent,
    FlightsIndexComponent
  ],
  imports: [
    CommonModule,
    LoadingModule
  ],
  exports: [
    FlightComponent,
    FlightsIndexComponent
  ]
})
export class FlightModule { }
