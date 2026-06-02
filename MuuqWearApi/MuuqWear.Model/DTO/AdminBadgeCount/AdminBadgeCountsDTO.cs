namespace MuuqWear.Model.DTO.AdminBadgeCount;


public class AdminBadgeCountsDTO
{
    public int PendingOrders { get; set; }
    public int TotalCustomers { get; set; }          // changed
    public int TotalProducts { get; set; }            // changed
    public int PendingAffiliateApplications { get; set; }
    public int ActiveChats { get; set; }
    public int OpenTickets { get; set; }
}
