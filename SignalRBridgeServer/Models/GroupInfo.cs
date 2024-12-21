using System.Collections.Concurrent;

namespace SignalRBridgeServer.Models
{
    public class GroupInfo
    {
        public string GroupId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, string> Metadata { get; set; }
        public ConcurrentDictionary<string, ClientInfo> Members { get; set; } = new();

        public GroupInfo(string groupId, Dictionary<string, string> metadata = null)
        {
            GroupId = groupId;
            Metadata = metadata ?? new();
        }
    }
}
