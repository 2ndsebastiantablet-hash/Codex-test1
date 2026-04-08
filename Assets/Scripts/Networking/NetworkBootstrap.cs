using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

namespace VRTemplate.Networking
{
    public class NetworkBootstrap : MonoBehaviour, INetworkRunnerCallbacks
    {
        public static NetworkBootstrap Instance { get; private set; }

        [SerializeField] private NetworkRunner runnerPrefab;
        [SerializeField] private int maxPlayers = 10;

        public event Action<string> OnStatusChanged;
        public event Action<IReadOnlyList<Fusion.SessionInfo>> OnRoomListChanged;
        public event Action<SessionInfo> OnSessionJoined;
        public event Action OnSessionLeft;

        public SessionInfo? CurrentSession { get; private set; }

        private NetworkRunner _runner;
        private readonly List<Fusion.SessionInfo> _cachedRooms = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public async Task<bool> CreatePublicRoomAsync()
        {
            string roomName = $"PUBLIC-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
            return await StartHostAsync(roomName, isVisible: true, isPrivate: false, privateCode: string.Empty);
        }

        public async Task<bool> CreatePrivateRoomAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                OnStatusChanged?.Invoke("Private code is required.");
                return false;
            }

            string sanitizedCode = code.Trim().ToUpperInvariant();
            return await StartHostAsync(sanitizedCode, isVisible: false, isPrivate: true, privateCode: sanitizedCode);
        }

        public async Task<bool> JoinPrivateRoomAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                OnStatusChanged?.Invoke("Enter a code to join private room.");
                return false;
            }

            string sanitizedCode = code.Trim().ToUpperInvariant();
            return await StartClientAsync(sanitizedCode, isPrivate: true, privateCode: sanitizedCode);
        }

        public async Task<bool> JoinPublicRoomAsync(string roomName)
        {
            if (string.IsNullOrWhiteSpace(roomName))
            {
                OnStatusChanged?.Invoke("Invalid public room name.");
                return false;
            }

            return await StartClientAsync(roomName, isPrivate: false, privateCode: string.Empty);
        }

        public async Task LeaveRoomAsync()
        {
            if (_runner == null)
            {
                return;
            }

            OnStatusChanged?.Invoke("Leaving room...");
            await _runner.Shutdown();

            CurrentSession = null;
            OnSessionLeft?.Invoke();
            OnStatusChanged?.Invoke("Returned to menu.");
        }

        private async Task<bool> StartHostAsync(string roomName, bool isVisible, bool isPrivate, string privateCode)
        {
            EnsureRunner();

            var args = new StartGameArgs
            {
                GameMode = GameMode.Host,
                SessionName = roomName,
                PlayerCount = maxPlayers,
                IsVisible = isVisible,
                IsOpen = true
            };

            OnStatusChanged?.Invoke($"Creating room '{roomName}'...");
            var result = await _runner.StartGame(args);

            if (!result.Ok)
            {
                OnStatusChanged?.Invoke($"Failed to create room: {result.ShutdownReason}");
                return false;
            }

            CurrentSession = new SessionInfo(roomName, isPrivate, privateCode);
            OnSessionJoined?.Invoke(CurrentSession.Value);
            OnStatusChanged?.Invoke("Room created.");
            return true;
        }

        private async Task<bool> StartClientAsync(string roomName, bool isPrivate, string privateCode)
        {
            EnsureRunner();

            var args = new StartGameArgs
            {
                GameMode = GameMode.Client,
                SessionName = roomName
            };

            OnStatusChanged?.Invoke($"Joining '{roomName}'...");
            var result = await _runner.StartGame(args);

            if (!result.Ok)
            {
                OnStatusChanged?.Invoke($"Failed to join room: {result.ShutdownReason}");
                return false;
            }

            CurrentSession = new SessionInfo(roomName, isPrivate, privateCode);
            OnSessionJoined?.Invoke(CurrentSession.Value);
            OnStatusChanged?.Invoke("Joined room.");
            return true;
        }

        private void EnsureRunner()
        {
            if (_runner != null)
            {
                return;
            }

            _runner = Instantiate(runnerPrefab);
            _runner.name = "NetworkRunner";
            _runner.ProvideInput = true;
            _runner.AddCallbacks(this);
            DontDestroyOnLoad(_runner.gameObject);
        }

        public void OnSessionListUpdated(NetworkRunner runner, List<Fusion.SessionInfo> sessionList)
        {
            _cachedRooms.Clear();
            foreach (var room in sessionList)
            {
                if (room.IsVisible)
                {
                    _cachedRooms.Add(room);
                }
            }

            OnRoomListChanged?.Invoke(_cachedRooms);
        }

        public void OnConnectedToServer(NetworkRunner runner) { }
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnInput(NetworkRunner runner, NetworkInput input) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }
        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    }
}
