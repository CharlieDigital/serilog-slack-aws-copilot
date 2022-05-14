using System.Dynamic;
using Serilog.Events;

/// <summary>
/// Provides the log formatting for Slack messages.  We override to include properties.
///
/// From:
/// https://github.com/serilog-contrib/serilog-sinks-slackclient/blob/master/src/Serilog.Sinks.Slack.Core/Sinks/Slack/SlackSink.cs
/// </summary>
public static class SlackLogger
{
    public static string RenderMessage(
        LogEvent logEvent,
        string username,
        string iconEmoji
    )
    {
        dynamic body = new ExpandoObject();
        body.text = logEvent.RenderMessage();

        if (!string.IsNullOrWhiteSpace(username))
        {
            body.username = username;
        }

        if (!string.IsNullOrWhiteSpace(iconEmoji))
        {
            body.icon_emoji = iconEmoji;
        }

        body.attachments = WrapInAttachment(logEvent).ToArray();

        return System.Text.Json.JsonSerializer.Serialize(body);
    }

    private static string GetAttachmentColor(LogEventLevel level)
    {
        switch (level)
        {
            case LogEventLevel.Information:
                return "#5bc0de";
            case LogEventLevel.Warning:
                return "#f0ad4e";
            case LogEventLevel.Error:
            case LogEventLevel.Fatal:
                return "#d9534f";
            default:
                return "#777";
        }
    }

    private static object CreateAttachmentField(string title, string value, bool @short = true)
    {
        return new { title, value, @short };
    }

    private static object WrapInAttachment(Exception ex)
    {
        return new
        {
            title = "Exception",
            fallback = string.Format("Exception: {0} \n {1}", ex.Message, ex.StackTrace),
            color = GetAttachmentColor(LogEventLevel.Fatal),
            fields = new[]
            {
                    CreateAttachmentField("Message", ex.Message),
                    CreateAttachmentField("Type", "`"+ex.GetType().Name+"`"),
                    CreateAttachmentField("Stack Trace", "```"+ex.StackTrace+"```", false)
                },
            mrkdwn_in = new[] { "fields" }
        };
    }

    private static IEnumerable<dynamic> WrapInAttachment(LogEvent log)
    {
        string GetPropertyValue(string propertyName)
            => (log.Properties.GetValueOrDefault(propertyName) is ScalarValue sv && sv.Value != null)
                ? Convert.ToString(sv.Value)
                : string.Empty;

        // List of custom properties we want to include.
        var properties = new[]
        {
            ("SessionId", true)
        };

        var fields = new List<object>
        {
            CreateAttachmentField("Level", log.Level.ToString()),
            CreateAttachmentField("Timestamp", log.Timestamp.ToString())
        };

        foreach (var (propertyName, shortContent) in properties)
        {
            Console.WriteLine($"Type: {log.Properties.Keys.Count()}");

            Console.WriteLine($"Property: {propertyName}");

            var text = GetPropertyValue(propertyName);

            Console.WriteLine($"Property Value: {text}");

            if (string.IsNullOrEmpty(text))
            {
                continue;
            }

            fields.Add(CreateAttachmentField(propertyName, $"`{text}`", shortContent));
        }

        var result = new List<dynamic>
            {
                new
                {
                    fallback = string.Format("[{0}]{1}", log.Level, log.RenderMessage()),
                    color = GetAttachmentColor(log.Level),
                    fields = fields.ToArray()
                }
            };

        if (log.Exception != null)
        {
            result.Add(WrapInAttachment(log.Exception));
        }

        return result;
    }
}