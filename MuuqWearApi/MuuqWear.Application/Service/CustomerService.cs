using MuuqWear.API.Shared;
using MuuqWear.Application.Interfaces;
using MuuqWear.Model.DTO.CustomerDTO;
using System.Text.Json;

namespace MuuqWear.Application.Service;

public class CustomerService : ICustomerService
{
    private readonly Supabase.Client _client;

    public CustomerService(SupabaseClientFactory factory)
    {
        _client = factory.CreateClient();
    }

    // =============================================
    // GET ALL CUSTOMERS
    // =============================================
    public async Task<Response<PaginatedResponse<CustomerDTO>>> GetAll(
        string? search, int page, int pageSize)
    {
        try
        {
            var searchTerm = search?.Trim() ?? "";
            var offset = (page - 1) * pageSize;

            //  get total count first
            var countResult = await _client.Rpc(
                "get_customers_count",
                new Dictionary<string, object>
                {
                    { "p_search_term", searchTerm }
                });
            var totalCount = 0;
            //  fetch paginated data
            var dataResult = await _client.Rpc(
                "get_customers",
                new Dictionary<string, object>
                {
                    { "p_search_term", searchTerm },
                    { "p_page_size",   pageSize   },
                    { "p_offset",      offset     }
                });
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            };

            var customers = JsonSerializer
                .Deserialize<List<CustomerDTO>>(
                    dataResult.Content ?? "[]", options)
                ?? new List<CustomerDTO>();

            var totalPages = (int)Math.Ceiling(
                (double)totalCount / pageSize);

            var paginatedResponse = new PaginatedResponse<CustomerDTO>
            {
                Data = customers,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                HasMore = page < totalPages
            };

            return Response<PaginatedResponse<CustomerDTO>>
                .SuccessResponse(paginatedResponse, "Customers fetched");
        }
        catch (Exception ex)
        {
            return Response<PaginatedResponse<CustomerDTO>>
                .Fail("Error: " + ex.Message);
        }
    }
}
