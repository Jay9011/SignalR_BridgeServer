namespace SignalRBridgeServer.Models
{
    public static partial class Methods
    {
        /// <summary>
        /// 신규 그룹이 생성되었을 때 호출되는 메서드
        /// </summary>
        public const string GroupCreated = "GroupCreated";
        /// <summary>
        /// 그룹이 삭제되었을 때 호출되는 메서드
        /// </summary>
        public const string GroupRemoved = "GroupRemoved";
        /// <summary>
        /// 그룹 목록을 요청했을 때 호출되는 메서드
        /// </summary>
        public const string GroupList = "GroupList";
        /// <summary>
        /// 그룹 정보를 요청했을 때 호출되는 메서드
        /// </summary>
        public const string GroupInfo = "GroupInfo";
        /// <summary>
        /// 그룹 메타데이터가 업데이트되었을 때 호출되는 메서드
        /// </summary>
        public const string GroupMetadataUpdated = "GroupMetadataUpdated";
        /// <summary>
        /// 그룹에 신규 멤버가 참여했을 때 호출되는 메서드
        /// </summary>
        public const string GroupMemberJoined = "GroupMemberJoined";
        /// <summary>
        /// 그룹에서 멤버가 나갔을 때 호출되는 메서드
        /// </summary>
        public const string GroupMemberLeft = "GroupMemberLeft";
        /// <summary>
        /// 그룹에 속한 멤버 목록을 요청했을 때 호출되는 메서드
        /// </summary>
        public const string GroupMemberList = "GroupMemberList";
        /// <summary>
        /// 그룹 메시지를 받았을 때 호출되는 메서드
        /// </summary>
        public const string ReceiveGroupMessage = "ReceiveGroupMessage";
    }
}
