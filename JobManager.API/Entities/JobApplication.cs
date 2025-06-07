using System.Text.Json.Serialization;

namespace JobManager.API.Entities;

public class JobApplication
{
    public int Id { get; set; }
    public int JobId { get; set; }
    [JsonIgnore]
    public Job Job { get; set; }
    public string? CandidateName { get; set; }
    public string? CandidateEmail { get; set; }
    public string? CvUrl { get; set; }
}