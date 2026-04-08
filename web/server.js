import express from 'express';
import { WebSocketServer } from 'ws';
import http from 'http';
import path from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const app = express();
const server = http.createServer(app);
const wss = new WebSocketServer({ server });

const PORT = process.env.PORT || 8080;

app.use(express.static(path.join(__dirname, 'public')));

const rooms = new Map();
const clients = new Map();

function randomCode() {
  return Math.random().toString(36).slice(2, 8).toUpperCase();
}

function ensureRoom(name, isPublic) {
  if (!rooms.has(name)) {
    rooms.set(name, {
      name,
      isPublic,
      clients: new Set()
    });
  }
  return rooms.get(name);
}

function publicRoomList() {
  return [...rooms.values()]
    .filter((r) => r.isPublic)
    .map((r) => ({ name: r.name, playerCount: r.clients.size }));
}

function broadcastPublicRooms() {
  const msg = JSON.stringify({ type: 'public_rooms', rooms: publicRoomList() });
  for (const ws of clients.keys()) {
    if (ws.readyState === ws.OPEN) {
      ws.send(msg);
    }
  }
}

function roomSnapshot(room) {
  return [...room.clients].map((ws) => clients.get(ws)).filter(Boolean).map((c) => ({
    id: c.id,
    color: c.color
  }));
}

function leaveCurrentRoom(ws) {
  const client = clients.get(ws);
  if (!client || !client.roomName) return;

  const room = rooms.get(client.roomName);
  if (room) {
    room.clients.delete(ws);

    const leaveMsg = JSON.stringify({ type: 'player_left', id: client.id });
    for (const peer of room.clients) {
      if (peer.readyState === peer.OPEN) {
        peer.send(leaveMsg);
      }
    }

    if (room.clients.size === 0) {
      rooms.delete(room.name);
    }
  }

  client.roomName = null;
  broadcastPublicRooms();
}

function joinRoom(ws, roomName, isPublic) {
  const client = clients.get(ws);
  if (!client) return;

  leaveCurrentRoom(ws);

  const room = ensureRoom(roomName, isPublic);
  room.clients.add(ws);
  client.roomName = room.name;

  ws.send(JSON.stringify({
    type: 'joined_room',
    roomName: room.name,
    isPublic: room.isPublic,
    privateCode: room.isPublic ? '' : room.name,
    players: roomSnapshot(room),
    you: { id: client.id, color: client.color }
  }));

  const joinedMsg = JSON.stringify({ type: 'player_joined', id: client.id, color: client.color });
  for (const peer of room.clients) {
    if (peer !== ws && peer.readyState === peer.OPEN) {
      peer.send(joinedMsg);
    }
  }

  broadcastPublicRooms();
}

wss.on('connection', (ws) => {
  const clientId = crypto.randomUUID();
  const color = `hsl(${Math.floor(Math.random() * 360)} 75% 55%)`;
  clients.set(ws, { id: clientId, roomName: null, color });

  ws.send(JSON.stringify({ type: 'hello', id: clientId }));
  ws.send(JSON.stringify({ type: 'public_rooms', rooms: publicRoomList() }));

  ws.on('message', (raw) => {
    let data;
    try {
      data = JSON.parse(raw.toString());
    } catch {
      return;
    }

    if (data.type === 'create_room') {
      const roomName = data.isPublic
        ? `PUBLIC-${randomCode()}`
        : (data.code || '').trim().toUpperCase();

      if (!roomName) {
        ws.send(JSON.stringify({ type: 'error', message: 'Private code is required.' }));
        return;
      }

      joinRoom(ws, roomName, !!data.isPublic);
      return;
    }

    if (data.type === 'join_room') {
      const roomName = (data.roomName || '').trim().toUpperCase();
      if (!roomName || !rooms.has(roomName)) {
        ws.send(JSON.stringify({ type: 'error', message: 'Room not found.' }));
        return;
      }
      const room = rooms.get(roomName);
      joinRoom(ws, room.name, room.isPublic);
      return;
    }

    if (data.type === 'leave_room') {
      leaveCurrentRoom(ws);
      ws.send(JSON.stringify({ type: 'left_room' }));
      return;
    }

    if (data.type === 'pose') {
      const client = clients.get(ws);
      if (!client?.roomName) return;
      const room = rooms.get(client.roomName);
      if (!room) return;

      const msg = JSON.stringify({
        type: 'pose',
        id: client.id,
        head: data.head,
        left: data.left,
        right: data.right
      });

      for (const peer of room.clients) {
        if (peer !== ws && peer.readyState === peer.OPEN) {
          peer.send(msg);
        }
      }
    }
  });

  ws.on('close', () => {
    leaveCurrentRoom(ws);
    clients.delete(ws);
  });
});

server.listen(PORT, () => {
  console.log(`Server listening on http://localhost:${PORT}`);
});
