let connection = null;
let userName = localStorage.getItem('userName') || 'Guest' + (Math.floor(Math.random() * 9000) + 1000);
let currentRoom = '';
let groups = [];
let roomMembers = new Map();

// SignalR 연결 설정
async function startConnection() {
    connection = new signalR.HubConnectionBuilder()
        .withUrl("https://localhost:7049/bridgehub")
        .withAutomaticReconnect()
        .build();

    setupSignalRCallbacks();

    try {
        await connection.start();
        $('#connectionStatus').text('Connected');
        await registerClient();
    } catch (err) {
        console.error(err);
        $('#connectionStatus').text('Connection failed');
        setTimeout(startConnection, 5000);
    }
}

// SignalR 콜백 설정
function setupSignalRCallbacks() {
    connection.on('RequestRegistration', async () => {
        await registerClient();
    });

    connection.on('SuccessRegistered', (clientId, clientInfo) => {
        localStorage.setItem('userName', clientInfo.name);
        userName = clientInfo.name;
        $('#userName').text(clientInfo.name);
    });

    connection.on('GroupCreated', (groupInfo) => {
        console.log("Recieve: GroupCreated");
        groups = [...groups, groupInfo];
        updateRoomList(groups); // 기존 그룹 목록에 새 그룹 추가
    });

    connection.on('GroupRemoved', (groupName) => {
        console.log("Recieve: GroupRemoved");
        groups = groups.filter(group => group.groupId !== groupName);
        updateRoomList(groups);
    });

    connection.on('GroupList', (receivedGroups) => {
        groups = receivedGroups;
        updateRoomList(groups);
    });

    connection.on('GroupMemberList', (members) => {
        updateMemberList(members);
    });

    connection.on('ReceiveGroupMessage', (senderId, message, timestamp) => {
        addMessage(senderId, message, timestamp);
    });

    connection.on('GroupMemberJoined', (clientId, clientInfo) => {
        addMember(clientInfo);
        addSystemMessage(`${clientInfo.name} joined the room`);
    });

    connection.on('GroupMemberLeft', (clientId) => {
        let client = getMember(clientId);
        removeMember(clientId);
        addSystemMessage(`${client.name} left the room`);
    });
}

// 페이지 이탈 핸들러 설정
function setupPageLeaveHandlers() {
    window.onpopstate = async function (e) {
        if (currentRoom) {
            await leaveCurrentRoom();
        }
    }

    window.onbeforeunload = async function (e) {
        if (currentRoom) {
            await leaveCurrentRoom();
        }
    }

    window.addEventListener('beforeunload', async function (e) {
        if (currentRoom) {
            await leaveCurrentRoom();
        }
    });
}

// UI 이벤트 핸들러 설정
function setupUIHandlers() {
    // 이름 변경
    $('#userName').click(() => {
        $('#changeNameModal').modal('show');
    });

    $('#saveName').click(async () => {
        const newName = $('#newUserName').val().trim();
        if (newName) {
            userName = newName;
            await registerClient();
            $('#changeNameModal').modal('hide');
        }
    });

    // 방 생성
    $('#createRoom').click(async () => {
        const roomName = $('#newRoomName').val().trim();
        if (roomName) {
            await connection.invoke('JoinGroup', roomName, null);
            window.location.href = `/Home/Room?roomName=${encodeURIComponent(roomName)}`;
        }
    });

    // 메시지 전송
    $('#sendMessage').click(sendMessage);
    $('#messageInput').keypress((e) => {
        if (e.which === 13) {
            sendMessage();
        }
    });
}

// UI 업데이트 함수들
function updateRoomList(groups) {
    console.log('Updating room list with:', groups);

    const $roomList = $('#roomList');
    $roomList.empty();

    if (!groups || !Array.isArray(groups)) {
        console.error('Invalid groups data:', groups);
        return;
    }

    groups.forEach(group => {
        let groupMemberCount = group.members ? Object.keys(group.members).length : 0;

        $roomList.append(`
            <a href="/Home/Room?roomName=${encodeURIComponent(group.groupId)}" 
               class="list-group-item list-group-item-action d-flex justify-content-between align-items-center">
                ${group.groupId}
                <span class="badge bg-primary rounded-pill">${groupMemberCount}</span>
            </a>
        `);
    });
}

// 클라이언트 등록
async function registerClient() {
    const savedName = userName;
    await connection.invoke('RegisterClient', savedName, null);
}

// 메시지 전송
async function sendMessage() {
    const message = $('#messageInput').val().trim();
    if (message && currentRoom) {
        await connection.invoke('SendToGroup', currentRoom, message);
        $('#messageInput').val('');
    }
}

function updateMemberList(members) {
    const $memberList = $('#memberList');
    $memberList.empty();

    roomMembers.clear();

    members.forEach(member => {
        addMember(member);
    });
}

function addMember(member) {
    if (roomMembers.has(member.clientId)) {
        return;
    }

    const $memberList = $('#memberList');
    $memberList.append(`
        <div class="list-group-item" data-client-id="${member.clientId}">
            ${member.name}
        </div>
    `);

    roomMembers.set(member.clientId, member);
}

function removeMember(clientId) {
    $(`[data-client-id="${clientId}"]`).remove();

    if (roomMembers.has(clientId)) {
        roomMembers.delete(clientId);
    }
}

function getMember(clientId) {
    return roomMembers.get(clientId);
}

function getMemberCount() {
    return roomMembers.size;
}

function addMessage(senderId, message, timestamp) {
    const $messageList = $('#messageList');
    const time = new Date(timestamp).toLocaleTimeString();
    const sender = getMember(senderId);
    const senderName = sender ? sender.name : senderId;

    $messageList.append(`
        <div class="message mb-2 ${senderId === connection.connectionId ? 'text-end' : ''}">
            <small class="text-muted">${senderName}</small>
            <div class="message-content p-2 rounded ${senderId === connection.connectionId ? 'bg-primary text-white' : 'bg-secondary text-white'}">
                ${message}
            </div>
            <small class="text-muted">${time}</small>
        </div>
    `);

    $messageList.scrollTop($messageList[0].scrollHeight);
}

function addSystemMessage(message) {
    const $messageList = $('#messageList');

    $messageList.append(`
        <div class="system-message mb-1 text-center">
            <div class="message-content p-1 rounded bg-light text-light-emphasis small">
                ${message}
            </div>
        </div>
    `);

    $messageList.scrollTop($messageList[0].scrollHeight);
}

async function leaveCurrentRoom() {
    if (connection && connection.state === 'Connected') {
        try {
            await connection.invoke('LeaveGroup', currentRoom);
            currentRoom = '';
        } catch (err) {
            console.error(err);
        }
    }
}

// 초기화
$(document).ready(() => {
    setupUIHandlers();

    async function initialize() {
        await startConnection();

        // 현재 방 이름 가져오기
        const urlParams = new URLSearchParams(window.location.search);
        currentRoom = urlParams.get('roomName');

        if (currentRoom) {
            // 채팅방에 있는 경우
            connection.invoke('JoinGroup', currentRoom, null);
            connection.invoke('GetGroupMember', currentRoom);

            setupPageLeaveHandlers();
        } else {
            // 로비에 있는 경우
            connection.invoke('GetGroupList');
        }
    }

    initialize().catch(console.error);
});