using Amazon;
using Amazon.Extensions.NETCore.Setup;
using JobManager.API.Entities;
using JobManager.API.Persistence;
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

app.MapGet("/api/jobs", async (AppDbContext db) =>
{
    var jobs = await db.Jobs.ToListAsync();
    return Results.Ok(jobs);
});

app.MapPost("/api/jobs/{id}/applications", async (int id, JobApplication application, [FromServices] AppDbContext db) =>
{
    var jobExists = await db.Jobs.AnyAsync(j => j.Id == id);

    if (!jobExists)
        return Results.NotFound();

    application.JobId = id;

    await db.JobApplications.AddAsync(application);
    await db.SaveChangesAsync();

    return Results.NoContent();
});

app.MapPut("/api/applications/{id}", async (int id, IFormFile file, [FromServices] AppDbContext db) =>
{
    if(file == null || file.Length == 0)
        return Results.BadRequest();

    var extensions = Path.GetExtension(file.Name);

    var validExtensions = new List<string> {".pdf", ".docx" };

    if(!validExtensions.Contains(extensions))
        return Results.BadRequest();

    var key = $"job-applications/{id}-{file.FileName}";

    var application = await db.JobApplications.SingleOrDefaultAsync(ja => ja.Id == id);

    if (application == null)
        return Results.NotFound();

    application.CvUrl = key;

    await db.SaveChangesAsync();

    return Results.NoContent();
});

await app.RunAsync();