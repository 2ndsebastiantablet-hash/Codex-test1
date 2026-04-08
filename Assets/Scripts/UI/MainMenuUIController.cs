using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRTemplate.Networking;

namespace VRTemplate.UI
{
    public class MainMenuUIController : MonoBehaviour
    {
        [Header("Inputs")]
        [SerializeField] private TMP_InputField privateCodeInput;

        [Header("Buttons")]
        [SerializeField] private Button createPublicButton;
        [SerializeField] private Button createPrivateButton;
        [SerializeField] private Button joinPrivateButton;
        [SerializeField] private Button refreshPublicListButton;

        [Header("Public List")]
        [SerializeField] private Transform publicRoomListRoot;
        [SerializeField] private Button publicRoomRowPrefab;

        [Header("Status")]
        [SerializeField] private TMP_Text statusLabel;

        private readonly List<Button> _spawnedRows = new();

        private void OnEnable()
        {
            var net = NetworkBootstrap.Instance;
            net.OnStatusChanged += UpdateStatus;
            net.OnRoomListChanged += RebuildPublicRoomList;

            createPublicButton.onClick.AddListener(OnCreatePublicClicked);
            createPrivateButton.onClick.AddListener(OnCreatePrivateClicked);
            joinPrivateButton.onClick.AddListener(OnJoinPrivateClicked);
            refreshPublicListButton.onClick.AddListener(OnRefreshClicked);
        }

        private void OnDisable()
        {
            if (NetworkBootstrap.Instance != null)
            {
                NetworkBootstrap.Instance.OnStatusChanged -= UpdateStatus;
                NetworkBootstrap.Instance.OnRoomListChanged -= RebuildPublicRoomList;
            }

            createPublicButton.onClick.RemoveListener(OnCreatePublicClicked);
            createPrivateButton.onClick.RemoveListener(OnCreatePrivateClicked);
            joinPrivateButton.onClick.RemoveListener(OnJoinPrivateClicked);
            refreshPublicListButton.onClick.RemoveListener(OnRefreshClicked);
        }

        private async void OnCreatePublicClicked()
        {
            bool ok = await NetworkBootstrap.Instance.CreatePublicRoomAsync();
            if (ok)
            {
                // TODO: Load gameplay scene.
            }
        }

        private async void OnCreatePrivateClicked()
        {
            bool ok = await NetworkBootstrap.Instance.CreatePrivateRoomAsync(privateCodeInput.text);
            if (ok)
            {
                // TODO: Load gameplay scene.
            }
        }

        private async void OnJoinPrivateClicked()
        {
            bool ok = await NetworkBootstrap.Instance.JoinPrivateRoomAsync(privateCodeInput.text);
            if (ok)
            {
                // TODO: Load gameplay scene.
            }
        }

        private void OnRefreshClicked()
        {
            UpdateStatus("Refreshing public rooms... (Fusion auto updates list)");
        }

        private void RebuildPublicRoomList(IReadOnlyList<Fusion.SessionInfo> rooms)
        {
            foreach (var row in _spawnedRows)
            {
                Destroy(row.gameObject);
            }
            _spawnedRows.Clear();

            foreach (var room in rooms)
            {
                var row = Instantiate(publicRoomRowPrefab, publicRoomListRoot);
                _spawnedRows.Add(row);

                var label = row.GetComponentInChildren<TMP_Text>();
                if (label != null)
                {
                    label.text = $"{room.Name} ({room.PlayerCount}/{room.MaxPlayers})";
                }

                string roomName = room.Name;
                row.onClick.AddListener(async () =>
                {
                    bool ok = await NetworkBootstrap.Instance.JoinPublicRoomAsync(roomName);
                    if (ok)
                    {
                        // TODO: Load gameplay scene.
                    }
                });
            }

            UpdateStatus($"Public rooms found: {rooms.Count}");
        }

        private void UpdateStatus(string message)
        {
            if (statusLabel != null)
            {
                statusLabel.text = message;
            }
        }
    }
}
