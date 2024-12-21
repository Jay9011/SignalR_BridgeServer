using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using SignalRBridgeServer.Models;

namespace SignalRBridgeServer.Hubs
{
    public partial class BridgeHub : Hub
    {
        private static ConcurrentDictionary<string, ClientInfo> _connectedClients = new();

        private readonly ILogger<BridgeHub> _logger;

        public BridgeHub(ILogger<BridgeHub> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 클라이언트가 연결되었을 때 호출되는 메서드
        /// </summary>
        /// <remarks>
        ///     <list type="bullet">
        ///         <item><see cref="Methods.RequestRegistration"/> (): 요청 클라이언트에게 정보 등록 요청 메시지 전송.</item>
        ///         <item><see cref="Methods.ClientConnected"/> (ClientID): 다른 클라이언트에게 새로운 클라이언트 연결 정보 전송.</item>
        ///         <item><see cref="Methods.ConnectedClients"/> (IEnumerable&lt;<see cref="ClientInfo"/>&gt;): 요청 클라이언트에게 현재 연결된 클라이언트 목록 전송.</item>
        ///     </list>
        /// </remarks>
        public override async Task OnConnectedAsync()
        {
            var clientId = Context.ConnectionId;
            _connectedClients.TryAdd(clientId, new ClientInfo(clientId));

            await base.OnConnectedAsync();

            // 요청 클라이언트에게 정보 등록 요청 메시지 전송
            await Clients.Caller.SendAsync(Methods.RequestRegistration);
            // 다른 클라이언트에게 새로운 클라이언트 연결 정보 전송
            await Clients.Others.SendAsync(Methods.ClientConnected, clientId);
            // 요청 클라이언트에게 현재 연결된 클라이언트 목록 전송
            await Clients.Caller.SendAsync(Methods.ConnectedClients, _connectedClients.Values);

            _logger.LogInformation($"Client connected: {clientId}");
        }

        /// <summary>
        /// 클라이언트가 연결이 끊겼을 때 호출되는 메서드
        /// </summary>
        /// <param name="exception">연결이 끊긴 이유를 설명하는 예외 객체</param>
        /// <remarks>
        ///     <list type="bullet">
        ///         <item><see cref="Methods.ClientDisconnected"/> (ClientID): 다른 클라이언트에게 연결이 끊긴 클라이언트 정보 전송.</item>
        ///     </list>
        /// </remarks>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var clientId = Context.ConnectionId;
            _connectedClients.TryRemove(clientId, out _);

            // 현재 연결이 끊긴 클라이언트가 그룹에 속해있는 경우 그룹에서 제거
            foreach (var group in _groups.Values)
            {
                group.Members.TryRemove(clientId, out _);
            }

            // 다른 클라이언트에게 연결이 끊긴 클라이언트 정보 전송
            await Clients.Others.SendAsync(Methods.ClientDisconnected, clientId);

            await base.OnDisconnectedAsync(exception);

            _logger.LogInformation($"Client disconnected: {clientId}");
        }

        /// <summary>
        /// 클라이언트 정보를 등록하는 메서드
        /// </summary>
        /// <param name="clientName">클라이언트 식별자 이름(SignalR에서 사용하는 ID와는 다른 식별자)</param>
        /// <param name="metadata">클라이언트의 부가 정보</param>
        /// <remarks>
        ///     <list type="bullet">
        ///         <item><see cref="Methods.ClientUpdated"/> (ClientID, <see cref="ClientInfo"/>): 다른 클라이언트에게 업데이트된 정보를 전송.</item>
        ///         <item><see cref="Methods.SuccessRegistered"/> (ClientID, <see cref="ClientInfo"/>): 요청 클라이언트에게 등록 성공 메시지 전송.</item>
        ///     </list>
        /// </remarks>
        public async Task RegisterClient(string clientName, Dictionary<string, string> metadata)
        {
            var clientId = Context.ConnectionId;
            if (_connectedClients.TryGetValue(clientId, out var clientInfo))
            {
                clientInfo.Name = clientName;
                clientInfo.ConnectedAt = DateTime.UtcNow;
                clientInfo.Metadata = metadata;

                // 다른 클라이언트에게 업데이트된 정보를 전송
                await Clients.Others.SendAsync(Methods.ClientUpdated, clientId, clientInfo);

                // 요청 클라이언트에게 등록 성공 메시지 전송
                await Clients.Caller.SendAsync(Methods.SuccessRegistered, clientId, clientInfo);
            }
        }

        /// <summary>
        /// 클라이언트의 부가 정보를 업데이트하는 메서드
        /// </summary>
        /// <param name="metadata">업데이트할 클라이언트의 부가 정보</param>
        /// <remarks>
        ///     <list type="bullet">
        ///         <item><see cref="Methods.ClientUpdated"/> (ClientID, <see cref="ClientInfo"/>): 다른 클라이언트에게 업데이트된 정보를 전송.</item>
        ///     </list>
        /// </remarks>
        public async Task UpdateClientMetadata(Dictionary<string, string> metadata)
        {
            var clientId = Context.ConnectionId;
            if (_connectedClients.TryGetValue(clientId, out var clientInfo))
            {
                clientInfo.Metadata = metadata;
                // 다른 클라이언트에게 업데이트된 정보를 전송
                await Clients.Others.SendAsync(Methods.ClientUpdated, clientId, clientInfo);
            }
        }

