using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using VRTemplate.Networking;

namespace VRTemplate.UI
{
    public class InGameMenuController : MonoBehaviour
    {
        [SerializeField] private XRBaseController leftHandController;
        [SerializeField] private GameObject menuRoot;
        [SerializeField] private TMP_Text currentServerLabel;
        [SerializeField] private TMP_Text leavePromptLabel;
        [SerializeField] private Button leaveServerButton;
        [SerializeField] private InputHelpers.Button yButton = InputHelpers.Button.SecondaryButton;
        [SerializeField] private float inputThreshold = 0.1f;

        private bool _menuOpen;
        private bool _yPressedLastFrame;

        private void OnEnable()
        {
            leaveServerButton.onClick.AddListener(OnLeaveClicked);
            NetworkBootstrap.Instance.OnSessionJoined += UpdateSessionLabel;
            NetworkBootstrap.Instance.OnSessionLeft += ResetLabel;

            if (NetworkBootstrap.Instance.CurrentSession is SessionInfo info)
            {
                UpdateSessionLabel(info);
            }
        }

        private void OnDisable()
        {
            leaveServerButton.onClick.RemoveListener(OnLeaveClicked);

            if (NetworkBootstrap.Instance != null)
            {
                NetworkBootstrap.Instance.OnSessionJoined -= UpdateSessionLabel;
                NetworkBootstrap.Instance.OnSessionLeft -= ResetLabel;
            }
        }

        private void Update()
        {
            if (leftHandController == null)
            {
                return;
            }

            bool yPressed = leftHandController.inputDevice.IsPressed(yButton, out bool pressed, inputThreshold) && pressed;

            if (yPressed && !_yPressedLastFrame)
            {
                ToggleMenu();
            }

            _yPressedLastFrame = yPressed;
        }

        private void ToggleMenu()
        {
            _menuOpen = !_menuOpen;
            menuRoot.SetActive(_menuOpen);

            if (_menuOpen)
            {
                leavePromptLabel.text = "Leave server and return to main menu?";
            }
        }

        private async void OnLeaveClicked()
        {
            await NetworkBootstrap.Instance.LeaveRoomAsync();
            _menuOpen = false;
            menuRoot.SetActive(false);
            // TODO: Load main menu scene.
        }

        private void UpdateSessionLabel(SessionInfo info)
        {
            currentServerLabel.text = info.ToDisplayString();
        }

        private void ResetLabel()
        {
            currentServerLabel.text = "Not in a server";
        }
    }
}
