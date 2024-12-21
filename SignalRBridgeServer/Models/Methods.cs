namespace SignalRBridgeServer.Models
{
    public static partial class Methods
    {
        /// <summary>
        /// 다른 클라이언트가 연결되었을 때 호출되는 메서드
        /// </summary>
        public const string ClientConnected = "ClientConnected";
        /// <summary>
        /// 다른 클라이언트가 연결이 끊겼을 때 호출되는 메서드
        /// </summary>
        public const string ClientDisconnected = "ClientDisconnected";
        /// <summary>
        /// 현재 연결된 클라이언트 목록을 전송하는 메서드
        /// </summary>
        public const string ConnectedClients = "ConnectedClients";
        /// <summary>
        /// 정보 등록을 요청하는 메서드
        /// </summary>
        public const string RequestRegistration = "RequestRegistration";
        /// <summary>
        /// 정보 등록을 성공 했을 때 호출되는 메서드
        /// </summary>
        public const string SuccessRegistered = "SuccessRegistered";
        /// <summary>
        /// 다른 클라이언트의 정보가 업데이트 되었을 때 호출되는 메서드
        /// </summary>
        public const string ClientUpdated = "ClientUpdated";
        /// <summary>
        /// 메시지를 받았을 때 호출되는 메서드
        /// </summary>
        public const string ReceiveMessage = "ReceiveMessage";
    }
}
