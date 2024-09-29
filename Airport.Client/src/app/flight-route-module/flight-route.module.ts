import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LegComponent } from './components/leg/leg.component';
import { StationComponent } from './components/station/station.component';
import { FlightRouteComponent } from './components/flight-route/flight-route.component';
import { FlightRouteListComponent } from './components/flight-route-list/flight-route-list.component';
import { FlightModule } from '../flight-module/flight.module';
import { LoadingModule } from '../loading-module/loading.module';

@NgModule({
  declarations: [
    LegComponent,
    StationComponent,
    FlightRouteComponent,
    FlightRouteListComponent
  ],
  imports: [
    CommonModule,
    FlightModule,
    LoadingModule
  ],
  exports: [FlightRouteListComponent]
})
export class FlightRouteModule { }
