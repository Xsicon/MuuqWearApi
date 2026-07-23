namespace MuuqWear.Application.Shared;

/// <summary>
/// Centralized chat session ownership rules (used with service-role Supabase access).
/// </summary>
public static class ChatSessionAccess
{
    public static bool IsSessionOwner(Guid? sessionUserId, Guid? callerUserId)
    {
        if (callerUserId.HasValue)
            return sessionUserId == callerUserId.Value;

        return sessionUserId == null;
    }

    public static bool CanAccessSession(bool isChatAdmin, Guid? sessionUserId, Guid? callerUserId) =>
        isChatAdmin || IsSessionOwner(sessionUserId, callerUserId);
}
