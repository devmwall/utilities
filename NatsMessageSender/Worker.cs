using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using NATS.Net;

namespace NatsMessageSender;

public sealed class Worker(
    ILogger<Worker> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var url = "nats://127.0.0.1:4222";
        var subject = "orders.local";
        var filePath = "C:\\Code\\demos\\OrleansProject\\Files\\Orders.jsonl";

        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            logger.LogError("Input file not found: {FilePath}", filePath);
            return;
        }

        // NatsClient supports PublishAsync with string payloads via built-in serialization. :contentReference[oaicite:1]{index=1}
        var opts = new NatsOpts { Url = url, Name = "NatsMessageSender" };
        await using var nats = new NatsClient(opts);

        logger.LogInformation("Connected to NATS at {Url}. Publishing to {Subject}. Reading {FilePath}", url, subject, filePath);

        using var fs = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read
        );

        using var reader = new StreamReader(fs);

        long sent = 0;

        while (!reader.EndOfStream && !stoppingToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            // Each line -> one message
            await nats.PublishAsync(subject: subject, data: line, cancellationToken: stoppingToken);
            sent++;

            if (sent % 1000 == 0)
                logger.LogInformation("Published {Count} messages...", sent);
        }

        logger.LogInformation("Done. Published {Count} messages.", sent);
    }
}
