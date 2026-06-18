namespace Trap_Intel.Infrastructure.Notifications.RealTime;

public interface IListUpdatesHubClient
{
    Task ListChanged(ListUpdateMessage message);
}
