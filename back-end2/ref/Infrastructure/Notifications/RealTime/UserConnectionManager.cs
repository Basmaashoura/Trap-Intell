using System.Collections.Concurrent;

namespace Infrastructure.Notifications.RealTime;

/// <summary>
/// Manages user connections for SignalR notifications.
/// </summary>
public sealed class UserConnectionManager
{
    // Maps userId to list of connection IDs (user can have multiple connections)
    private readonly ConcurrentDictionary<Guid, HashSet<string>> _userConnections = new();

    // Maps connection ID to userId for reverse lookup
    private readonly ConcurrentDictionary<string, Guid> _connectionUsers = new();

    /// <summary>
    /// Adds a connection for a user.
    /// </summary>
    public void AddConnection(Guid userId, string connectionId)
    {
        _userConnections.AddOrUpdate(
            userId,
            _ => [connectionId],
            (_, connections) =>
            {
                lock (connections)
                {
                    connections.Add(connectionId);
                }
                return connections;
            });

        _connectionUsers[connectionId] = userId;
    }


    /// <summary>
    /// Removes a connection.
    /// </summary>
    public void RemoveConnection(string connectionId)
    {
        if (_connectionUsers.TryRemove(connectionId, out Guid userId) &&
            _userConnections.TryGetValue(userId, out HashSet<string>? connections))
        {
            lock (connections)
            {
                connections.Remove(connectionId);

                // Clean up empty user entry
                if (connections.Count == 0)
                {
                    _userConnections.TryRemove(userId, out _);
                }
            }
        }
    }

    /// <summary>
    /// Gets all connection IDs for a user.
    /// </summary>
    public IReadOnlyList<string> GetConnections(Guid userId)
    {
        if (_userConnections.TryGetValue(userId, out HashSet<string>? connections))
        {
            lock (connections)
            {
                return [.. connections];
            }
        }

        return [];
    }

    /// <summary>
    /// Gets all connection IDs for multiple users.
    /// </summary>
    public IReadOnlyList<string> GetConnections(IEnumerable<Guid> userIds)
    {
        var allConnections = new List<string>();

        foreach (Guid userId in userIds)
        {
            allConnections.AddRange(GetConnections(userId));
        }

        return allConnections;
    }

    /// <summary>
    /// Gets the user ID for a connection.
    /// </summary>
    public Guid? GetUserId(string connectionId)
    {
        return _connectionUsers.TryGetValue(connectionId, out Guid userId) ? userId : null;
    }

    /// <summary>
    /// Checks if a user has any active connections.
    /// </summary>
    public bool IsUserConnected(Guid userId)
    {
        return _userConnections.TryGetValue(userId, out HashSet<string>? connections)
               && connections.Count > 0;
    }

    /// <summary>
    /// Gets count of connected users.
    /// </summary>
    public int GetConnectedUserCount() => _userConnections.Count;

    /// <summary>
    /// Gets count of total connections.
    /// </summary>
    public int GetConnectionCount() => _connectionUsers.Count;
}
