import { HttpClientModule } from '@angular/common/http';
import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { AppRoutingModule } from './app-routing.module';

import { AppComponent } from './app.component';
import { FlightModule } from './flight-module/flight.module';
import { PageNotFoundComponent } from './components/page-not-found/page-not-found.component';
import { FlightSummaryModule } from './flight-summary-module/flight-summary.module';
import { FlightRouteModule } from './flight-route-module/flight-route.module';
import { RouterLink } from '@angular/router';

@NgModule({
  declarations: [
    AppComponent,
    PageNotFoundComponent,
  ],
  imports: [
    BrowserModule,
    HttpClientModule,
    RouterLink,
    AppRoutingModule,
    FlightModule,
    FlightSummaryModule,
    FlightRouteModule,
  ],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule { }
