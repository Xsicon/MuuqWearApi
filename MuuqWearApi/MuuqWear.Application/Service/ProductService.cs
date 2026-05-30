using Microsoft.AspNetCore.Http;
using MuuqWear.API.DTO.ProductDTO;
using MuuqWear.API.Interfaces;
using MuuqWear.API.Shared;
using MuuqWear.Application.Shared;
using MuuqWear.Model.Models.Product;
using Supabase;
using Supabase.Postgrest;
using static Supabase.Postgrest.Constants;

namespace MuuqWear.API.Service;
public class ProductService : IProductService
{
    private readonly Supabase.Client _client;
    private readonly Supabase.Client _adminClient;

    public ProductService(SupabaseClientFactory factory, SupabaseAdminClientFactory adminClient)
    {
        _client = factory.CreateClient();
        _adminClient = factory.CreateClient();

    }

    public async Task<Response<PaginatedResponse<ProductDTO>>> GetAll(ProductFilterDTO filter)
    {
        try
        {
            var baseParams = BuildFilterParameters(filter);

            var countResult = await _client.Rpc("get_products_count", baseParams);
            var totalCount = 0;
            if (!int.TryParse(countResult.Content?.Trim('"'), out totalCount))
                totalCount = 0;

            var offset = (filter.Page - 1) * filter.PageSize;
            var dataParams = new Dictionary<string, object>(baseParams)
        {
            { "p_sort_by",   filter.SortBy ?? "newest" },
            { "p_page_size", filter.PageSize },
            { "p_offset",    offset }
        };

            var dataResult = await _client.Rpc("get_products", dataParams);

            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower
            };

            var products = System.Text.Json.JsonSerializer
                .Deserialize<List<ProductDTO>>(
                    dataResult.Content ?? "[]", options)
                ?? new List<ProductDTO>();

            //  fetch size stock for all products in one query
            var productIds = products.Select(p => p.Id).ToList();
            var sizeStockMap = await FetchSizeStock(productIds);

            //  attach size stock to each product
            foreach (var product in products)
            {
                if (sizeStockMap.TryGetValue(product.Id, out var sizeStock))
                    product.SizeStock = sizeStock;
            }

            var totalPages = (int)Math.Ceiling((double)totalCount / filter.PageSize);

            var paginatedResponse = new PaginatedResponse<ProductDTO>
            {
                Data = products,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize,
                TotalPages = totalPages,
                HasMore = filter.Page < totalPages
            };

            return Response<PaginatedResponse<ProductDTO>>
                .SuccessResponse(paginatedResponse, "Products fetched successfully");
        }
        catch (Exception ex)
        {
            return Response<PaginatedResponse<ProductDTO>>
                .Fail("Error: " + ex.Message);
        }
    }

    private Dictionary<string, object> BuildFilterParameters(ProductFilterDTO filter)
    {
        return new Dictionary<string, object>
    {
        { "p_search_term", filter.Search ?? "" },
        { "p_category_id", filter.CategoryId.HasValue
            ? (object)filter.CategoryId.Value.ToString()
            : null! },
        { "p_size_filter", filter.Sizes ?? "" },
        { "p_min_price", filter.MinPrice ?? 0 },
        { "p_max_price", filter.MaxPrice ?? 999999 }
    };
    }

    public async Task<Response<HomeProductsDTO>> GetHomeProducts()
    {
        try
        {
            // fetch 6 new arrivals
            var newArrivalsResult = await _client
                .From<Product>()
                .Filter("is_new_arrival", Supabase.Postgrest.Constants.Operator.Equals, "true")
                .Filter("is_active", Supabase.Postgrest.Constants.Operator.Equals, "true")
                .Filter("is_deleted", Supabase.Postgrest.Constants.Operator.Equals, "false")
                .Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending)
                .Range(0, 5)
                .Get();

            // fetch 6 featured products
            var featuredResult = await _client
                .From<Product>()
                .Filter("is_featured", Supabase.Postgrest.Constants.Operator.Equals, "true")
                .Filter("is_active", Supabase.Postgrest.Constants.Operator.Equals, "true")
                .Filter("is_deleted", Supabase.Postgrest.Constants.Operator.Equals, "false")
                .Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending)
                .Range(0, 5)
                .Get();

            var bestSellersResult = await _client
                .From<Product>()
                .Filter("is_best_seller", Supabase.Postgrest.Constants.Operator.Equals, "true")
                .Filter("is_active", Supabase.Postgrest.Constants.Operator.Equals, "true")
                .Filter("is_deleted", Supabase.Postgrest.Constants.Operator.Equals, "false")
                .Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending)
                .Range(0, 5)
                .Get();

            //  collect all product IDs
            var allProductIds = newArrivalsResult.Models
                .Concat(featuredResult.Models)
                .Concat(bestSellersResult.Models)
                .Select(p => p.Id)
                .Distinct()
                .ToList();

            //  fetch size stock for all products in one query
            var sizeStockMap = await FetchSizeStock(allProductIds);

            // map to DTOs
            var homeProducts = new HomeProductsDTO
            {
                NewArrivals = newArrivalsResult.Models.Select(p =>
                {
                    var sizeStock = sizeStockMap.TryGetValue(p.Id, out var stock)
                        ? stock
                        : new List<SizeStockDTO>();

                    return new ProductDTO
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Price = p.Price,
                        Badge = p.Badge,
                        ImageUrl = p.ImageUrl,
                        Stock = sizeStock.Sum(s => s.Quantity), //  calculated
                        IsActive = p.IsActive,
                        IsNewArrival = p.IsNewArrival,
                        IsFeatured = p.IsFeatured,
                        IsBestSeller = p.IsBestSeller,
                        CategoryId = p.CategoryId,
                        Description = p.Description
                    };
                }).ToList(),

                Featured = featuredResult.Models.Select(p =>
                {
                    var sizeStock = sizeStockMap.TryGetValue(p.Id, out var stock)
                        ? stock
                        : new List<SizeStockDTO>();

                    return new ProductDTO
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Price = p.Price,
                        Badge = p.Badge,
                        ImageUrl = p.ImageUrl,
                        Stock = sizeStock.Sum(s => s.Quantity), //  calculated
                        IsActive = p.IsActive,
                        IsNewArrival = p.IsNewArrival,
                        IsFeatured = p.IsFeatured,
                        IsBestSeller = p.IsBestSeller,
                        CategoryId = p.CategoryId,
                        Description = p.Description
                    };
                }).ToList(),

                BestSellers = bestSellersResult.Models.Select(p =>
                {
                    var sizeStock = sizeStockMap.TryGetValue(p.Id, out var stock)
                        ? stock
                        : new List<SizeStockDTO>();

                    return new ProductDTO
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Price = p.Price,
                        Badge = p.Badge,
                        ImageUrl = p.ImageUrl,
                        Stock = sizeStock.Sum(s => s.Quantity), //  calculated
                        IsActive = p.IsActive,
                        IsNewArrival = p.IsNewArrival,
                        IsFeatured = p.IsFeatured,
                        IsBestSeller = p.IsBestSeller,
                        CategoryId = p.CategoryId,
                        Description = p.Description
                    };
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
                // ❌ Stock = request.Stock, // removed - no longer in DB
                Category = request.Category,
                IsActive = request.IsActive,
                IsNewArrival = request.IsNewArrival,
                IsFeatured = request.IsFeatured,
                IsBestSeller = request.IsBestSeller,
                Description = request.Description,
                Gender = request.Gender,
                CategoryId = request.CategoryId,
                ColorOptions = request.ColorOptions ?? new List<string>(),
                CreatedAt = DateTime.UtcNow
            };

            var result = await _client
                .From<Product>()
                .Insert(product);

            var inserted = result.Models.FirstOrDefault();
            if (inserted == null)
                return Response<ProductDTO>.Fail("Failed to add product");

            //  auto-generate SKU from product id
            var sku = $"MQ-{inserted.Id.ToString().Substring(0, 6).ToUpper()}";
            await _client
                .From<Product>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals,
                    inserted.Id.ToString())
                .Set(p => p.Sku!, sku)
                .Update();

            inserted.Sku = sku;

            //  insert size stock rows if sizes provided
            var sizeStock = new List<SizeStockDTO>();
            if (request.Sizes.Any())
            {
                var totalSizes = request.Sizes.Count;
                //  still use request.Stock to distribute - it's just input data
                var baseQty = request.Stock / totalSizes;
                var remainder = request.Stock % totalSizes;

                var sizeStockRows = request.Sizes.Select((size, index) =>
                    new ProductSizeStock
                    {
                        Id = Guid.NewGuid(),
                        ProductId = inserted.Id,
                        Size = size,
                        Quantity = index == 0
                            ? baseQty + remainder
                            : baseQty
                    }).ToList();

                var sizeResult = await _client
                    .From<ProductSizeStock>()
                    .Insert(sizeStockRows);

                sizeStock = sizeResult.Models.Select(x => new SizeStockDTO
                {
                    Id = x.Id,
                    Size = x.Size,
                    Quantity = x.Quantity
                }).ToList();
            }

            var productDTO = new ProductDTO
            {
                Id = inserted.Id,
                Name = inserted.Name,
                Price = inserted.Price,
                Badge = inserted.Badge,
                ImageUrl = inserted.ImageUrl,
                Stock = sizeStock.Sum(s => s.Quantity), //  calculated from sizes
                Category = inserted.Category,
                IsActive = inserted.IsActive,
                CreatedAt = inserted.CreatedAt,
                IsNewArrival = inserted.IsNewArrival,
                IsFeatured = inserted.IsFeatured,
                IsBestSeller = inserted.IsBestSeller,
                Description = inserted.Description,
                Gender = inserted.Gender,
                CategoryId = inserted.CategoryId,
                Sku = inserted.Sku,
                ColorOptions = request.ColorOptions,
                SizeStock = sizeStock
            };

            return Response<ProductDTO>.SuccessResponse(
                productDTO, "Product added successfully");
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

            var fileName =
                $"products/{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            await _adminClient.Storage
                .From("app-images")
                .Upload(buffer, fileName, new Supabase.Storage.FileOptions
                {
                    ContentType = file.ContentType,
                    Upsert = true
                });

            var publicUrl = _adminClient.Storage
                .From("app-images")
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
                .Set(p => p.Category!, request.Category)
                .Set(p => p.IsActive, request.IsActive)
                .Set(p => p.IsNewArrival, request.IsNewArrival)
                .Set(p => p.IsFeatured, request.IsFeatured)
                .Set(p => p.IsBestSeller, request.IsBestSeller)
                .Set(p => p.Description!, request.Description)
                .Set(p => p.Gender!, request.Gender)
                .Set(p => p.CategoryId!, request.CategoryId)
                .Set(p => p.ColorOptions!, request.ColorOptions)
                .Update();

            var updated = result.Models.FirstOrDefault();
            if (updated == null)
                return Response<ProductDTO>.Fail("Failed to update product");

            //  fetch size stock to calculate total
            var sizeStockMap = await FetchSizeStock(new List<Guid> { updated.Id });
            var sizeStock = sizeStockMap.TryGetValue(updated.Id, out var stock)
                ? stock
                : new List<SizeStockDTO>();

            var productDTO = new ProductDTO
            {
                Id = updated.Id,
                Name = updated.Name,
                Price = updated.Price,
                Badge = updated.Badge,
                ImageUrl = updated.ImageUrl,
                Stock = sizeStock.Sum(s => s.Quantity), //  calculated
                Category = updated.Category,
                IsActive = updated.IsActive,
                CreatedAt = updated.CreatedAt,
                IsNewArrival = updated.IsNewArrival,
                IsFeatured = updated.IsFeatured,
                IsBestSeller = updated.IsBestSeller,
                Description = updated.Description,
                Gender = updated.Gender,
                CategoryId = updated.CategoryId,
                ColorOptions = updated.ColorOptions ?? new List<string>()

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
             .Filter("id", Supabase.Postgrest.Constants.Operator.Equals,
                 id.ToString())
             .Set(p => p.IsDeleted, true)
             .Set(p => p.DeletedAt!, DateTime.UtcNow)
             .Update();


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
     .Filter("is_deleted", Supabase.Postgrest.Constants.Operator.Equals, "false") // ← add
     .Single();

            if (productResult == null)
                return Response<ProductDTO>.Fail("Product not found");

            var imagesResult = await _client
                .From<ProductImage>()
                .Filter("product_id",
                    Supabase.Postgrest.Constants.Operator.Equals, id.ToString())
                .Order("sort_order", Supabase.Postgrest.Constants.Ordering.Ascending)
                .Get();

            var images = imagesResult.Models.Select(img => new ProductImageDTO
            {
                Id = img.Id,
                ProductId = img.ProductId,
                ImageUrl = img.ImageUrl,
                SortOrder = img.SortOrder
            }).ToList();

            //  fetch size stock for this product
            var sizeStockMap = await FetchSizeStock(new List<Guid> { id });
            var sizeStock = sizeStockMap.TryGetValue(id, out var stock)
                ? stock
                : new List<SizeStockDTO>();

            var totalStock = sizeStock.Sum(s => s.Quantity);

            var productDTO = new ProductDTO
            {
                Id = productResult.Id,
                Name = productResult.Name,
                Price = productResult.Price,
                Badge = productResult.Badge,
                ImageUrl = productResult.ImageUrl,
                Stock = totalStock,
                IsActive = productResult.IsActive,
                CreatedAt = productResult.CreatedAt,
                IsNewArrival = productResult.IsNewArrival,
                IsFeatured = productResult.IsFeatured,
                IsBestSeller = productResult.IsBestSeller,
                CategoryId = productResult.CategoryId,
                Gender = productResult.Gender,
                Description = productResult.Description,
                Sku = productResult.Sku,        // ← add
                SizeStock = sizeStock,                // ← add
                Images = images,
                ColorOptions = productResult.ColorOptions ?? new List<string>()
            };

            return Response<ProductDTO>.SuccessResponse(
                productDTO, "Product fetched successfully");
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
                    .Filter("is_deleted", Supabase.Postgrest.Constants.Operator.Equals, "false")
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
                        .Filter("is_deleted", Supabase.Postgrest.Constants.Operator.Equals,
                            "false")
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
                    .Filter("is_deleted", Supabase.Postgrest.Constants.Operator.Equals, "false")
                    .Order("created_at",
                        Supabase.Postgrest.Constants.Ordering.Descending)
                    .Limit(4)
                    .Get();

                relatedProducts = result.Models;
            }

            //  fetch size stock for all related products
            var productIds = relatedProducts.Select(p => p.Id).ToList();
            var sizeStockMap = await FetchSizeStock(productIds);

            // Step 3 — map to DTOs
            var products = relatedProducts.Select(p =>
            {
                var sizeStock = sizeStockMap.TryGetValue(p.Id, out var stock)
                    ? stock
                    : new List<SizeStockDTO>();

                return new ProductDTO
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    Badge = p.Badge,
                    ImageUrl = p.ImageUrl,
                    Stock = sizeStock.Sum(s => s.Quantity), //  calculated
                    IsActive = p.IsActive,
                    CategoryId = p.CategoryId,
                    Description = p.Description
                };
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

    private async Task<Dictionary<Guid, List<SizeStockDTO>>> FetchSizeStock(
    List<Guid> productIds)
    {
        if (!productIds.Any())
            return new Dictionary<Guid, List<SizeStockDTO>>();

        var result = await _client
            .From<ProductSizeStock>()
            .Filter("product_id",
                Supabase.Postgrest.Constants.Operator.In,
                productIds.Select(id => id.ToString()).ToList())
            .Order("size", Supabase.Postgrest.Constants.Ordering.Ascending)
            .Get();

        // group by product_id → Dictionary<productId, List<SizeStockDTO>>
        return result.Models
            .GroupBy(x => x.ProductId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => new SizeStockDTO
                {
                    Id = x.Id,
                    Size = x.Size,
                    Quantity = x.Quantity
                }).ToList()
            );
    }

    public async Task<Response<List<SizeStockDTO>>> GetSizeStock(Guid productId)
    {
        try
        {
            var result = await _client
                .From<ProductSizeStock>()
                .Filter("product_id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    productId.ToString())
                .Order("size", Supabase.Postgrest.Constants.Ordering.Ascending)
                .Get();

            var sizeStock = result.Models.Select(x => new SizeStockDTO
            {
                Id = x.Id,
                Size = x.Size,
                Quantity = x.Quantity
            }).ToList();

            return Response<List<SizeStockDTO>>
                .SuccessResponse(sizeStock, "Size stock fetched");
        }
        catch (Exception ex)
        {
            return Response<List<SizeStockDTO>>.Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // UPDATE SIZE STOCK
    // =============================================
    public async Task<Response<SizeStockDTO>> UpdateSizeStock(
        Guid sizeStockId, int quantity)
    {
        try
        {
            if (quantity < 0)
                return Response<SizeStockDTO>.Fail("Quantity cannot be negative");

            var result = await _client
                .From<ProductSizeStock>()
                .Filter("id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    sizeStockId.ToString())
                .Set(x => x.Quantity, quantity)
                .Update();

            var updated = result.Models.FirstOrDefault();
            if (updated == null)
                return Response<SizeStockDTO>.Fail("Size stock not found");

            return Response<SizeStockDTO>.SuccessResponse(new SizeStockDTO
            {
                Id = updated.Id,
                Size = updated.Size,
                Quantity = updated.Quantity
            }, "Size stock updated");
        }
        catch (Exception ex)
        {
            return Response<SizeStockDTO>.Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // ADD SIZE STOCK
    // =============================================
    public async Task<Response<SizeStockDTO>> AddSizeStock(
        Guid productId, string size, int quantity)
    {
        try
        {
            var result = await _client
                .From<ProductSizeStock>()
                .Insert(new ProductSizeStock
                {
                    Id = Guid.NewGuid(),
                    ProductId = productId,
                    Size = size,
                    Quantity = quantity
                });

            var inserted = result.Models.FirstOrDefault();
            if (inserted == null)
                return Response<SizeStockDTO>.Fail("Failed to add size");

            return Response<SizeStockDTO>.SuccessResponse(new SizeStockDTO
            {
                Id = inserted.Id,
                Size = inserted.Size,
                Quantity = inserted.Quantity
            }, "Size added");
        }
        catch (Exception ex)
        {
            return Response<SizeStockDTO>.Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // DELETE SIZE STOCK
    // =============================================
    public async Task<Response<bool>> DeleteSizeStock(Guid sizeStockId)
    {
        try
        {
            await _client
                .From<ProductSizeStock>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals,
                    sizeStockId.ToString())
                .Delete();

            return Response<bool>.SuccessResponse(true, "Size deleted");
        }
        catch (Exception ex)
        {
            return Response<bool>.Fail("Error: " + ex.Message);
        }
    }
}
