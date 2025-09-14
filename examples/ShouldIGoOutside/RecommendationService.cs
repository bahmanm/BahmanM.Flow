
using BahmanM.Flow;

namespace ShouldIGoOutside;

public class RecommendationService
{
    private readonly IGeolocationClient _geolocationClient;
    private readonly IWeatherClient _weatherClient;

    public RecommendationService(IGeolocationClient geolocationClient, IWeatherClient weatherClient)
    {
        _geolocationClient = geolocationClient;
        _weatherClient = weatherClient;
    }

    public IFlow<string> GetRecommendation()
    {
        return _geolocationClient.GetGeolocation()
            .Chain(location =>
            {
                Console.WriteLine($"--> Determined location: {location.City}");

                // After getting the location, fetch weather and air quality in parallel.
                var weatherFlow = _weatherClient.GetWeather(location).Select(w => (object)w);
                var airQualityFlow = _weatherClient.GetAirQuality(location)
                    .DoOnFailure(ex => Console.WriteLine($"--> Air Quality API failed: {ex.Message}. Recovering..."))
                    .Recover(ex => Flow.Succeed(new AirQuality(0)))
                    .Select(aq => (object)aq); // Recover with a default 'Good' value.

                return Flow.All(weatherFlow, airQualityFlow)
                    .Select(results =>
                    {
                        var weather = (Weather)results[0];
                        var airQuality = (AirQuality)results[1];

                        Console.WriteLine($"--> Fetched Weather: {weather.Temperature}°C");
                        Console.WriteLine($"--> Fetched Air Quality (AQI): {airQuality.Aqi}");

                        // Apply business logic to the aggregated results.
                        return MakeRecommendation(weather, airQuality);
                    });
            });
    }

    private static string MakeRecommendation(Weather weather, AirQuality airQuality)
    {
        if (weather.IsRaining())
        {
            return "No, it's raining!";
        }

        if (weather.Temperature < 10)
        {
            return "No, it's too cold!";
        }

        if (airQuality.Aqi > 100)
        {
            return "No, the air quality is poor.";
        }

        return "Yes, it's a great day to go outside!";
    }
}

// Helper extension method for readability
public static class WeatherExtensions
{
    // Weather codes from Open-Meteo documentation for rain
    public static bool IsRaining(this Weather weather) => weather.WeatherCode is >= 51 and <= 67 or >= 80 and <= 82;
}
