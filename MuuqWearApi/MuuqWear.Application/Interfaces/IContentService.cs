using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MuuqWear.API.Shared;
using MuuqWear.Model.DTO.ContentItemDTO;

namespace MuuqWear.Application.Interfaces;

public interface IContentService
{
    // get all items for a content type
    Task<Response<List<ContentItemDTO>>> GetAll(ContentCategory type);

    // get single item
    Task<Response<ContentItemDTO>> GetById(ContentCategory type, Guid id);

    // create new item
    Task<Response<ContentItemDTO>> Create(ContentCategory type, CreateContentItemDTO request);

    // update existing item
    Task<Response<ContentItemDTO>> Update(ContentCategory type, Guid id, UpdateContentItemDTO request);

    // delete item
    Task<Response<bool>> Delete(ContentCategory type, Guid id);

    // publish item
    Task<Response<ContentItemDTO>> Publish(ContentCategory type, Guid id);

    // unpublish item
    Task<Response<ContentItemDTO>> Unpublish(ContentCategory type, Guid id);
    Task<Response<string>> UploadImage(IFormFile file);
}
