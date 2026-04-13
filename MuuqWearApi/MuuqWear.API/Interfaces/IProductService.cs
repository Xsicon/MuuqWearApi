using MuuqWear.API.DTO.ProductDTO;
using MuuqWear.API.Shared;

namespace MuuqWear.API.Interfaces;
public interface IProductService
{
    Task<Response<List<ProductDTO>>> GetAll();
    Task<Response<ProductDTO>> Add(AddProductDTO request);
    Task<Response<string>> UploadImage(IFormFile file);
    Task<Response<ProductDTO>> Update(Guid id, UpdateProductDTO request);
    Task<Response<bool>> Delete(Guid id);


}
