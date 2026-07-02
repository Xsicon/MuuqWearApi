using Microsoft.Extensions.Logging;
using MuuqWear.API.Shared;
using MuuqWear.Application.Interfaces;
using MuuqWear.Application.Shared;
using MuuqWear.Model.DTO.RefundDTO;
using MuuqWear.Model.Models.Order;
using Stripe;
using RefundRecord = MuuqWear.Model.Models.Order.Refund;

namespace MuuqWear.Application.Service;

public class RefundService : IRefundService
{
    private readonly Supabase.Client _client;
    private readonly ILogger<RefundService> _logger;

    public RefundService(
        SupabaseAdminClientFactory adminFactory,
        ILogger<RefundService> logger)
    {
        // Admin refunds use service-role client (schema MuuqWear) so RLS on
        // refunds does not block reads when the caller JWT is authenticated.
        _client = adminFactory.CreateClient();
        _logger = logger;
    }

    public async Task<Response<PaginatedResponse<RefundDTO>>> GetAllRefunds(
        string? status, int page, int pageSize)
    {
        try
        {
            var statusFilter = status?.Trim().ToLower() ?? "";
            var offset = (page - 1) * pageSize;

            _logger.LogInformation(
                "GetAllRefunds: schema=MuuqWear table=refunds status={StatusFilter} page={Page} pageSize={PageSize}",
                string.IsNullOrEmpty(statusFilter) ? "(all)" : statusFilter,
                page,
                pageSize);

            var totalCount = await CountRefunds(statusFilter);

            _logger.LogInformation(
                "GetAllRefunds: count query returned totalCount={TotalCount}",
                totalCount);

            var query = _client
                .From<RefundRecord>()
                .Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending);

            if (!string.IsNullOrEmpty(statusFilter))
            {
                query = query.Filter("status",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    statusFilter);
            }

            var result = await query
                .Range(offset, offset + pageSize - 1)
                .Get();

            var refunds = result.Models
                .Select(MapToDto)
                .ToList();

            _logger.LogInformation(
                "GetAllRefunds: fetched {RowCount} rows (offset={Offset})",
                refunds.Count,
                offset);

            var totalPages = totalCount == 0
                ? 0
                : (int)Math.Ceiling((double)totalCount / pageSize);

            var paginated = new PaginatedResponse<RefundDTO>
            {
                Data = refunds,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                HasMore = page < totalPages
            };

            return Response<PaginatedResponse<RefundDTO>>
                .SuccessResponse(paginated, "Refunds fetched");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetAllRefunds failed");
            return Response<PaginatedResponse<RefundDTO>>
                .Fail("Error: " + ex.Message);
        }
    }

    public async Task<Response<RefundDTO>> GetRefundById(Guid refundId)
    {
        try
        {
            var refund = await _client
                .From<RefundRecord>()
                .Filter("id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    refundId.ToString())
                .Single();

            if (refund == null)
                return Response<RefundDTO>.Fail("Refund not found");

            return Response<RefundDTO>.SuccessResponse(
                MapToDto(refund), "Refund fetched");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetRefundById failed for {RefundId}", refundId);
            return Response<RefundDTO>.Fail("Error: " + ex.Message);
        }
    }

    public async Task<Response<RefundDTO>> ProcessRefund(Guid refundId)
    {
        try
        {
            var refund = await _client
                .From<RefundRecord>()
                .Filter("id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    refundId.ToString())
                .Single();

            if (refund == null)
                return Response<RefundDTO>.Fail("Refund not found");

            if (!string.IsNullOrEmpty(refund.StripeRefundId)
                && refund.Status == RefundStatus.Completed)
            {
                return Response<RefundDTO>.SuccessResponse(
                    MapToDto(refund), "Refund already processed");
            }

            var processableStatuses = new[]
            {
                RefundStatus.Pending,
                RefundStatus.Processing
            };

            if (!processableStatuses.Contains(refund.Status))
            {
                return Response<RefundDTO>.Fail(
                    $"Refund cannot be processed while status is '{refund.Status}'");
            }

            var order = await _client
                .From<Order>()
                .Filter("id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    refund.OrderId.ToString())
                .Single();

            if (order == null)
                return Response<RefundDTO>.Fail("Linked order not found");

            if (order.PaymentStatus != PaymentStatus.Paid
                && order.PaymentStatus != PaymentStatus.Refunded)
            {
                return Response<RefundDTO>.Fail(
                    "Order payment is not in a refundable state");
            }

            var paymentIntentId = refund.StripePaymentIntentId
                ?? order.StripePaymentIntentId;

            if (string.IsNullOrWhiteSpace(paymentIntentId))
            {
                return Response<RefundDTO>.Fail(
                    "No Stripe payment intent found for this order");
            }

            refund.Status = RefundStatus.Processing;
            refund.StripePaymentIntentId = paymentIntentId;
            refund.UpdatedAt = DateTime.UtcNow;
            refund.FailureReason = null;

            await _client.From<RefundRecord>().Update(refund);

            try
            {
                var stripeRefund = await new Stripe.RefundService().CreateAsync(
                    new RefundCreateOptions
                    {
                        PaymentIntent = paymentIntentId,
                        Amount = (long)(refund.Amount * 100)
                    });

                refund.Status = RefundStatus.Completed;
                refund.StripeRefundId = stripeRefund.Id;
                refund.ProcessedAt = DateTime.UtcNow;
                refund.UpdatedAt = DateTime.UtcNow;

                await _client.From<RefundRecord>().Update(refund);

                if (order.PaymentStatus == PaymentStatus.Paid)
                {
                    order.PaymentStatus = PaymentStatus.Refunded;
                    await _client.From<Order>().Update(order);
                }

                return Response<RefundDTO>.SuccessResponse(
                    MapToDto(refund), "Refund processed successfully");
            }
            catch (StripeException ex)
            {
                refund.Status = RefundStatus.Failed;
                refund.FailureReason = ex.Message;
                refund.UpdatedAt = DateTime.UtcNow;
                await _client.From<RefundRecord>().Update(refund);

                return Response<RefundDTO>.Fail(
                    $"Stripe refund failed: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ProcessRefund failed for {RefundId}", refundId);
            return Response<RefundDTO>.Fail("Error: " + ex.Message);
        }
    }

    public async Task CreatePendingRefundFromReturn(OrderReturn orderReturn)
    {
        if (orderReturn.OrderId == null)
            return;

        try
        {
            var existing = await _client
                .From<RefundRecord>()
                .Filter("return_id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    orderReturn.Id.ToString())
                .Single();

            if (existing != null)
                return;

            var order = await _client
                .From<Order>()
                .Filter("id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    orderReturn.OrderId.Value.ToString())
                .Single();

            if (order == null)
                return;

            if (order.PaymentStatus != PaymentStatus.Paid)
                return;

            if (string.IsNullOrWhiteSpace(order.StripePaymentIntentId))
                return;

            var refundId = Guid.NewGuid();
            var refund = new RefundRecord
            {
                Id = refundId,
                RefundNumber = GenerateRefundNumber(refundId),
                OrderId = order.Id,
                ReturnId = orderReturn.Id,
                OrderNumber = order.OrderNumber,
                Email = orderReturn.Email,
                CustomerName = orderReturn.FullName,
                Amount = order.Total,
                Currency = "usd",
                Status = RefundStatus.Pending,
                StripePaymentIntentId = order.StripePaymentIntentId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _client.From<RefundRecord>().Insert(refund);

            _logger.LogInformation(
                "CreatePendingRefundFromReturn: created {RefundNumber} for return {ReturnId}",
                refund.RefundNumber,
                orderReturn.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "CreatePendingRefundFromReturn failed for return {ReturnId}",
                orderReturn.Id);
        }
    }

    private async Task<int> CountRefunds(string statusFilter)
    {
        try
        {
            if (string.IsNullOrEmpty(statusFilter))
            {
                return await _client.From<RefundRecord>()
                    .Count(Supabase.Postgrest.Constants.CountType.Exact);
            }

            return await _client.From<RefundRecord>()
                .Filter("status",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    statusFilter)
                .Count(Supabase.Postgrest.Constants.CountType.Exact);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "CountRefunds exact count failed for status={StatusFilter}, using fallback",
                string.IsNullOrEmpty(statusFilter) ? "(all)" : statusFilter);

            var rows = string.IsNullOrEmpty(statusFilter)
                ? await _client.From<RefundRecord>().Get()
                : await _client.From<RefundRecord>()
                    .Filter("status",
                        Supabase.Postgrest.Constants.Operator.Equals,
                        statusFilter)
                    .Get();

            return rows.Models.Count;
        }
    }

    private static string GenerateRefundNumber(Guid refundId)
    {
        return $"RF-{refundId.ToString()[..6].ToUpper()}";
    }

    private static RefundDTO MapToDto(RefundRecord refund)
    {
        return new RefundDTO
        {
            Id = refund.Id,
            RefundNumber = refund.RefundNumber,
            OrderId = refund.OrderId,
            OrderNumber = refund.OrderNumber,
            Email = refund.Email,
            FullName = refund.CustomerName,
            Amount = refund.Amount,
            Status = refund.Status,
            CreatedAt = refund.CreatedAt,
            ProcessedAt = refund.ProcessedAt,
            ReturnId = refund.ReturnId,
            StripeRefundId = refund.StripeRefundId,
            FailureReason = refund.FailureReason,
            Currency = refund.Currency,
            StripePaymentIntentId = refund.StripePaymentIntentId
        };
    }
}
