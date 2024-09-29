import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { PageNotFoundComponent } from './components/page-not-found/page-not-found.component';
import { FlightsIndexComponent } from './flight-module/components/flights-index/flights-index.component';
import { FlightSummaryListComponent } from './flight-summary-module/components/flight-summary-list/flight-summary-list.component';
import { FlightRouteListComponent } from './flight-route-module/components/flight-route-list/flight-route-list.component';

const routes: Routes = [
  { path: '', redirectTo: '/flights', pathMatch: 'full' }, 
  { path: 'flights', component: FlightsIndexComponent },
  { path: 'stations', component: FlightRouteListComponent },
  { path: 'summary', component: FlightSummaryListComponent },
  { path: '**', component: PageNotFoundComponent },  
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule],
})
export class AppRoutingModule { }
