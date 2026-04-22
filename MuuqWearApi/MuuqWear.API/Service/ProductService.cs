using MuuqWear.API.DTO.ProductDTO;
using MuuqWear.API.Interfaces;
using MuuqWear.API.Models;
using MuuqWear.API.Shared;
using Supabase;
using Supabase.Postgrest;
using static Supabase.Postgrest.Constants;

namespace MuuqWear.API.Service;
public class ProductService : IProductService
{
    private readonly Supabase.Client _client;

    public ProductService(Supabase.Client client)
    {
        _client = client;
    }

    public async Task<Response<PaginatedResponse<ProductDTO>>> GetAll(int page = 1, int pageSize = 10, string? search = null)
    {
        try
        {
            // Step 1 — get count via RPC
            // returns just a number, no data fetched ✅
            var parameters = new Dictionary<string, object>
        {
            { "search_term", search ?? "" }
        };
            var countResult = await _client.Rpc("get_products_count", parameters);
            var totalCount = int.Parse(countResult.Content!.Trim('"'));

            // Step 2 — calculate page range
            var startIndex = (page - 1) * pageSize;
            var endIndex = startIndex + pageSize - 1;

            // Step 3 — fetch only current page
            if (!string.IsNullOrEmpty(search))
            {
                var result = await _client
                    .From<Product>()
                    .Filter("name", Supabase.Postgrest.Constants.Operator.ILike, $"%{search}%")
                    .Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending)
                    .Range(startIndex, endIndex)
                    .Get();

                var products = MapToDTO(result.Models);

                return Response<PaginatedResponse<ProductDTO>>.SuccessResponse(
                    new PaginatedResponse<ProductDTO>
                    {
                        Data = products,
                        TotalCount = totalCount,
                        Page = page,
                        PageSize = pageSize
                    }, "Products fetched successfully");
            }
            else
            {
                var result = await _client
                    .From<Product>()
                    .Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending)
                    .Range(startIndex, endIndex)
                    .Get();

                var products = MapToDTO(result.Models);

                return Response<PaginatedResponse<ProductDTO>>.SuccessResponse(
                    new PaginatedResponse<ProductDTO>
                    {
                        Data = products,
                        TotalCount = totalCount,
                        Page = page,
                        PageSize = pageSize
                    }, "Products fetched successfully");
            }
        }
        catch (Exception ex)
        {
            return Response<PaginatedResponse<ProductDTO>>.Fail("Error: " + ex.Message);
        }
    }

    // extract mapping to private method to avoid repetition
    private List<ProductDTO> MapToDTO(List<Product> models)
    {
        return models.Select(p => new ProductDTO
        {
            Id = p.Id,
            Name = p.Name,
            Price = p.Price,
            Badge = p.Badge,
            ImageUrl = p.ImageUrl,
            Stock = p.Stock,
            IsActive = p.IsActive,
            CreatedAt = p.CreatedAt,
            IsBestSeller = p.IsBestSeller,
            IsFeatured = p.IsFeatured,
            IsNewArrival = p.IsNewArrival,
            CategoryId = p.CategoryId,
            Sizes = p.Sizes,
            Gender = p.Gender,
            Description = p.Description
        }).ToList();
    }
    public async Task<Response<HomeProductsDTO>> GetHomeProducts()
    {
        try
        {
            // fetch 6 new arrivals
            // only active products
            // ordered by newest first
            var newArrivalsResult = await _client
                .From<Product>()
                .Filter("is_new_arrival", Supabase.Postgrest.Constants.Operator.Equals, "true")
                .Filter("is_active", Supabase.Postgrest.Constants.Operator.Equals, "true")
                .Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending)
                .Range(0, 5) // 6 products (0 to 5)
                .Get();

            // fetch 6 featured products
            var featuredResult = await _client
                .From<Product>()
                .Filter("is_featured", Supabase.Postgrest.Constants.Operator.Equals, "true")
                .Filter("is_active", Supabase.Postgrest.Constants.Operator.Equals, "true")
                .Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending)
                .Range(0, 5)
                .Get();

            // fetch 6 best sellers
            var bestSellersResult = await _client
                .From<Product>()
                .Filter("is_best_seller", Supabase.Postgrest.Constants.Operator.Equals, "true")
                .Filter("is_active", Supabase.Postgrest.Constants.Operator.Equals, "true")
                .Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending)
                .Range(0, 5)
                .Get();

            // map to DTOs
            var homeProducts = new HomeProductsDTO
            {
                NewArrivals = newArrivalsResult.Models.Select(p => new ProductDTO
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    Badge = p.Badge,
                    ImageUrl = p.ImageUrl,
                    Stock = p.Stock,
                    IsActive = p.IsActive,
                    IsNewArrival = p.IsNewArrival,
                    IsFeatured = p.IsFeatured,
                    IsBestSeller = p.IsBestSeller,
                    CategoryId = p.CategoryId,
                    Description = p.Description
                }).ToList(),

                Featured = featuredResult.Models.Select(p => new ProductDTO
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    Badge = p.Badge,
                    ImageUrl = p.ImageUrl,
                    Stock = p.Stock,
                    IsActive = p.IsActive,
                    IsNewArrival = p.IsNewArrival,
                    IsFeatured = p.IsFeatured,
                    IsBestSeller = p.IsBestSeller,
                    CategoryId = p.CategoryId,
                    Description = p.Description
                }).ToList(),

                BestSellers = bestSellersResult.Models.Select(p => new ProductDTO
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    Badge = p.Badge,
                    ImageUrl = p.ImageUrl,
                    Stock = p.Stock,
                    IsActive = p.IsActive,
                    IsNewArrival = p.IsNewArrival,
                    IsFeatured = p.IsFeatured,
                    IsBestSeller = p.IsBestSeller,
                    CategoryId = p.CategoryId,
                    Description = p.Description
                }).ToList()
            };

            return Response<HomeProductsDTO>.SuccessResponse(homeProducts, "Home products fetched");
        }
        catch (Exception ex)
        {
            return Response<HomeProductsDTO>.Fail("Error: " + ex.Message);
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
                IsBestSeller = request.IsBestSeller,
                Description = request.Description,
                Sizes = request.Sizes,
                Gender = request.Gender,
                CategoryId = request.CategoryId,
                CreatedAt = DateTime.UtcNow
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
                IsBestSeller = inserted.IsBestSeller,
                Description = inserted.Description,
                Sizes = inserted.Sizes,
                Gender = inserted.Gender,
                CategoryId = inserted.CategoryId
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
                .From("product")
                .Upload(buffer, fileName, new Supabase.Storage.FileOptions
                {
                    ContentType = file.ContentType,
                    Upsert = true
                });

            var publicUrl = _client.Storage
                .From("product")
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
                .Set(p => p.Description!, request.Description)
                .Set(p => p.Sizes!, request.Sizes)
                .Set(p => p.Gender!, request.Gender)
                .Set(p => p.CategoryId!, request.CategoryId)
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
                IsBestSeller = updated.IsBestSeller,
                Description = updated.Description,
                Sizes = updated.Sizes,
                Gender = updated.Gender,
                CategoryId = updated.CategoryId
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

    public async Task<Response<ProductDTO>> GetById(Guid id)
    {
        try
        {
            var productResult = await _client
    .From<Product>()
    .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, id.ToString())
    .Single();

            // if product not found → return fail
            // Single() returns null if no match
            if (productResult == null)
                return Response<ProductDTO>.Fail("Product not found");

            var imagesResult = await _client
           .From<ProductImage>()
           .Filter("product_id", Supabase.Postgrest.Constants.Operator.Equals, id.ToString())
           .Order("sort_order", Supabase.Postgrest.Constants.Ordering.Ascending)
           .Get();

            // Step 3 — map images to DTOs
            var images = imagesResult.Models.Select(img => new ProductImageDTO
            {
                Id = img.Id,
                ProductId = img.ProductId,
                ImageUrl = img.ImageUrl,
                SortOrder = img.SortOrder
            }).ToList();


            var productDTO = new ProductDTO
            {
                Id = productResult.Id,
                Name = productResult.Name,
                Price = productResult.Price,
                Badge = productResult.Badge,
                ImageUrl = productResult.ImageUrl,
                Stock = productResult.Stock,
                IsActive = productResult.IsActive,
                CreatedAt = productResult.CreatedAt,
                IsNewArrival = productResult.IsNewArrival,
                IsFeatured = productResult.IsFeatured,
                IsBestSeller = productResult.IsBestSeller,
                CategoryId = productResult.CategoryId,
                Sizes = productResult.Sizes,
                Gender = productResult.Gender,
                Description = productResult.Description,
                Images = images
            };

            return Response<ProductDTO>.SuccessResponse(productDTO, "Product fetched successfully");
        }
        catch (Exception ex)
        {
            return Response<ProductDTO>.Fail("Error: " + ex.Message);
        }
    }

    public async Task<Response<List<ProductDTO>>> GetRelated(Guid productId, Guid? categoryId)
    {
        try
        {
            List<Product> relatedProducts;

            if (categoryId.HasValue)
            {
                var result = await _client
     .From<Product>()
     .Filter("category_id",
         Supabase.Postgrest.Constants.Operator.Equals,
         categoryId.Value.ToString())
     .Filter("id",
         Supabase.Postgrest.Constants.Operator.NotEqual,
         productId.ToString())
     .Filter("is_active",
         Supabase.Postgrest.Constants.Operator.Equals,
         "true")
     .Order("created_at",
         Supabase.Postgrest.Constants.Ordering.Descending)
     .Limit(4)
     .Get();

                relatedProducts = result.Models;

                if (relatedProducts.Count < 4)
                {
                    // how many more do we need
                    var remaining = 4 - relatedProducts.Count;

                    // ids to exclude — current product + already fetched
                    var excludeIds = relatedProducts
                        .Select(p => p.Id.ToString())
                        .ToList();
                    excludeIds.Add(productId.ToString());

                    // fetch remaining from other categories
                    var fillResult = await _client
                        .From<Product>()
                        .Filter("id",
                            Supabase.Postgrest.Constants.Operator.NotEqual,
                            productId.ToString())
                        .Filter("is_active",
                            Supabase.Postgrest.Constants.Operator.Equals,
                            "true")
                        .Order("created_at",
                            Supabase.Postgrest.Constants.Ordering.Descending)
                        .Limit(remaining)
                        .Get();

                    relatedProducts.AddRange(fillResult.Models);
                }
            }
            else
            {
                var result = await _client
    .From<Product>()
    .Filter("id",
        Supabase.Postgrest.Constants.Operator.NotEqual,
        productId.ToString())
    .Filter("is_active",
        Supabase.Postgrest.Constants.Operator.Equals,
        "true")
    .Order("created_at",
        Supabase.Postgrest.Constants.Ordering.Descending)
    .Limit(4)
    .Get();

                relatedProducts = result.Models;
            }

            // Step 3 — map to DTOs
            var products = relatedProducts.Select(p => new ProductDTO
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price,
                Badge = p.Badge,
                ImageUrl = p.ImageUrl,
                Stock = p.Stock,
                IsActive = p.IsActive,
                CategoryId = p.CategoryId,
                Description = p.Description
            }).ToList();

            return Response<List<ProductDTO>>
                .SuccessResponse(products, "Related products fetched");
        }
        catch (Exception ex)
        {
            return Response<List<ProductDTO>>.Fail("Error: " + ex.Message);
        }
    }

    public async Task<Response<ProductImageDTO>> AddProductImage(AddProductImageDTO request)
    {
        try
        {
            var image = new ProductImage
            {
                Id = Guid.NewGuid(),
                ProductId = request.ProductId,
                ImageUrl = request.ImageUrl,
                SortOrder = request.SortOrder
            };

            var result = await _client
                .From<ProductImage>()
                .Insert(image);

            var inserted = result.Models.FirstOrDefault();

            if (inserted == null)
                return Response<ProductImageDTO>.Fail("Failed to add image");

            return Response<ProductImageDTO>.SuccessResponse(new ProductImageDTO
            {
                Id = inserted.Id,
                ProductId = inserted.ProductId,
                ImageUrl = inserted.ImageUrl,
                SortOrder = inserted.SortOrder
            }, "Image added successfully");
        }
        catch (Exception ex)
        {
            return Response<ProductImageDTO>.Fail("Error: " + ex.Message);
        }
    }

    public async Task<Response<bool>> DeleteProductImage(Guid imageId)
    {
        try
        {
            await _client
                .From<ProductImage>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, imageId.ToString())
                .Delete();

            return Response<bool>.SuccessResponse(true, "Image deleted successfully");
        }
        catch (Exception ex)
        {
            return Response<bool>.Fail("Error: " + ex.Message);
        }
    }
}
