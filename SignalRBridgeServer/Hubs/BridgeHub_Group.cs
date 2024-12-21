using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using SignalRBridgeServer.Models;

namespace SignalRBridgeServer.Hubs
{
    public partial class BridgeHub
    {
        private static ConcurrentDictionary<string, GroupInfo> _groups = new();

        /// <summary>
        /// 그룹에 참여하는 메서드
        /// </summary>
        /// <param name="groupName">그룹명</param>
        /// <param name="metadata">메타데이터</param>
        /// <remarks>
        ///     <list type="bullet">
        ///         <item><see cref="Methods.GroupCreated"/> (<see cref="GroupInfo"/>): 다른 클라이언트에게 새로운 그룹이 생성되었음을 알림.</item>
        ///         <item><see cref="Methods.GroupInfo"/> (<see cref="GroupInfo"/>): 다른 클라이언트에게 그룹 정보 변경을 알림.</item>
        ///         <item><see cref="Methods.GroupMemberJoined"/> (ClientID, <see cref="ClientInfo"/>): 그룹 내 다른 클라이언트에게 새로운 멤버가 참여했음을 알림.</item>
        ///     </list>
        /// </remarks>
        public async Task JoinGroup(string groupName, Dictionary<string, string> metadata)
        {
            bool isNewGroup = !_groups.ContainsKey(groupName);
            
            // SignalR 그룹에 추가 혹은 참여
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

            // 그룹 정보 관리 (신규 혹은 참여)
            var groupInfo = _groups.GetOrAdd(groupName, new GroupInfo(groupName, metadata));
            
            var clientInfo = _connectedClients[Context.ConnectionId];

            // 그룹에 참여한 클라이언트 정보 추가
            groupInfo.Members.TryAdd(Context.ConnectionId, clientInfo);

            // 그룹이 신규 생성되었을 경우
            if (isNewGroup)
            {
                // 다른 클라이언트들에게 새로운 그룹이 생성되었음을 알림
                await Clients.Others.SendAsync(Methods.GroupCreated, groupInfo);
            }
            else
            {
                // 다른 클라이언트들에게 그룹 맴버 변경이 있었음을 알림
                await Clients.OthersInGroup(groupName).SendAsync(Methods.GroupInfo, groupInfo);
            }

            // 그룹 내 다른 클라이언트들에게 새로운 멤버가 참여했음을 알림
            await Clients.Group(groupName).SendAsync(Methods.GroupMemberJoined, Context.ConnectionId, clientInfo);

            _logger.LogInformation($"Client '{Context.ConnectionId}' joined group '{groupName}'");
        }

        /// <summary>
        /// 그룹에서 나가는 메서드
        /// </summary>
        /// <param name="groupName">그룹명</param>
        /// <remarks>
        ///     <list type="bullet">
        ///         <item><see cref="Methods.GroupMemberLeft"/> (ClientID): 그룹 내 다른 클라이언트에게 멤버가 나갔음을 알림.</item>
        ///         <item><see cref="Methods.GroupInfo"/> (<see cref="GroupInfo"/>): 다른 클라이언트에게 그룹 정보 변경을 알림.</item>
        ///         <item><see cref="Methods.GroupRemoved"/> (GroupName): 다른 클라이언트에게 그룹이 삭제되었음을 알림.</item>
        ///     </list>
        /// </remarks>
        public async Task LeaveGroup(string groupName)
        {
            bool isRemoved = false;

            // 그룹에서 나가기
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

            if (_groups.TryGetValue(groupName, out var groupInfo))
            {
                groupInfo.Members.TryRemove(Context.ConnectionId, out _);

                // 그룹에 멤버가 남아있지 않으면 그룹 정보 삭제
                if (groupInfo.Members.IsEmpty)
                {
                    _groups.TryRemove(groupName, out _);
                    isRemoved = true;
                }
            }

            // 그룹 내 다른 클라이언트들에게 멤버가 나갔음을 알림
            await Clients.Group(groupName).SendAsync(Methods.GroupMemberLeft, Context.ConnectionId);

            // 다른 클라이언트들에게 그룹 맴버 변경이 있었음을 알림
            await Clients.OthersInGroup(groupName).SendAsync(Methods.GroupInfo, groupInfo);

            if (isRemoved)
            {
                // 다른 클라이언트들에게 그룹이 삭제되었음을 알림
                await Clients.Others.SendAsync(Methods.GroupRemoved, groupName);
            }
        }

