using MuuqWear.API.DTO.ProductDTO;
using MuuqWear.API.Shared; 

namespace MuuqWear.API.Interfaces;
public interface ICategoryService
{
    Task<Response<List<CategoryDTO>>> GetAll();
    Task<Response<CategoryDTO>> Add(AddCategoryDTO request);
}
