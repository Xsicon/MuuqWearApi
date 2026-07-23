using MuuqWear.Model.Models.Chat;
using Supabase;

namespace MuuqWear.Application.Shared;

/// <summary>
/// Reliable chat message queries that avoid PostgREST row-limit truncation.
/// </summary>
public static class ChatMessageQueryHelper
{
    private const int PageSize = 1000;
    private const int LatestMessageBatchSize = 10;

    public static async Task<Dictionary<Guid, ChatMessage>> LoadLatestMessagesBySessionAsync(
        Client client,
        IReadOnlyList<Guid> sessionIds)
    {
        if (sessionIds.Count == 0)
            return new Dictionary<Guid, ChatMessage>();

        var latestBySession = new Dictionary<Guid, ChatMessage>();

        foreach (var batch in sessionIds.Chunk(LatestMessageBatchSize))
        {
            var batchResults = await Task.WhenAll(batch.Select(async sessionId =>
            {
                var response = await client
                    .From<ChatMessage>()
                    .Where(m => m.SessionId == sessionId)
                    .Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending)
                    .Limit(1)
                    .Get();

                return (sessionId, Message: response.Models?.FirstOrDefault());
            }));

            foreach (var (sessionId, message) in batchResults)
            {
                if (message != null)
                    latestBySession[sessionId] = message;
            }
        }

        return latestBySession;
    }

    public static async Task<Dictionary<Guid, SessionMessageStats>> LoadMessageStatsBySessionAsync(
        Client client,
        IReadOnlyList<Guid> sessionIds)
    {
        if (sessionIds.Count == 0)
            return new Dictionary<Guid, SessionMessageStats>();

        var allMessages = await LoadAllMessagesForSessionsAsync(client, sessionIds);

        return allMessages
            .GroupBy(m => m.SessionId)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var ordered = g.OrderBy(m => m.CreatedAt).ToList();
                    return new SessionMessageStats(
                        ordered.Count,
                        CountUnreadCustomerMessages(ordered));
                });
    }

    public static int CountWaitingSessions(
        IReadOnlyList<ChatSession> sessions,
        IReadOnlyDictionary<Guid, ChatMessage> latestMessageBySession)
    {
        return sessions.Count(session =>
        {
            if (!latestMessageBySession.TryGetValue(session.Id, out var latest))
                return true;

            return latest.SenderType is "customer";
        });
    }

    public static int CountUnreadCustomerMessages(IReadOnlyList<ChatMessage> orderedMessages)
    {
        var lastAdminIndex = -1;
        for (var i = orderedMessages.Count - 1; i >= 0; i--)
        {
            if (orderedMessages[i].SenderType is "admin" or "agent")
            {
                lastAdminIndex = i;
                break;
            }
        }

        var unread = 0;
        for (var i = lastAdminIndex + 1; i < orderedMessages.Count; i++)
        {
            if (orderedMessages[i].SenderType == "customer")
                unread++;
        }

        return unread;
    }

    private static async Task<List<ChatMessage>> LoadAllMessagesForSessionsAsync(
        Client client,
        IReadOnlyList<Guid> sessionIds)
    {
        var allMessages = new List<ChatMessage>();
        var offset = 0;

        while (true)
        {
            var response = await client
                .From<ChatMessage>()
                .Filter(
                    "session_id",
                    Supabase.Postgrest.Constants.Operator.In,
                    sessionIds.Select(id => id.ToString()).ToList())
                .Order("created_at", Supabase.Postgrest.Constants.Ordering.Ascending)
                .Range(offset, offset + PageSize - 1)
                .Get();

            var page = response.Models ?? new List<ChatMessage>();
            allMessages.AddRange(page);

            if (page.Count < PageSize)
                break;

            offset += PageSize;
        }

        return allMessages;
    }

    public sealed record SessionMessageStats(int Total, int Unread);
}
