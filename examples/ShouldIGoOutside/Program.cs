using BahmanM.Flow;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ShouldIGoOutside;

/// <summary>
/// The entry point of the application, responsible for setting up dependency injection,
/// composing the final Flow, and handling its outcome.
/// </summary>
public static class Program
{
    public static async Task Main()
    {
        // 1️⃣ Set up the DI container.
        var services = new ServiceCollection();
        ConfigureServices(services);
        await using var serviceProvider = services.BuildServiceProvider();

        Console.WriteLine("Determining if you should go outside...");

        // Support Ctrl+C cancellation for the whole process.
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            Console.WriteLine("\n--> Cancellation requested. Shutting down.");
            e.Cancel = true;
            cts.Cancel();
        };

        // 2️⃣ Get the main service and define the overall Flow recipe.
        var recommendationService = serviceProvider.GetRequiredService<RecommendationService>();

        // The Flow is retrieved as a "recipe". Nothing has been executed yet.
        // We can still compose it further, here adding an overall deadline for the entire process.
        var recommendationFlow = recommendationService
            .GetRecommendationFlow()
            .WithTimeout(TimeSpan.FromSeconds(10));

        // 3️⃣ Execute the Flow and wait for the outcome.
        var outcome = await FlowEngine
            .ExecuteAsync(recommendationFlow, new(cts.Token));

        Console.WriteLine("\n----------------------------------------");

        // 4️⃣ Handle the final result in a type-safe way.
        var finalMessage = outcome switch
        {
            Success<string> s => $"Recommendation: {s.Value}",
            Failure<string> f => $"Sorry, the process failed: {f.Exception.Message}",
            _ => "An unknown error occurred."
        };

        Console.WriteLine(finalMessage);
        Console.WriteLine("----------------------------------------");
    }

    /// <summary>
    /// Registers all the necessary services for the application.
    /// </summary>
    private static void ConfigureServices(IServiceCollection services)
    {
        // Logging for internal diagnostics; Console is reserved for user-facing messages.
        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddSimpleConsole(options =>
            {
                options.SingleLine = true;
                options.TimestampFormat = "HH:mm:ss ";
            });
            loggingBuilder.SetMinimumLevel(LogLevel.Information);
        });

        // Register our API clients with HttpClientFactory support.
        services.AddHttpClient<IGeolocationClient, GeolocationClient>();
        services.AddHttpClient<IWeatherClient, WeatherClient>();

        // Register our main service that composes the Flows.
        services.AddSingleton<RecommendationService>();
    }
}