        /// <summary>
        /// 모든 클라이언트에게 메시지를 전송하는 메서드
        /// </summary>
        /// <param name="message">전송할 메시지</param>
        /// <remarks>
        ///     <list type="bullet">
        ///         <item><see cref="Methods.ReceiveMessage"/> (ClientID, string, DateTime): 모든 클라이언트에게 메시지 전송.</item>
        ///     </list>
        /// </remarks>
        public async Task BroadcastMessage(string message)
        {
            // 모든 클라이언트에게 메시지 전송
            await Clients.All.SendAsync(Methods.ReceiveMessage, Context.ConnectionId, message, DateTime.UtcNow);
        }

        /// <summary>
        /// 다른 클라이언트에게 메시지를 전송하는 메서드
        /// </summary>
        /// <param name="message">전송할 메시지</param>
        /// <remarks>
        ///     <list type="bullet">
        ///         <item><see cref="Methods.ReceiveMessage"/> (ClientID, string, DateTime): 다른 클라이언트에게 메시지 전송.</item>
        ///     </list>
        /// </remarks>
        public async Task SendToOthers(string message)
        {
            // 다른 클라이언트에게 메시지 전송
            await Clients.Others.SendAsync(Methods.ReceiveMessage, Context.ConnectionId, message, DateTime.UtcNow);
        }

        /// <summary>
        /// 특정 클라이언트에게 메시지를 전송하는 메서드
        /// </summary>
        /// <param name="targetClientId">메시지를 받을 클라이언트의 ID</param>
        /// <param name="message">전송할 메시지</param>
        /// <remarks>
        ///     <list type="bullet">
        ///         <item><see cref="Methods.ReceiveMessage"/> (ClientID, string, DateTime): 특정 클라이언트에게 메시지 전송.</item>
        ///         <item><see cref="Methods.ReceiveMessage"/> ("Server", "Fail: string", DateTime): 요청 클라이언트에게 클라이언트를 찾을 수 없다는 메시지 전송.</item>
        ///     </list>
        /// </remarks>
        public async Task SendToClient(string targetClientId, string message)
        {
            if (_connectedClients.ContainsKey(targetClientId))
            {
                // 특정 클라이언트에게 메시지 전송
                await Clients.Client(targetClientId).SendAsync(Methods.ReceiveMessage, Context.ConnectionId, message, DateTime.UtcNow);
            }
            else
            {
                // 요청 클라이언트에게 클라이언트를 찾을 수 없다는 메시지 전송
                await Clients.Caller.SendAsync(Methods.ReceiveMessage, "Server", $"Fail: Client {targetClientId} not found", DateTime.UtcNow);
            }
        }

        /// <summary>
        /// 연결된 클라이언트 목록을 요청하는 메서드
        /// </summary>
        /// <remarks>
        ///     <list type="bullet">
        ///         <item><see cref="Methods.ConnectedClients"/> (IEnumerable&lt;<see cref="ClientInfo"/>&gt;): 요청 클라이언트에게 현재 연결된 클라이언트 목록 전송.</item>
        ///     </list>
        /// </remarks>
        public async Task RequestConnectedClients()
        {
            // 요청 클라이언트에게 현재 연결된 클라이언트 목록 전송
            await Clients.Caller.SendAsync(Methods.ConnectedClients, _connectedClients.Values);
        }

        /// <summary>
        /// 특정 클라이언트의 정보를 요청하는 메서드
        /// </summary>
        /// <param name="targetClientId">특정 클라이언트 ID</param>
        /// <remarks>
        ///     <list type="bullet">
        ///         <item><see cref="Methods.ClientUpdated"/> (ClientID, <see cref="ClientInfo"/>): 요청 클라이언트에게 클라이언트 정보 전송.</item>
        ///         <item><see cref="Methods.ReceiveMessage"/> ("Server", "Fail: string", DateTime): 요청 클라이언트에게 클라이언트를 찾을 수 없다는 메시지 전송.</item>
        ///     </list>
        /// </remarks>
        public async Task RequestClientInfo(string targetClientId)
        {
            if (_connectedClients.TryGetValue(targetClientId, out var clientInfo))
            {
                // 요청 클라이언트에게 클라이언트 정보 전송
                await Clients.Caller.SendAsync(Methods.ClientUpdated, targetClientId, clientInfo);
            }
            else
            {
                // 요청 클라이언트에게 클라이언트를 찾을 수 없다는 메시지 전송
                await Clients.Caller.SendAsync(Methods.ReceiveMessage, "Server", $"Fail: Client {targetClientId} not found", DateTime.UtcNow);
            }
        }
    }
}
