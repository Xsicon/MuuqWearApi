using MuuqWear.API.DTO.ProductDTO;
using MuuqWear.API.Interfaces;
using MuuqWear.API.Shared;
using MuuqWear.Model.Models.Category;
using Supabase;

namespace MuuqWear.API.Service;
public class CategoryService : ICategoryService
{
    private readonly Supabase.Client _client;

    public CategoryService(SupabaseClientFactory factory)
    {
        _client = factory.CreateClient();
    }

    public async Task<Response<List<CategoryDTO>>> GetAll()
    {
        try
        {
            var result = await _client
                .From<Category>()
                .Get();

            var categories = result.Models.Select(c => new CategoryDTO
            {
                Id = c.Id,
                Name = c.Name
            }).ToList();

            return Response<List<CategoryDTO>>.SuccessResponse(categories, "Categories fetched");
        }
        catch (Exception ex)
        {
            return Response<List<CategoryDTO>>.Fail("Error: " + ex.Message);
        }
    }

    public async Task<Response<CategoryDTO>> Add(AddCategoryDTO request)
    {
        try
        {
            var category = new Category
            {
                Name = request.Name
            };

            var result = await _client
                .From<Category>()
                .Insert(category);

            var inserted = result.Models.FirstOrDefault();

            if (inserted == null)
                return Response<CategoryDTO>.Fail("Failed to add category");

            return Response<CategoryDTO>.SuccessResponse(new CategoryDTO
            {
                Id = inserted.Id,
                Name = inserted.Name
            }, "Category added");
        }
        catch (Exception ex)
        {
            return Response<CategoryDTO>.Fail("Error: " + ex.Message);
        }
    }
}
