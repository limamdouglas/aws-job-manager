using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Extensions.NETCore.Setup;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SimpleSystemsManagement.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using JobManager.API.Entities;
using JobManager.API.Persistence;
using JobManager.API.Persistence.Models;
using JobManager.API.Worker;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Configuration.AddSystemsManager(source =>
{
    source.AwsOptions = new AWSOptions
    {
        Region = RegionEndpoint.SAEast1
    };

    source.Path = "/";
    source.ReloadAfter = TimeSpan.FromSeconds(30);
});
var connectionString = builder.Configuration.GetConnectionString("AppDb");
builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlServer(connectionString));

builder.Services.AddHostedService<JobApplicationNotificationWorker>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPost("/api/jobs", async (Job job, AppDbContext db) =>
{
    await db.Jobs.AddAsync(job);
    await db.SaveChangesAsync();

    return Results.Created($"/api/jobs/{job.Id}", job);
});

app.MapPost("/api/v2/jobs", async (Job job) =>
{
    var client = new AmazonDynamoDBClient(RegionEndpoint.SAEast1);
    var db = new DynamoDBContext(client);

    var model = JobDbModel.FromEntity(job);

    await db.SaveAsync(model);

    return Results.Created($"/api/jobs/{job.Id}", job);
});

app.MapGet("/api/jobs", async (AppDbContext db) =>
{
    var jobs = await db.Jobs.ToListAsync();
    return Results.Ok(jobs);
});

app.MapGet("/api/v2/jobs", async () =>
{
    var client = new AmazonDynamoDBClient(RegionEndpoint.SAEast1);
    var db = new DynamoDBContext(client);

    var jobs = await db.ScanAsync<JobDbModel>([]).GetRemainingAsync();
    return Results.Ok(jobs);
});

app.MapPost("/api/jobs/{id}/applications", async (int id, JobApplication application, 
    [FromServices] AppDbContext db, [FromServices] IConfiguration configuration) =>
{
    var jobExists = await db.Jobs.AnyAsync(j => j.Id == id);

    if (!jobExists)
        return Results.NotFound();

    application.JobId = id;

    await db.JobApplications.AddAsync(application);
    await db.SaveChangesAsync();

    var client = new AmazonSQSClient(RegionEndpoint.SAEast1);
    
    var message = $"New application received for Job {id} from {application.CandidateName} | {application.CandidateEmail}";
    var request = new SendMessageRequest
    {
        QueueUrl = configuration.GetValue<string>("AWS:SQSQueueUrl"),
        MessageBody = message
    };

    var result = await client.SendMessageAsync(request);

    return Results.NoContent();
});

app.MapPut("/api/applications/{id}", async (int id, IFormFile file, [FromServices] AppDbContext db) =>
{
    if (file == null || file.Length == 0)
        return Results.BadRequest();

    var extensions = Path.GetExtension(file.Name);

    var validExtensions = new List<string> { ".pdf", ".docx" };

    if (!validExtensions.Contains(extensions))
        return Results.BadRequest();

    var client = new AmazonS3Client(RegionEndpoint.SAEast1);

    var bucketName = "awsjobmanager";
    var key = $"job-applications/{id}-{file.FileName}";

    using var stream = file.OpenReadStream();

    var putObject = new PutObjectRequest
    {
        BucketName = bucketName,
        Key = key,
        InputStream = stream
    };

    var response = await client.PutObjectAsync(putObject);

    var application = await db.JobApplications.SingleOrDefaultAsync(ja => ja.Id == id);

    if (application == null)
        return Results.NotFound();

    application.CvUrl = key;

    await db.SaveChangesAsync();

    return Results.NoContent();
}).DisableAntiforgery();

app.MapGet("/api/applications/{id}/cv", async (int id, [FromServices] AppDbContext db) =>
{
    var baseS3Url = "https://awsjobmanager.s3.sa-east-1.amazonaws.com";

    var application = await db.JobApplications.FirstOrDefaultAsync(ja => ja.Id == id);
    if (application is null)
    {
        return Results.NotFound();
    }

    var bucketName = "awsjobmanager";

    var getRequest = new GetObjectRequest
    {
        BucketName = bucketName,
        Key = application.CvUrl
    };

    var client = new AmazonS3Client(RegionEndpoint.SAEast1);

    var response = await client.GetObjectAsync(getRequest);

    return Results.File(response.ResponseStream, response.Headers.ContentType);
});

await app.RunAsync();