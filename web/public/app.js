const wsProto = location.protocol === 'https:' ? 'wss:' : 'ws:';
const ws = new WebSocket(`${wsProto}//${location.host}`);

const ui = {
  menu: document.getElementById('menu'),
  status: document.getElementById('status'),
  privateCode: document.getElementById('privateCode'),
  publicRooms: document.getElementById('publicRooms'),
  createPublic: document.getElementById('createPublic'),
  createPrivate: document.getElementById('createPrivate'),
  joinPrivate: document.getElementById('joinPrivate'),
  inGameHud: document.getElementById('inGameHud'),
  serverInfo: document.getElementById('serverInfo'),
  leaveMenu: document.getElementById('leaveMenu'),
  leaveServerInfo: document.getElementById('leaveServerInfo'),
  leaveServer: document.getElementById('leaveServer'),
  closeLeaveMenu: document.getElementById('closeLeaveMenu')
};

const entities = {
  head: document.getElementById('head'),
  leftHand: document.getElementById('leftHand'),
  rightHand: document.getElementById('rightHand'),
  remoteRoot: document.getElementById('remoteRoot')
};

const state = {
  roomName: null,
  isPublic: false,
  privateCode: '',
  players: new Map()
};

function setStatus(msg) {
  ui.status.textContent = msg;
}

function send(data) {
  if (ws.readyState === WebSocket.OPEN) {
    ws.send(JSON.stringify(data));
  }
}

function clearRemotePlayers() {
  for (const p of state.players.values()) {
    p.root.remove();
  }
  state.players.clear();
}

function upsertPlayer(player) {
  if (state.players.has(player.id)) return;

  const root = document.createElement('a-entity');
  root.setAttribute('id', `p-${player.id}`);

  const head = document.createElement('a-sphere');
  head.setAttribute('radius', '0.16');
  head.setAttribute('color', player.color || '#ff4d4d');

  const left = document.createElement('a-sphere');
  left.setAttribute('radius', '0.07');
  left.setAttribute('color', player.color || '#ff4d4d');

  const right = document.createElement('a-sphere');
  right.setAttribute('radius', '0.07');
  right.setAttribute('color', player.color || '#ff4d4d');

  root.appendChild(head);
  root.appendChild(left);
  root.appendChild(right);
  entities.remoteRoot.appendChild(root);

  state.players.set(player.id, { root, head, left, right });
}

function removePlayer(id) {
  const p = state.players.get(id);
  if (!p) return;
  p.root.remove();
  state.players.delete(id);
}

function updatePose(id, pose) {
  const p = state.players.get(id);
  if (!p) return;

  const apply = (el, data) => {
    if (!data) return;
    el.object3D.position.set(data.x, data.y, data.z);
    el.object3D.quaternion.set(data.qx, data.qy, data.qz, data.qw);
  };

  apply(p.head, pose.head);
  apply(p.left, pose.left);
  apply(p.right, pose.right);
}

function renderPublicRooms(rooms) {
  ui.publicRooms.innerHTML = '';
  if (!rooms.length) {
    const none = document.createElement('div');
    none.textContent = 'No public rooms yet.';
    ui.publicRooms.appendChild(none);
    return;
  }

  for (const room of rooms) {
    const btn = document.createElement('button');
    btn.textContent = `${room.name} (${room.playerCount})`;
    btn.onclick = () => send({ type: 'join_room', roomName: room.name });
    ui.publicRooms.appendChild(btn);
  }
}

function getTransform(entity) {
  const p = entity.object3D.position;
  const q = entity.object3D.quaternion;
  return { x: p.x, y: p.y, z: p.z, qx: q.x, qy: q.y, qz: q.z, qw: q.w };
}

setInterval(() => {
  if (!state.roomName) return;
  send({
    type: 'pose',
    head: getTransform(entities.head),
    left: getTransform(entities.leftHand),
    right: getTransform(entities.rightHand)
  });
}, 50);

function openLeaveMenu() {
  if (!state.roomName) return;
  ui.leaveServerInfo.textContent = state.isPublic
    ? `Current server: ${state.roomName} (Public)`
    : `Current server: ${state.roomName} (Private) | Code: ${state.privateCode}`;
  ui.leaveMenu.classList.remove('hidden');
}

function closeLeaveMenu() {
  ui.leaveMenu.classList.add('hidden');
}

window.addEventListener('keydown', (e) => {
  if (e.key.toLowerCase() === 'y') {
    if (ui.leaveMenu.classList.contains('hidden')) openLeaveMenu();
    else closeLeaveMenu();
  }
});

setInterval(() => {
  const pads = navigator.getGamepads?.() || [];
  const yPressed = pads.some((gp) => gp?.buttons?.[3]?.pressed);
  if (yPressed && ui.leaveMenu.classList.contains('hidden')) {
    openLeaveMenu();
  }
}, 100);

ui.createPublic.onclick = () => send({ type: 'create_room', isPublic: true });
ui.createPrivate.onclick = () => send({ type: 'create_room', isPublic: false, code: ui.privateCode.value });
ui.joinPrivate.onclick = () => send({ type: 'join_room', roomName: ui.privateCode.value });
ui.leaveServer.onclick = () => send({ type: 'leave_room' });
ui.closeLeaveMenu.onclick = closeLeaveMenu;

ws.addEventListener('open', () => setStatus('Connected.'));
ws.addEventListener('close', () => setStatus('Disconnected from server.'));

ws.addEventListener('message', (event) => {
  const msg = JSON.parse(event.data);

  if (msg.type === 'error') {
    setStatus(msg.message);
    return;
  }

  if (msg.type === 'public_rooms') {
    renderPublicRooms(msg.rooms);
    return;
  }

  if (msg.type === 'joined_room') {
    state.roomName = msg.roomName;
    state.isPublic = msg.isPublic;
    state.privateCode = msg.privateCode || '';

    clearRemotePlayers();
    msg.players.forEach(upsertPlayer);

    ui.menu.classList.add('hidden');
    ui.inGameHud.classList.remove('hidden');
    ui.serverInfo.textContent = state.isPublic
      ? `Server: ${state.roomName} (Public)`
      : `Server: ${state.roomName} (Private) | Code: ${state.privateCode}`;

    setStatus(`Joined ${state.roomName}`);
    return;
  }

  if (msg.type === 'player_joined') {
    upsertPlayer(msg);
    return;
  }

  if (msg.type === 'player_left') {
    removePlayer(msg.id);
    return;
  }

  if (msg.type === 'left_room') {
    state.roomName = null;
    state.isPublic = false;
    state.privateCode = '';
    clearRemotePlayers();
    ui.menu.classList.remove('hidden');
    ui.inGameHud.classList.add('hidden');
    closeLeaveMenu();
    setStatus('Returned to main menu.');
    return;
  }

  if (msg.type === 'pose') {
    updatePose(msg.id, msg);
  }
});
