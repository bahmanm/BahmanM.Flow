# 'Should I Go Outside?'

A simple console application that demonstrates how to use Flows to compose several real-world, failable API calls into a single, readable, resilient process.

It determines, using a silly algorithm 😅, if you should go outside by fetching your current location, weather, and air quality, and then applying a set of business rules to them.

# Quick Overview

It's got three main components, which are registered for DI in `Program.cs`.

```mermaid
classDiagram
  Program ..> RecommendationService
  RecommendationService ..> IGeolocationClient
  RecommendationService ..> IWeatherClient
  GeolocationClient --|> IGeolocationClient
  WeatherClient --|> IWeatherClient
  IGeolocationClient : +IFlow~Geolocation~ GetGeolocationFlow()
  IWeatherClient : +IFlow~Weather~ GetWeatherFlow(Geolocation)
  IWeatherClient : +IFlow~AirQuality~ GetAirQualityFlow(Geolocation)
  RecommendationService: +IFlow~string~ GetRecommendationFlow()
```

---

The high-level sequence of operations looks like this:

```mermaid
sequenceDiagram
    participant P as Program
    participant R as RecommendationService
    participant G as GeolocationClient
    participant W as WeatherClient

    P->>R: GetRecommendationFlow()
    R->>G: GetGeolocationFlow()
    G-->>R: Flow~Geolocation~
    R->>W: GetWeatherFlow(loc)
    R->>W: GetAirQualityFlow(loc)
    W-->>R: Flow~Weather~
    W-->>R: Flow~AirQuality~
    R-->>P: Flow~string~
    P->>FlowEngine: ExecuteAsync(flow)
    FlowEngine-->>P: Outcome~string~
```

# How to Run

From the root of the repository, run the following command:

```
dotnet run --project examples/ShouldIGoOutside
```

For example: 

```
Determining if you should go outside...
--> Determined location: San Jose
--> Air Quality API failed. Recovering with default AQI.
--> Fetched Weather: 25.1°C
--> Fetched Air Quality (AQI): 0

----------------------------------------
Recommendation: Yes, it's a great day to go outside!
----------------------------------------
```
