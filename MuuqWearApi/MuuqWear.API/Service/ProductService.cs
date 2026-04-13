using MuuqWear.API.DTO.ProductDTO;
using MuuqWear.API.Interfaces;
using MuuqWear.API.Models;
using MuuqWear.API.Shared;
using Supabase;

namespace MuuqWear.API.Service;
public class ProductService : IProductService
{
    private readonly Client _client;

    public ProductService(Client client)
    {
        _client = client;
    }

    public async Task<Response<List<ProductDTO>>> GetAll()
    {
        try
        {
            var result = await _client
                .From<Product>()
                .Get();

            var products = result.Models.Select(p => new ProductDTO
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price,
                Badge = p.Badge,
                ImageUrl = p.ImageUrl,
                Stock = p.Stock,
                Category = p.Category,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt,
                IsBestSeller=p.IsBestSeller,
                IsFeatured=p.IsFeatured,
                IsNewArrival=p.IsNewArrival
            }).ToList();

            return Response<List<ProductDTO>>.SuccessResponse(products, "Products fetched successfully");
        }
        catch (Exception ex)
        {
            return Response<List<ProductDTO>>.Fail("Error: " + ex.Message);
        }
    }

    public async Task<Response<ProductDTO>> Add(AddProductDTO request)
    {
        try
        {
            var product = new Product
            {
                Name = request.Name,
                Price = request.Price,
                Badge = request.Badge,
                ImageUrl = request.ImageUrl,
                Stock = request.Stock,
                Category = request.Category,
                IsActive = request.IsActive,
                IsNewArrival = request.IsNewArrival,
                IsFeatured = request.IsFeatured,
                IsBestSeller = request.IsBestSeller
            };

            var result = await _client
                .From<Product>()
                .Insert(product);

            var inserted = result.Models.FirstOrDefault();

            if (inserted == null)
                return Response<ProductDTO>.Fail("Failed to add product");

            var productDTO = new ProductDTO
            {
                Id = inserted.Id,
                Name = inserted.Name,
                Price = inserted.Price,
                Badge = inserted.Badge,
                ImageUrl = inserted.ImageUrl,
                Stock = inserted.Stock,
                Category = inserted.Category,
                IsActive = inserted.IsActive,
                CreatedAt = inserted.CreatedAt,
                IsNewArrival = inserted.IsNewArrival,
                IsFeatured = inserted.IsFeatured,
                IsBestSeller = inserted.IsBestSeller
            };

            return Response<ProductDTO>.SuccessResponse(productDTO, "Product added successfully");
        }
        catch (Exception ex)
        {
            return Response<ProductDTO>.Fail("Error: " + ex.Message);
        }
    }
    public async Task<Response<string>> UploadImage(IFormFile file)
    {
        try
        {
            using var stream = file.OpenReadStream();
            var buffer = new byte[file.Length];
            await stream.ReadAsync(buffer);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

            await _client.Storage
                .From("products")
                .Upload(buffer, fileName, new Supabase.Storage.FileOptions
                {
                    ContentType = file.ContentType,
                    Upsert = true
                });

            var publicUrl = _client.Storage
                .From("products")
                .GetPublicUrl(fileName);

            return Response<string>.SuccessResponse(publicUrl, "Image uploaded successfully");
        }
        catch (Exception ex)
        {
            return Response<string>.Fail("Error: " + ex.Message);
        }
    }

    public async Task<Response<ProductDTO>> Update(Guid id, UpdateProductDTO request)
    {
        try
        {
            var result = await _client
                .From<Product>()
                .Where(p => p.Id == id)
                .Set(p => p.Name!, request.Name)
                .Set(p => p.Price, request.Price)
                .Set(p => p.Badge!, request.Badge)
                .Set(p => p.ImageUrl!, request.ImageUrl)
                .Set(p => p.Stock, request.Stock)
                .Set(p => p.Category!, request.Category)
                .Set(p => p.IsActive, request.IsActive)
                .Set(p => p.IsNewArrival, request.IsNewArrival)
                .Set(p => p.IsFeatured, request.IsFeatured)
                .Set(p => p.IsBestSeller, request.IsBestSeller)
                .Update();

            var updated = result.Models.FirstOrDefault();

            if (updated == null)
                return Response<ProductDTO>.Fail("Failed to update product");

            var productDTO = new ProductDTO
            {
                Id = updated.Id,
                Name = updated.Name,
                Price = updated.Price,
                Badge = updated.Badge,
                ImageUrl = updated.ImageUrl,
                Stock = updated.Stock,
                Category = updated.Category,
                IsActive = updated.IsActive,
                CreatedAt = updated.CreatedAt,
                IsNewArrival = updated.IsNewArrival,
                IsFeatured = updated.IsFeatured,
                IsBestSeller = updated.IsBestSeller
            };

            return Response<ProductDTO>.SuccessResponse(productDTO, "Product updated successfully");
        }
        catch (Exception ex)
        {
            return Response<ProductDTO>.Fail("Error: " + ex.Message);
        }
    }

    public async Task<Response<bool>> Delete(Guid id)
    {
        try
        {
            await _client
                .From<Product>()
                .Where(p => p.Id == id)
                .Delete();

            return Response<bool>.SuccessResponse(true, "Product deleted successfully");
        }
        catch (Exception ex)
        {
            return Response<bool>.Fail("Error: " + ex.Message);
        }
    }
}