        /// <summary>
        /// 그룹 메타데이터를 업데이트하는 메서드
        /// </summary>
        /// <param name="groupName">그룹명</param>
        /// <param name="metadata">메타데이터</param>
        /// <remarks>
        ///     <list type="bullet">
        ///         <item><see cref="Methods.GroupMetadataUpdated"/> (<see cref="GroupInfo"/>): 그룹 내 클라이언트에게 메타데이터가 업데이트되었음을 알림.</item>
        ///         <item><see cref="Methods.GroupMetadataUpdated"/> ("Fail: string"): 그룹 메타데이터 업데이트 실패시 요청자에게 실패 메시지 전송.</item>
        ///     </list>
        /// </remarks>
        public async Task UpdateGroupMetadata(string groupName, Dictionary<string, string> metadata)
        {
            if (_groups.TryGetValue(groupName, out var groupInfo))
            {
                groupInfo.Metadata = metadata;

                // 그룹 내 클라이언트들에게 메타데이터가 업데이트되었음을 알림
                await Clients.Group(groupName).SendAsync(Methods.GroupMetadataUpdated, groupInfo);
            }
            else
            {
                // 그룹 메타데이터 업데이트 실패시 요청자에게 실패 메시지 전송
                await Clients.Caller.SendAsync(Methods.GroupMetadataUpdated, $"Fail: Group '{groupName}' not found.");
            }
        }

        /// <summary>
        /// 그룹 정보를 요청하는 메서드
        /// </summary>
        /// <param name="groupName">그룹명</param>
        /// <remarks>
        ///     <list type="bullet">
        ///         <item><see cref="Methods.GroupInfo"/> (<see cref="GroupInfo"/>): 요청 클라이언트에게 그룹 정보 전송.</item>
        ///         <item><see cref="Methods.GroupInfo"/> ("Fail: string"): 그룹 정보를 찾을 수 없을 때 요청 클라이언트에게 실패 메시지 전송.</item>
        ///     </list>
        /// </remarks>
        public async Task GetGroupInfo(string groupName)
        {
            if (_groups.TryGetValue(groupName, out var groupInfo))
            {
                await Clients.Caller.SendAsync(Methods.GroupInfo, groupInfo);
            }
            else
            {
                await Clients.Caller.SendAsync(Methods.GroupInfo, $"Fail: Group '{groupName}' not found.");
            }
        }

        /// <summary>
        /// 그룹 목록을 요청하는 메서드
        /// </summary>
        /// <remarks>
        ///     <list type="bullet">
        ///         <item><see cref="Methods.GroupList"/> (IEnumerable&lt;<see cref="GroupInfo"/>&gt;): 요청 클라이언트에게 그룹 목록 전송.</item>
        ///     </list>
        /// </remarks>
        public async Task GetGroupList()
        {
            var groupList = _groups.Values;

            // 요청 클라이언트에게 그룹 목록 전송
            await Clients.Caller.SendAsync(Methods.GroupList, groupList);
        }

        /// <summary>
        /// 그룹 멤버 목록을 요청하는 메서드
        /// </summary>
        /// <param name="groupName">그룹명</param>
        /// <remarks>
        ///     <list type="bullet">
        ///         <item><see cref="Methods.GroupMemberList"/> (IEnumerable&lt;<see cref="ClientInfo"/>&gt;): 요청 클라이언트에게 그룹 멤버 목록 전송.</item>
        ///         <item><see cref="Methods.GroupMemberList"/> ("Fail: string"): 그룹 멤버 목록을 찾을 수 없을 때 요청 클라이언트에게 실패 메시지 전송.</item>
        ///     </list>
        /// </remarks>
        public async Task GetGroupMember(string groupName)
        {
            if (_groups.TryGetValue(groupName, out var groupInfo))
            {
                await Clients.Caller.SendAsync(Methods.GroupMemberList, groupInfo.Members.Values);
            }
            else
            {
                await Clients.Caller.SendAsync(Methods.GroupMemberList, $"Fail: Group '{groupName}' not found.");
            }
        }

        /// <summary>
        /// 그룹 메시지를 보내는 메서드
        /// </summary>
        /// <param name="groupName">그룹명</param>
        /// <param name="message">메시지</param>
        /// <remarks>
        ///     <list type="bullet">
        ///         <item><see cref="Methods.ReceiveGroupMessage"/> (ClientID, string, DateTime): 그룹 내 모든 클라이언트에게 메시지 전송.</item>
        ///     </list>
        /// </remarks>
        public async Task SendToGroup(string groupName, string message)
        {
            await Clients.Group(groupName).SendAsync(Methods.ReceiveGroupMessage, Context.ConnectionId, message, DateTime.UtcNow);
        }

        /// <summary>
        /// 그룹 내 다른 클라이언트들에게 메시지를 보내는 메서드
        /// </summary>
        /// <param name="groupName">그룹명</param>
        /// <param name="message">메시지</param>
        /// <remarks>
        ///     <list type="bullet">
        ///         <item><see cref="Methods.ReceiveGroupMessage"/> (ClientID, string, DateTime): 그룹 내 다른 클라이언트에게 메시지 전송.</item>
        ///     </list>
        /// </remarks>
        public async Task SendToOthersInGroup(string groupName, string message)
        {
            await Clients.OthersInGroup(groupName).SendAsync(Methods.ReceiveGroupMessage, Context.ConnectionId, message, DateTime.UtcNow);
        }
    }
}
