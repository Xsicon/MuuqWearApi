using MuuqWear.Application.Shared;
using MuuqWear.Model.Models.Chat;
using Xunit;

namespace MuuqWearApi.Tests.Chat;

public class ChatMessageQueryHelperTests
{
    private static readonly Guid SessionId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public void CountUnreadCustomerMessages_counts_only_after_last_admin_reply()
    {
        var messages = new List<ChatMessage>
        {
            CreateMessage("customer", minutesAgo: 30),
            CreateMessage("admin", minutesAgo: 20),
            CreateMessage("customer", minutesAgo: 10),
            CreateMessage("customer", minutesAgo: 5)
        };

        Assert.Equal(2, ChatMessageQueryHelper.CountUnreadCustomerMessages(messages));
    }

    [Fact]
    public void CountUnreadCustomerMessages_treats_agent_as_staff_reply()
    {
        var messages = new List<ChatMessage>
        {
            CreateMessage("customer", minutesAgo: 10),
            CreateMessage("agent", minutesAgo: 5),
            CreateMessage("customer", minutesAgo: 1)
        };

        Assert.Equal(1, ChatMessageQueryHelper.CountUnreadCustomerMessages(messages));
    }

    [Fact]
    public void CountWaitingSessions_counts_empty_and_customer_last_threads()
    {
        var sessions = new List<ChatSession>
        {
            new() { Id = SessionId, Status = "active" },
            new() { Id = Guid.NewGuid(), Status = "active" }
        };

        var latest = new Dictionary<Guid, ChatMessage>
        {
            [sessions[1].Id] = CreateMessage("admin", minutesAgo: 1, sessionId: sessions[1].Id)
        };

        Assert.Equal(1, ChatMessageQueryHelper.CountWaitingSessions(sessions, latest));
    }

    private static ChatMessage CreateMessage(
        string senderType,
        int minutesAgo,
        Guid? sessionId = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId ?? SessionId,
            SenderType = senderType,
            Message = "hello",
            CreatedAt = DateTime.UtcNow.AddMinutes(-minutesAgo)
        };
}
