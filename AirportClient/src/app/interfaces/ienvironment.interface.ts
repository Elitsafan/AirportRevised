export interface IEnvironment {
  remoteUrl: string;
  airportHubEP: string;
  startEP: string;
  statusEP: string;
  summaryEP: string;
  flightsEP: string;
  flightRunDone: string;
  stationCleared: string;
  flightRunStarted: string;
  timeout: { minutes: number; hours: number };
  http: { retryCount: number; retryDelayMs: number };
  loginCredentials: { username: string; password: string };
  minutesPassedArg: string;
  flightRefreshMinutes: number;
  landingColors: string[];
  departureColors: string[];
}
