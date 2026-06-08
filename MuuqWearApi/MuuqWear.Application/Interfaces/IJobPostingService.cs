using Microsoft.AspNetCore.Http;
using MuuqWear.API.Shared;
using MuuqWear.Model.DTO.JobApplicationDTO;
using MuuqWear.Model.DTO.JobPostingDTO;

namespace MuuqWear.Application.Interfaces;

public interface IJobPostingService
{
    Task<Response<List<JobPostingDTO>>> GetAll();
    Task<Response<List<JobPostingDTO>>> GetOpen();
    Task<Response<JobPostingDTO>> GetById(Guid id);
    Task<Response<JobPostingDTO>> Create(CreateJobPostingDTO request);
    Task<Response<JobPostingDTO>> Update(Guid id, UpdateJobPostingDTO request);
    Task<Response<bool>> Delete(Guid id);
    Task<Response<JobPostingDTO>> Close(Guid id);
    Task<Response<JobPostingDTO>> Reopen(Guid id);

    //Application methods

    Task<Response<JobApplicationDTO>> SubmitApplication(SubmitJobApplicationDTO request);
    Task<Response<List<JobApplicationDTO>>> GetApplicationsByJob(Guid jobId);
    Task<Response<JobApplicationDTO>> GetApplicationById(Guid applicationId);
    Task<Response<JobApplicationDTO>> UpdateApplicationStatus(
        Guid applicationId, UpdateJobApplicationStatusDTO request);
    Task<Response<bool>> DeleteApplication(Guid applicationId);
    Task<Response<string>> UploadResume(IFormFile file);

}