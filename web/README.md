# WebXR Multiplayer Template

This is a browser-playable multiplayer template that supports the requested flow:

- Create **public** server (shows up in server list)
- Create **private** server by code
- Join private server by code
- Join public server from list
- In-game leave menu with current server info
- Leave via **Y button** (with keyboard `Y` fallback)

## Run

```bash
cd web
npm install
npm start
```

Open on Quest browser:

- `http://<your-server-ip>:8080` (same Wi-Fi)
- or deploy to any Node host and open the public HTTPS URL

## Notes

- This is a lightweight template using A-Frame + WebSocket.
- For production, add auth, moderation, persistence, and authoritative movement validation.
