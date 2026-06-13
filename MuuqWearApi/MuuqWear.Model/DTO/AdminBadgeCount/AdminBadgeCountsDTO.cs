namespace MuuqWear.Model.DTO.AdminBadgeCount;


public class AdminBadgeCountsDTO
{
    public int PendingOrders { get; set; }
    public int TotalCustomers { get; set; }          // changed
    public int TotalProducts { get; set; }            // changed
    public AffiliateCountsDTO AffiliateCounts { get; set; } = new();
    public int ActiveChats { get; set; }
    public int OpenTickets { get; set; }
}


public class AffiliateCountsDTO
{
    public int Pending { get; set; }
    public int Approved { get; set; }
    public int Rejected { get; set; }
    public int Waitlisted { get; set; }

    public int Total => Pending + Approved + Rejected + Waitlisted;
}
