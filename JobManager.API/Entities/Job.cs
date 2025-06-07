using System.Text.Json.Serialization;

namespace JobManager.API.Entities;

public class Job
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public decimal MinSalary { get; set; }
    public decimal MaxSalary { get; set; }
    public string? Company { get; set; }

    [JsonIgnore]
    public List<JobApplication> Applications { get; set; }
}