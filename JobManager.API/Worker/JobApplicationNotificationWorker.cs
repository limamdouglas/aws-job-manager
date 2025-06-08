
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using System.Net;

namespace JobManager.API.Worker;

public class JobApplicationNotificationWorker : BackgroundService
{
    private readonly IConfiguration _configuration;

    public JobApplicationNotificationWorker(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var client = new AmazonSQSClient(RegionEndpoint.SAEast1);

        var queueUrl = _configuration["Aws:SqsQueueUrl"] ?? string.Empty;

        while (!stoppingToken.IsCancellationRequested)
        {
            var request = new ReceiveMessageRequest
            {
                QueueUrl = queueUrl,
                MessageAttributeNames = ["All"],
                WaitTimeSeconds = 20
            };

            var response = await client.ReceiveMessageAsync(request, stoppingToken);

            if (response.HttpStatusCode == HttpStatusCode.OK)
            {
                foreach (var message in response.Messages)
                {
                    Console.WriteLine($"Processing message: {message.Body}");
                    await client.DeleteMessageAsync(queueUrl, message.ReceiptHandle);
                }
            }
        }
    }
}