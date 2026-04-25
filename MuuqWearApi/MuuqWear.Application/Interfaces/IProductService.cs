using Microsoft.AspNetCore.Http;
using MuuqWear.API.DTO.ProductDTO;
using MuuqWear.API.Shared;

namespace MuuqWear.API.Interfaces;
public interface IProductService
{
    // add search parameter
    Task<Response<PaginatedResponse<ProductDTO>>> GetAll(ProductFilterDTO filter);
    Task<Response<HomeProductsDTO>> GetHomeProducts();
    Task<Response<ProductDTO>> GetById(Guid id);
    Task<Response<List<ProductDTO>>> GetRelated(Guid productId, Guid? categoryId);
    Task<Response<ProductDTO>> Add(AddProductDTO request);
    Task<Response<string>> UploadImage(IFormFile file);
    Task<Response<ProductDTO>> Update(Guid id, UpdateProductDTO request);
    Task<Response<bool>> Delete(Guid id);
    Task<Response<ProductImageDTO>> AddProductImage(AddProductImageDTO request);
    Task<Response<bool>> DeleteProductImage(Guid imageId);


}
