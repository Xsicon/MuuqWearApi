using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MuuqWear.API.Shared;
using MuuqWear.Application.Controllers;
using MuuqWear.Application.Interfaces;
using MuuqWear.Model.DTO.JobApplicationDTO;
using MuuqWear.Model.DTO.JobPostingDTO;

namespace MuuqWear.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "admin")]
public class JobPostingController : BaseController
{
    private readonly IJobPostingService _service;

    public JobPostingController(IJobPostingService service)
    {
        _service = service;
    }

    // =============================================
    // ADMIN: GET ALL (open + closed)
    // =============================================
    [HttpGet]
    public async Task<ActionResult<Response<List<JobPostingDTO>>>> GetAll()
    {
        var result = await _service.GetAll();
        return HandleResponse(result);
    }

    // =============================================
    // PUBLIC: GET OPEN (Career page)
    // =============================================
    [HttpGet("open")]
    [AllowAnonymous]
    public async Task<ActionResult<Response<List<JobPostingDTO>>>> GetOpen()
    {
        var result = await _service.GetOpen();
        return HandleResponse(result);
    }

    // =============================================
    // ADMIN: GET BY ID
    // =============================================
    [HttpGet("{id}")]
    public async Task<ActionResult<Response<JobPostingDTO>>> GetById(Guid id)
    {
        var result = await _service.GetById(id);
        return HandleResponse(result);
    }

    // =============================================
    // ADMIN: CREATE
    // =============================================
    [HttpPost]
    public async Task<ActionResult<Response<JobPostingDTO>>> Create(
        [FromBody] CreateJobPostingDTO request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(Response<JobPostingDTO>.Fail("Title is required"));

        if (string.IsNullOrWhiteSpace(request.Slug))
            return BadRequest(Response<JobPostingDTO>.Fail("Slug is required"));

        if (string.IsNullOrWhiteSpace(request.Department))
            return BadRequest(Response<JobPostingDTO>.Fail("Department is required"));

        if (string.IsNullOrWhiteSpace(request.Location))
            return BadRequest(Response<JobPostingDTO>.Fail("Location is required"));

        if (string.IsNullOrWhiteSpace(request.Type))
            return BadRequest(Response<JobPostingDTO>.Fail("Type is required"));

        var result = await _service.Create(request);
        return HandleResponse(result);
    }

    // =============================================
    // ADMIN: UPDATE
    // =============================================
    [HttpPut("{id}")]
    public async Task<ActionResult<Response<JobPostingDTO>>> Update(
        Guid id,
        [FromBody] UpdateJobPostingDTO request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(Response<JobPostingDTO>.Fail("Title is required"));

        if (string.IsNullOrWhiteSpace(request.Slug))
            return BadRequest(Response<JobPostingDTO>.Fail("Slug is required"));

        if (string.IsNullOrWhiteSpace(request.Department))
            return BadRequest(Response<JobPostingDTO>.Fail("Department is required"));

        if (string.IsNullOrWhiteSpace(request.Location))
            return BadRequest(Response<JobPostingDTO>.Fail("Location is required"));

        if (string.IsNullOrWhiteSpace(request.Type))
            return BadRequest(Response<JobPostingDTO>.Fail("Type is required"));

        var result = await _service.Update(id, request);
        return HandleResponse(result);
    }

    // =============================================
    // ADMIN: DELETE
    // =============================================
    [HttpDelete("{id}")]
    public async Task<ActionResult<Response<bool>>> Delete(Guid id)
    {
        var result = await _service.Delete(id);
        return HandleResponse(result);
    }

    // =============================================
    // ADMIN: CLOSE
    // =============================================
    [HttpPatch("{id}/close")]
    public async Task<ActionResult<Response<JobPostingDTO>>> Close(Guid id)
    {
        var result = await _service.Close(id);
        return HandleResponse(result);
    }

    // =============================================
    // ADMIN: REOPEN
    // =============================================
    [HttpPatch("{id}/reopen")]
    public async Task<ActionResult<Response<JobPostingDTO>>> Reopen(Guid id)
    {
        var result = await _service.Reopen(id);
        return HandleResponse(result);
    }

    // =============================================
    // PUBLIC: SUBMIT APPLICATION
    // =============================================
    [HttpPost("{jobId}/applications")]
    [AllowAnonymous]
    public async Task<ActionResult<Response<JobApplicationDTO>>> SubmitApplication(
        Guid jobId,
        [FromBody] SubmitJobApplicationDTO request)
    {
        // Ensure the URL jobId matches the body — guard against tampering
        if (request.JobId != jobId)
            return BadRequest(Response<JobApplicationDTO>.Fail(
                "Job ID mismatch"));

        // Server-side validation
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(Response<JobApplicationDTO>.Fail(
                "Name is required"));

        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
            return BadRequest(Response<JobApplicationDTO>.Fail(
                "A valid email is required"));

        if (string.IsNullOrWhiteSpace(request.ResumeUrl))
            return BadRequest(Response<JobApplicationDTO>.Fail(
                "Resume is required"));

        var result = await _service.SubmitApplication(request);
        return HandleResponse(result);
    }

    // =============================================
    // ADMIN: GET APPLICATIONS FOR A JOB
    // =============================================
    [HttpGet("{jobId}/applications")]
    public async Task<ActionResult<Response<List<JobApplicationDTO>>>> GetApplicationsByJob(
        Guid jobId)
    {
        var result = await _service.GetApplicationsByJob(jobId);
        return HandleResponse(result);
    }

    // =============================================
    // ADMIN: GET APPLICATION BY ID
    // =============================================
    [HttpGet("applications/{applicationId}")]
    public async Task<ActionResult<Response<JobApplicationDTO>>> GetApplicationById(
        Guid applicationId)
    {
        var result = await _service.GetApplicationById(applicationId);
        return HandleResponse(result);
    }

    // =============================================
    // ADMIN: UPDATE APPLICATION STATUS / NOTES
    // =============================================
    [HttpPatch("applications/{applicationId}/status")]
    public async Task<ActionResult<Response<JobApplicationDTO>>> UpdateApplicationStatus(
        Guid applicationId,
        [FromBody] UpdateJobApplicationStatusDTO request)
    {
        if (string.IsNullOrWhiteSpace(request.Status))
            return BadRequest(Response<JobApplicationDTO>.Fail(
                "Status is required"));

        var result = await _service.UpdateApplicationStatus(applicationId, request);
        return HandleResponse(result);
    }

    // =============================================
    // ADMIN: DELETE APPLICATION
    // =============================================
    [HttpDelete("applications/{applicationId}")]
    public async Task<ActionResult<Response<bool>>> DeleteApplication(Guid applicationId)
    {
        var result = await _service.DeleteApplication(applicationId);
        return HandleResponse(result);
    }

    [HttpPost("applications/upload-resume")]
    [Consumes("multipart/form-data")]
    [AllowAnonymous]
    public async Task<ActionResult<Response<string>>> UploadResume(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(Response<string>.Fail("No file provided"));

        // Size cap — 5MB per the careers form
        const long maxBytes = 5 * 1024 * 1024;
        if (file.Length > maxBytes)
            return BadRequest(Response<string>.Fail("Resume must be 5MB or smaller"));

        // Type check — PDF or DOC/DOCX
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".pdf" && ext != ".doc" && ext != ".docx")
            return BadRequest(Response<string>.Fail(
                "Resume must be PDF or DOC/DOCX"));

        var result = await _service.UploadResume(file);
        return HandleResponse(result);
    }
}
