# SignalR BridgeServer

SignalR BridgeServer는 실시간 양방향 통신을 위한 서버로, 클라이언트 간의 메시지 전달과 그룹 관리 기능을 제공합니다.

## 목차

- [기본 기능](#기본-기능)
  - [클라이언트 연결 관리](#클라이언트-연결-관리)
  - [메시지 전송](#메시지-전송)
  - [클라이언트 정보 관리](#클라이언트-정보-관리)
- [그룹 기능](#그룹-기능)
  - [그룹 관리](#그룹-관리)
  - [그룹 메시지](#그룹-메시지)
  - [그룹 정보 관리](#그룹-정보-관리)

## 기본 기능

### 클라이언트 연결 관리

#### 클라이언트 연결 시 (OnConnectedAsync)

- 서버 → 연결 클라이언트: `RequestRegistration`
  - 클라이언트 정보 등록 요청
- 서버 → 다른 클라이언트들: `ClientConnected (clientId)`
  - 새로운 클라이언트 연결 알림
- 서버 → 연결 클라이언트: `ConnectedClients (List<ClientInfo>)`
  - 현재 연결된 클라이언트 목록 전송

#### 클라이언트 연결 해제 시 (OnDisconnectedAsync)

- 서버 → 다른 클라이언트들: `ClientDisconnected (clientId)`
  - 클라이언트 연결 해제 알림

### 메시지 전송

#### 전체 메시지 (BroadcastMessage(string message))

- 클라이언트 → 서버: `BroadcastMessage (message)`
- 서버 → 모든 클라이언트: `ReceiveMessage (senderId, message, timestamp)`

#### 특정 클라이언트 제외 메시지 (SendToOthers(string message))

- 클라이언트 → 서버: `SendToOthers (message)`
- 서버 → 다른 클라이언트들: `ReceiveMessage (senderId, message, timestamp)`

#### 특정 클라이언트 메시지 (SendToClient(string targetClientId, string message))

- 클라이언트 → 서버: `SendToClient (targetClientId, message)`
- 서버 → 대상 클라이언트: `ReceiveMessage (senderId, message, timestamp)`
- 서버 → 요청 클라이언트: `ReceiveMessage ("Server", "Fail: Client {targetClientId} not found", timestamp)`
  - 대상 클라이언트를 찾을 수 없는 경우 앞에 "Fail: "을 붙여 전송

### 클라이언트 정보 관리

#### 클라이언트 등록 (RegisterClient(string clientName, Dictionary<string, string> metadata))

- 클라이언트 → 서버: `RegisterClient (clientName, metadata)`
- 서버 → 다른 클라이언트들: `ClientUpdated (clientId, ClientInfo)`
- 서버 → 요청 클라이언트: `SuccessRegistered (clientId, ClientInfo)`

#### 클라이언트 메타데이터 업데이트 (UpdateClientMetadata(Dictionary<string, string> metadata))

- 클라이언트 → 서버: `UpdateClientMetadata (metadata)`
- 서버 → 다른 클라이언트들: `ClientUpdated (clientId, ClientInfo)`

#### 클라이언트 정보 조회 (RequestConnectedClients())

- 클라이언트 → 서버: `RequestConnectedClients ()`
- 서버 → 요청 클라이언트: `ConnectedClients (List<ClientInfo>)`

#### 특정 클라이언트 정보 조회 (RequestClientInfo(string targetClientId))

- 클라이언트 → 서버: `RequestClientInfo (targetClientId)`
- 서버 → 요청 클라이언트: `ClientUpdated (targetClientId, ClientInfo)`
- 서버 → 요청 클라이언트: `ReceiveMessage ("Server", "Fail: Client {targetClientId} not found", timestamp)`
  - 대상 클라이언트를 찾을 수 없는 경우 앞에 "Fail: "을 붙여 전송

## 그룹 기능

### 그룹 관리

#### 그룹 참여 (JoinGroup(string groupName, Dictionary<string, string> metadata))

- 클라이언트 → 서버: `JoinGroup (groupName, metadata)`
- 서버 → 다른 클라이언트들: `GroupCreated (GroupInfo)`
  - 새로운 그룹이 생성된 경우
- 서버 → 그룹의 다른 클라이언트들: `GroupInfo (GroupInfo)`
  - 기존 그룹에 참여한 경우
- 서버 → 그룹의 모든 클라이언트들: `GroupMemberJoined (clientId, ClientInfo)`

#### 그룹 나가기 (LeaveGroup(string groupName))

- 클라이언트 → 서버: `LeaveGroup (groupName)`
- 서버 → 그룹의 클라이언트들: `GroupMemberLeft (clientId)`
- 서버 → 그룹의 다른 클라이언트들: `GroupInfo (GroupInfo)`
- 서버 → 다른 클라이언트들: `GroupRemoved (groupName)`
  - 그룹의 모든 멤버가 나가서 그룹이 삭제된 경우

### 그룹 메시지

#### 그룹 전체 메시지 (SendToGroup(string groupName, string message))

- 클라이언트 → 서버: `SendToGroup (groupName, message)`
- 서버 → 그룹의 모든 클라이언트들: `ReceiveGroupMessage (senderId, message, timestamp)`

#### 그룹의 다른 클라이언트 메시지 (SendToOthersInGroup(string groupName, string message))

- 클라이언트 → 서버: `SendToOthersInGroup (groupName, message)`
- 서버 → 그룹의 다른 클라이언트들: `ReceiveGroupMessage (senderId, message, timestamp)`

### 그룹 정보 관리

#### 그룹 메타데이터 업데이트 (UpdateGroupMetadata(string groupName, Dictionary<string, string> metadata))

- 클라이언트 → 서버: `UpdateGroupMetadata (groupName, metadata)`
- 서버 → 그룹의 클라이언트들: `GroupMetadataUpdated (GroupInfo)`
- 서버 → 요청 클라이언트: `GroupMetadataUpdated ("Fail: Group '{groupName}' not found")`
    - 그룹을 찾을 수 없는 경우 앞에 "Fail: "을 붙여 전송

#### 그룹 정보 조회 (GetGroupInfo(string groupName))

- 클라이언트 → 서버: `GetGroupInfo (groupName)`
- 서버 → 요청 클라이언트: `GroupInfo (GroupInfo)`
- 서버 → 요청 클라이언트: `GroupInfo ("Fail: Group '{groupName}' not found")`
  - 그룹을 찾을 수 없는 경우 앞에 "Fail: "을 붙여 전송

#### 그룹 목록 조회 (GetGroupList())

- 클라이언트 → 서버: `GetGroupList ()`
- 서버 → 요청 클라이언트: `GroupList (List<GroupInfo>)`

#### 그룹 멤버 목록 조회 (GetGroupMember(string groupName))

- 클라이언트 → 서버: `GetGroupMember (groupName)`
- 서버 → 요청 클라이언트: `GroupMemberList (List<ClientInfo>)`
- 서버 → 요청 클라이언트: `GroupMemberList ("Fail: Group '{groupName}' not found")`
  - 그룹을 찾을 수 없는 경우 앞에 "Fail: "을 붙여 전송

## 데이터 구조

### ClientInfo
```csharp
public class ClientInfo
{
    public string ClientId { get; set; }
    public string Name { get; set; }
    public DateTime ConnectedAt { get; set; }
    public Dictionary<string, string> Metadata { get; set; }
}
```

### GroupInfo
```csharp
public class GroupInfo
{
    public string GroupId { get; set; }
    public DateTime CreatedAt { get; set; }
    public Dictionary<string, string> Metadata { get; set; }
    public ConcurrentDictionary<string, ClientInfo> Members { get; set; }
}
```