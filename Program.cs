using Amazon.CloudWatchLogs;
using Serilog;
using Serilog.Context;
using Serilog.Events;
using Serilog.Formatting.Json;
using Serilog.Sinks.AwsCloudWatch;
using Serilog.Sinks.Slack.Core;

var builder = WebApplication.CreateBuilder(args);

Serilog.Debugging.SelfLog.Enable(Console.Error);

bool SourceContextContains(LogEvent logEvent, string keyword)
    => logEvent.Properties.GetValueOrDefault("SourceContext") is ScalarValue sv
        && sv.Value != null
        && sv.Value.ToString().ToLowerInvariant().Contains(keyword.ToLowerInvariant()
    );

builder.Host.UseSerilog((context, config) =>
{
    var webhook = Environment.GetEnvironmentVariable("SLACK_WEBHOOK");
    var client = new AmazonCloudWatchLogsClient();
    var options = new CloudWatchSinkOptions
    {
        LogGroupName = $"testLogGroup/chuck-testing",
        TextFormatter = new JsonFormatter(),
        BatchSizeLimit = 100,
        QueueSizeLimit = 1000,
        Period = TimeSpan.FromSeconds(5),
        CreateLogGroup = false,
        RetryAttempts = 1,
    };

    config
        .Enrich.FromLogContext()
        .WriteTo.Console().MinimumLevel.Debug()
        // Slack target requires a valid webhook endpoint.
        .WriteTo.Slack(
            webhook,
            renderMessageImplementation: SlackLogger.RenderMessage)
            .MinimumLevel.Fatal()
        // CloudWatch target requires IAM permissions to be configured correctly
        .WriteTo.AmazonCloudWatch(
            options,
            client)
            .Filter.ByIncludingOnly(l => SourceContextContains(l, "Background"))
            .MinimumLevel.Error();
});

var app = builder.Build();

if(app.Environment.IsEnvironment("container"))
{
    app.Urls.Add("http://0.0.0.0:8080"); // Supoorts AppRunner
}

Log.Information("Added Slack!");

app.MapGet("/log/{message}", (string message) =>
{
    using (LogContext.PushProperty("SessionId", Guid.NewGuid().ToString("N")))
    {
        Log.Information($"Testing INF: {message}");
        Log.Error($"Testing ERR: {message}");
        try
        {
            throw new Exception("Something bad just happened...");
        }
        catch (Exception ex) {
            Log.Fatal(ex, $"Testing FTL: {message}");
        }
    }
})
.WithName("Log");

app.Run();