import { Component, OnInit } from '@angular/core';
import { AirportService } from './services/airport.service';
import { SignalrService } from './services/signalr.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit {
  constructor(private airportSvc: AirportService, private signalRSvc: SignalrService) { }

  async ngOnInit(): Promise<void> {
    if (this.airportSvc.hasStarted)
      await this.signalRSvc.startConnection();
    else
      this.airportSvc.start()
        .subscribe({
          next: async (res) => {
            //console.log(res);
            await this.signalRSvc.startConnection();
          },
          error: (error) => console.log(error)
        });
  }
  title = 'Airport Client';
}
