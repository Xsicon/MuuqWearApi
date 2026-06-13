using Microsoft.AspNetCore.Http;
using MuuqWear.API.Shared;
using MuuqWear.Application.Interfaces;
using MuuqWear.Application.Shared;
using MuuqWear.Model.DTO.JobApplicationDTO;
using MuuqWear.Model.DTO.JobPostingDTO;
using MuuqWear.Model.Models.JobApplication;
using MuuqWear.Model.Models.JobPosting;

namespace MuuqWear.Application.Service;

public class JobPostingService : IJobPostingService
{
    private readonly Supabase.Client _client;

    public JobPostingService(SupabaseAdminClientFactory factory)
    {
        _client = factory.CreateClient();
    }

    // =============================================
    // GET ALL — admin: returns open + closed
    // =============================================
    public async Task<Response<List<JobPostingDTO>>> GetAll()
    {
        try
        {
            var result = await _client.From<JobPosting>()
                .Order("created_at",
                    Supabase.Postgrest.Constants.Ordering.Descending)
                .Get();

            var jobs = result.Models.Select(ToDTO).ToList();

            return Response<List<JobPostingDTO>>.SuccessResponse(
                jobs, "Job postings fetched");
        }
        catch (Exception ex)
        {
            return Response<List<JobPostingDTO>>.Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // GET OPEN — public Career page
    // =============================================
    public async Task<Response<List<JobPostingDTO>>> GetOpen()
    {
        try
        {
            var result = await _client.From<JobPosting>()
                .Filter("status",
                    Supabase.Postgrest.Constants.Operator.Equals, "open")
                .Order("opened_at",
                    Supabase.Postgrest.Constants.Ordering.Descending)
                .Get();

            var jobs = result.Models.Select(ToDTO).ToList();

            return Response<List<JobPostingDTO>>.SuccessResponse(
                jobs, "Open job postings fetched");
        }
        catch (Exception ex)
        {
            return Response<List<JobPostingDTO>>.Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // GET BY ID
    // =============================================
    public async Task<Response<JobPostingDTO>> GetById(Guid id)
    {
        try
        {
            var job = await _client.From<JobPosting>()
                .Filter("id",
                    Supabase.Postgrest.Constants.Operator.Equals, id.ToString())
                .Single();

            if (job == null)
                return Response<JobPostingDTO>.Fail("Job posting not found");

            return Response<JobPostingDTO>.SuccessResponse(
                ToDTO(job), "Job posting fetched");
        }
        catch (Exception ex)
        {
            return Response<JobPostingDTO>.Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // CREATE — new jobs are immediately open
    // =============================================
    public async Task<Response<JobPostingDTO>> Create(CreateJobPostingDTO request)
    {
        try
        {
            var now = DateTime.UtcNow;

            var result = await _client.From<JobPosting>()
                .Insert(new JobPosting
                {
                    Id = Guid.NewGuid(),
                    Slug = request.Slug,
                    Title = request.Title,
                    Department = request.Department,
                    Location = request.Location,
                    Type = request.Type,
                    Description = request.Description,
                    Status = "open",
                    ApplicationCount = 0,
                    CreatedAt = now,
                    OpenedAt = now
                });

            var created = result.Models.FirstOrDefault();
            if (created == null)
                return Response<JobPostingDTO>.Fail("Failed to create job posting");

            return Response<JobPostingDTO>.SuccessResponse(
                ToDTO(created), "Job posting created");
        }
        catch (Exception ex)
        {
            return Response<JobPostingDTO>.Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // UPDATE — does not change status
    // =============================================
    public async Task<Response<JobPostingDTO>> Update(
        Guid id, UpdateJobPostingDTO request)
    {
        try
        {
            var result = await _client.From<JobPosting>()
                .Filter("id",
                    Supabase.Postgrest.Constants.Operator.Equals, id.ToString())
                .Set(x => x.Slug, request.Slug)
                .Set(x => x.Title, request.Title)
                .Set(x => x.Department, request.Department)
                .Set(x => x.Location, request.Location)
                .Set(x => x.Type, request.Type)
                .Set(x => x.Description!, request.Description)
                .Update();

            var updated = result.Models.FirstOrDefault();
            if (updated == null)
                return Response<JobPostingDTO>.Fail("Failed to update job posting");

            return Response<JobPostingDTO>.SuccessResponse(
                ToDTO(updated), "Job posting updated");
        }
        catch (Exception ex)
        {
            return Response<JobPostingDTO>.Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // DELETE
    // =============================================
    public async Task<Response<bool>> Delete(Guid id)
    {
        try
        {
            await _client.From<JobPosting>()
                .Filter("id",
                    Supabase.Postgrest.Constants.Operator.Equals, id.ToString())
                .Delete();

            return Response<bool>.SuccessResponse(true, "Job posting deleted");
        }
        catch (Exception ex)
        {
            return Response<bool>.Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // CLOSE — mark as filled / no longer accepting
    // =============================================
    public async Task<Response<JobPostingDTO>> Close(Guid id)
    {
        try
        {
            var result = await _client.From<JobPosting>()
                .Filter("id",
                    Supabase.Postgrest.Constants.Operator.Equals, id.ToString())
                .Set(x => x.Status, "closed")
                .Set(x => x.ClosedAt!, DateTime.UtcNow)
                .Update();

            var closed = result.Models.FirstOrDefault();
            if (closed == null)
                return Response<JobPostingDTO>.Fail("Failed to close job posting");

            return Response<JobPostingDTO>.SuccessResponse(
                ToDTO(closed), "Job posting closed");
        }
        catch (Exception ex)
        {
            return Response<JobPostingDTO>.Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // REOPEN — bring back to open state
    // =============================================
    public async Task<Response<JobPostingDTO>> Reopen(Guid id)
    {
        try
        {
            var result = await _client.From<JobPosting>()
                .Filter("id",
                    Supabase.Postgrest.Constants.Operator.Equals, id.ToString())
                .Set(x => x.Status, "open")
                .Set(x => x.OpenedAt!, DateTime.UtcNow)
                .Set(x => x.ClosedAt!, (DateTime?)null)
                .Update();

            var reopened = result.Models.FirstOrDefault();
            if (reopened == null)
                return Response<JobPostingDTO>.Fail("Failed to reopen job posting");

            return Response<JobPostingDTO>.SuccessResponse(
                ToDTO(reopened), "Job posting reopened");
        }
        catch (Exception ex)
        {
            return Response<JobPostingDTO>.Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // SUBMIT APPLICATION — public submission
    // =============================================
    public async Task<Response<JobApplicationDTO>> SubmitApplication(
        SubmitJobApplicationDTO request)
    {
        try
        {
            // Verify the job exists and is open before accepting an application
            var job = await _client.From<JobPosting>()
                .Filter("id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    request.JobId.ToString())
                .Single();

            if (job == null)
                return Response<JobApplicationDTO>.Fail("Job posting not found");

            if (job.Status != "open")
                return Response<JobApplicationDTO>.Fail(
                    "This position is no longer accepting applications");

            // Insert the application
            var insertResult = await _client.From<JobApplication>()
                .Insert(new JobApplication
                {
                    Id = Guid.NewGuid(),
                    JobId = request.JobId,
                    Name = request.Name,
                    Email = request.Email,
                    PortFolioUrl = request.PortfolioUrl,
                    ResumeUrl = request.ResumeUrl,
                    Status = "new",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });

            var created = insertResult.Models.FirstOrDefault();
            if (created == null)
                return Response<JobApplicationDTO>.Fail("Failed to submit application");

            // Update the job's application_count
            // Wrapped in try/catch — we don't fail the submission if count update fails
            try
            {
                await _client.From<JobPosting>()
                    .Filter("id",
                        Supabase.Postgrest.Constants.Operator.Equals,
                        request.JobId.ToString())
                    .Set(x => x.ApplicationCount, job.ApplicationCount + 1)
                    .Update();
            }
            catch
            {
                // Log and continue — application is saved; count just drifts
            }

            return Response<JobApplicationDTO>.SuccessResponse(
                ToDTO(created), "Application submitted");
        }
        catch (Exception ex)
        {
            return Response<JobApplicationDTO>.Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // GET APPLICATIONS FOR A JOB — admin only
    // =============================================
    public async Task<Response<List<JobApplicationDTO>>> GetApplicationsByJob(Guid jobId)
    {
        try
        {
            var result = await _client.From<JobApplication>()
                .Filter("job_id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    jobId.ToString())
                .Order("created_at",
                    Supabase.Postgrest.Constants.Ordering.Descending)
                .Get();

            var apps = result.Models.Select(ToDTO).ToList();

            return Response<List<JobApplicationDTO>>.SuccessResponse(
                apps, "Applications fetched");
        }
        catch (Exception ex)
        {
            return Response<List<JobApplicationDTO>>.Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // GET APPLICATION BY ID — admin only
    // =============================================
    public async Task<Response<JobApplicationDTO>> GetApplicationById(Guid applicationId)
    {
        try
        {
            var app = await _client.From<JobApplication>()
                .Filter("id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    applicationId.ToString())
                .Single();

            if (app == null)
                return Response<JobApplicationDTO>.Fail("Application not found");

            return Response<JobApplicationDTO>.SuccessResponse(
                ToDTO(app), "Application fetched");
        }
        catch (Exception ex)
        {
            return Response<JobApplicationDTO>.Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // UPDATE APPLICATION STATUS / NOTES — admin only
    // =============================================
    public async Task<Response<JobApplicationDTO>> UpdateApplicationStatus(
        Guid applicationId, UpdateJobApplicationStatusDTO request)
    {
        try
        {
            // Validate status against known values
            var validStatuses = new[]
            {
            "new", "reviewing", "interviewing", "rejected", "hired"
        };

            if (!validStatuses.Contains(request.Status))
                return Response<JobApplicationDTO>.Fail(
                    "Invalid status. Allowed values: " + string.Join(", ", validStatuses));

            var result = await _client.From<JobApplication>()
                .Filter("id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    applicationId.ToString())
                .Set(x => x.Status, request.Status)
                .Set(x => x.Notes!, request.Notes)
                .Set(x => x.UpdatedAt!, DateTime.UtcNow)
                .Update();

            var updated = result.Models.FirstOrDefault();
            if (updated == null)
                return Response<JobApplicationDTO>.Fail("Failed to update application");

            return Response<JobApplicationDTO>.SuccessResponse(
                ToDTO(updated), "Application updated");
        }
        catch (Exception ex)
        {
            return Response<JobApplicationDTO>.Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // DELETE APPLICATION — admin only
    // =============================================
    public async Task<Response<bool>> DeleteApplication(Guid applicationId)
    {
        try
        {
            // Fetch the application first to know which job's count to decrement
            var app = await _client.From<JobApplication>()
                .Filter("id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    applicationId.ToString())
                .Single();

            if (app == null)
                return Response<bool>.Fail("Application not found");

            // Delete the application
            await _client.From<JobApplication>()
                .Filter("id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    applicationId.ToString())
                .Delete();

            // Decrement the job's application_count
            try
            {
                var job = await _client.From<JobPosting>()
                    .Filter("id",
                        Supabase.Postgrest.Constants.Operator.Equals,
                        app.JobId.ToString())
                    .Single();

                if (job != null)
                {
                    await _client.From<JobPosting>()
                        .Filter("id",
                            Supabase.Postgrest.Constants.Operator.Equals,
                            app.JobId.ToString())
                        .Set(x => x.ApplicationCount, Math.Max(0, job.ApplicationCount - 1))
                        .Update();
                }
            }
            catch
            {
                // Application deleted; count update failed — drift is acceptable
            }

            return Response<bool>.SuccessResponse(true, "Application deleted");
        }
        catch (Exception ex)
        {
            return Response<bool>.Fail("Error: " + ex.Message);
        }
    }


    public async Task<Response<string>> UploadResume(IFormFile file)
    {
        try
        {
            var fileName =
                $"resumes/{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

            using var stream = file.OpenReadStream();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            var bytes = ms.ToArray();

            await _client.Storage
                .From("app-images")  // or use a dedicated "resumes" bucket
                .Upload(bytes, fileName, new Supabase.Storage.FileOptions
                {
                    ContentType = file.ContentType,
                    Upsert = false
                });

            var url = _client.Storage
                .From("app-images")
                .GetPublicUrl(fileName);

            return Response<string>.SuccessResponse(url, "Resume uploaded");
        }
        catch (Exception ex)
        {
            return Response<string>.Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // MAPPING HELPER — JobApplication → DTO
    // =============================================
    private static JobApplicationDTO ToDTO(JobApplication app) => new()
    {
        Id = app.Id,
        JobId = app.JobId,
        Name = app.Name,
        Email = app.Email,
        PortfolioUrl = app.PortFolioUrl,
        ResumeUrl = app.ResumeUrl,
        Status = app.Status,
        Notes = app.Notes,
        CreatedAt = app.CreatedAt,
        UpdatedAt = app.UpdatedAt
    };


    // =============================================
    // MAPPING HELPER
    // =============================================
    private static JobPostingDTO ToDTO(JobPosting job) => new()
    {
        Id = job.Id,
        Slug = job.Slug,
        Title = job.Title,
        Department = job.Department,
        Location = job.Location,
        Type = job.Type,
        Description = job.Description,
        Status = job.Status,
        ApplicationCount = job.ApplicationCount,
        CreatedAt = job.CreatedAt,
        OpenedAt = job.OpenedAt,
        ClosedAt = job.ClosedAt
    };
}
