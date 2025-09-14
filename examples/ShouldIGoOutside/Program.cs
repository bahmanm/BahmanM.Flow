using BahmanM.Flow;
using Microsoft.Extensions.DependencyInjection;

namespace ShouldIGoOutside;

public static class Program
{
    public static async Task Main()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);

        await using var serviceProvider = services.BuildServiceProvider();

        Console.WriteLine("Determining if you should go outside...");

        // Support Ctrl+C cancellation
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

        var recommendationService = serviceProvider.GetRequiredService<RecommendationService>();

        // Get the Flow recipe from the service and add an overall deadline
        var recommendationFlow = recommendationService
            .GetRecommendationFlow()
            .WithTimeout(TimeSpan.FromSeconds(10));

        // Execute the Flow with external cancellation
        var outcome = await FlowEngine.ExecuteAsync(recommendationFlow, new(cts.Token));

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
