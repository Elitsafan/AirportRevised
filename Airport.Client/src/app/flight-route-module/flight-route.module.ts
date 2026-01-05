import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LegComponent } from './components/leg/leg.component';
import { StationComponent } from './components/station/station.component';
import { FlightRouteComponent } from './components/flight-route/flight-route.component';
import { FlightRouteListComponent } from './components/flight-route-list/flight-route-list.component';
import { FlightModule } from '../flight-module/flight.module';
import { LoadingModule } from '../loading-module/loading.module';
import { SharedModule } from '../shared-module/shared.module';
import { RoutesContainerComponent } from './components/routes-container/routes-container.component';

@NgModule({
  declarations: [
    LegComponent,
    StationComponent,
    FlightRouteComponent,
    FlightRouteListComponent,
    RoutesContainerComponent
  ],
  imports: [
    CommonModule,
    FlightModule,
    LoadingModule,
    SharedModule
  ],
  exports: [FlightRouteListComponent]
})
export class FlightRouteModule { }
