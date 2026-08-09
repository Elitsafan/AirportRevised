export const environment = {
  remoteUrl: 'https://airport.api.elitzafan.com',
  loginEP: '/api/Auth/Login',
  airportHubEP: '/airporthub',
  startEP: '/api/Airport/Start',
  statusEP: '/api/Airport/Status',
  summaryEP: '/api/Airport/Summary',
  flightsEP: '/api/Flights',
  flightRunDone: 'FlightRunDone',
  stationCleared: 'StationCleared',
  flightRunStarted: 'FlightRunStarted',
  timeout: {
    minutes: 1,
    hours: 0
  },
  http: {
    retryCount: 5,
    retryDelayMs: 5000
  },
  loginCredentials: {
    username: "admin",
    password: "password123"
  },
  minutesPassedArg: "minutesPassed",
  flightRefreshMinutes: 7,
  //landingColors: [
  //  "#666666",
  //  "#52657A",
  //  "#3D638F",
  //  "#2962A3",
  //  "#1461B8",
  //  "#005FCC",
  //],
  landingColors: [
    "#974302",
    "#CA5902",
    "#FC6F03",
    "#FD8C35",
    "#FEB781",
    "#341C09",
  ],
  departureColors: [
    "#ABABAB",
    "#9AAABC",
    "#89A9CD",
    "#78A7DD",
    "#68A6EE",
    "#57A5FF",
  ]
};
