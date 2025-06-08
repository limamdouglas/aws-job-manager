using JobManager.API.Entities;
using System.Text.Json.Serialization;

namespace JobManager.API.Persistence.Models;

public class JobApplicationDbModel
{
    public string Id { get; set; }
    public string CandidateName { get; set; }
    public string CandidateEmail { get; set; }
    public string CvUrl { get; set; }
}