using BahmanM.Flow;
using Microsoft.Extensions.DependencyInjection;

namespace ShouldIGoOutside;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var services = new ServiceCollection();
        ConfigureServices(services);

        var serviceProvider = services.BuildServiceProvider();

        Console.WriteLine("Determining if you should go outside...");

        var recommendationService = serviceProvider.GetRequiredService<RecommendationService>();

        // Get the Flow recipe from the service
        var recommendationFlow = recommendationService.GetRecommendation();

        // Execute the Flow
        var outcome = await FlowEngine.ExecuteAsync(recommendationFlow);

        Console.WriteLine("\n----------------------------------------");

        // Handle the final outcome
        var finalMessage = outcome switch
        {
            Success<string> s => $"Recommendation: {s.Value}",
            Failure<string> f => $"Sorry, the process failed: {f.Exception.Message}",
            _ => "An unknown error occurred."
        };

        Console.WriteLine(finalMessage);
        Console.WriteLine("----------------------------------------");
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddHttpClient<IGeolocationClient, GeolocationClient>();
        services.AddHttpClient<IWeatherClient, WeatherClient>();
        services.AddSingleton<RecommendationService>();
    }
}
