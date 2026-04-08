# VR Multiplayer Template (Gorilla-Tag-Style Lobby Flow)

This repository now contains a **basic Unity + Photon Fusion multiplayer template** for a VR game flow inspired by Gorilla Tag's room model:

- Join by private code
- Create private room by code
- Create public room and show in a public list
- Join from public room list
- In-game Y-button menu to leave room
- Display current room name and private code (when applicable)

## Included scripts

- `Assets/Scripts/Networking/NetworkBootstrap.cs`
  - Owns Photon Fusion startup/shutdown, room creation/joining, and room-list refresh.
- `Assets/Scripts/Networking/SessionInfo.cs`
  - Lightweight model for current room/session data.
- `Assets/Scripts/UI/MainMenuUIController.cs`
  - Main-menu flow: create public/private rooms, join by code, join public from list.
- `Assets/Scripts/UI/InGameMenuController.cs`
  - In-game Y-button menu that shows current server info and lets the player leave.

## Engine / package assumptions

- Unity 2022 LTS+ (or newer)
- Photon Fusion 2 package installed
- TextMeshPro package installed
- XR Interaction Toolkit + Input System for VR input

## Scene wiring overview

1. Add a `NetworkRunner` prefab (or let `NetworkBootstrap` create one from `runnerPrefab`).
2. Place `NetworkBootstrap` in your bootstrap scene and assign:
   - `runnerPrefab`
   - `maxPlayers`
3. Create a main menu canvas and wire `MainMenuUIController` fields:
   - Buttons: create public/private, join private, refresh, start public quick-join
   - Inputs: private code
   - Room list root + row prefab
   - Status text
4. Create in-game menu canvas and wire `InGameMenuController` fields:
   - `menuRoot`
   - `currentServerLabel`
   - `leavePromptLabel`
   - `leftHandController` (XRBaseController)
5. Hook scene transitions where marked TODO in scripts.

## Behavior details

- **Create Public**: generates a room name and marks it visible/open, then starts host.
- **Create Private**: uses player-entered code, marks room hidden (not in public list), then starts host.
- **Join by Code**: joins a hidden/private room directly by name.
- **Join Public**: joins selected room from Fusion's session list.
- **In-game Y button**: toggles leave menu; choosing Leave disconnects and returns to main menu.

## Notes

- This is a **template foundation**; you still need to integrate your VR player rig/networked avatar prefab.
- Add server-side validation for production (name/code rules, rate limits, anti-cheat movement checks).

## Web Link Option (Quest Browser)

If you need to play directly from your Oculus/Quest browser without Unity tooling, use the new `web/` template:

- See `web/README.md` for run/deploy steps.
- It implements the same lobby contract (public/private/create/join/leave/Y button menu) in WebXR.
