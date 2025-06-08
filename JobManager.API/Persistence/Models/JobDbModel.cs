using Amazon.DynamoDBv2.DataModel;
using JobManager.API.Entities;
using System.Text.Json.Serialization;

namespace JobManager.API.Persistence.Models;

[DynamoDBTable("JobDb")]
public class JobDbModel
{
    [DynamoDBHashKey]
    public string Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public decimal MinSalary { get; set; }
    public decimal MaxSalary { get; set; }
    public string Company { get; set; }

    [JsonIgnore]
    public List<JobApplication> Applications { get; set; }

    public static JobDbModel FromEntity(Job job)
        => new JobDbModel
        {
            Id = job.Id.ToString(),
            Title = job.Title!,
            Description = job.Description!,
            MinSalary = job.MinSalary,
            MaxSalary = job.MaxSalary,
            Company = job.Company!,
            Applications = []
        };
}