namespace SignalRBridgeServer.Models
{
    public class ClientInfo
    {
        public string ClientId { get; set; }
        public string Name { get; set; }
        public DateTime ConnectedAt { get; set; }
        public Dictionary<string, string> Metadata { get; set; }

        public ClientInfo(string clientId)
        {
            ClientId = clientId;
            Metadata = new();
        }
    }
}
