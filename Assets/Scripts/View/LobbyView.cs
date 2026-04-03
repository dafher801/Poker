// LobbyView.cs
// 로비 UI를 구성하는 MonoBehaviour View.
// 참가 인원(2~10) Slider, 시작 칩 InputField, 스몰/빅 블라인드 InputField,
// '게임 시작' Button, 에러 메시지 Text로 구성된다.
// LobbyDirector에 대한 참조를 외부에서 주입받아(Initialize),
// 버튼 클릭 시 RequestStartGame을 호출하고,
// OnValidationError / OnLobbyActivated 이벤트를 구독하여 UI를 갱신한다.

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Poker.Director;

namespace TexasHoldem.View
{
    public class LobbyView : MonoBehaviour
    {
        [Header("Player Count")]
        [SerializeField] private Slider _playerCountSlider;
        [SerializeField] private TMP_Text _playerCountLabel;

        [Header("Starting Chips")]
        [SerializeField] private TMP_InputField _startingChipsInput;

        [Header("Blinds")]
        [SerializeField] private TMP_InputField _smallBlindInput;
        [SerializeField] private TMP_InputField _bigBlindInput;

        [Header("Buttons")]
        [SerializeField] private Button _startGameButton;

        [Header("Error")]
        [SerializeField] private TMP_Text _errorText;

        private LobbyDirector _lobbyDirector;

        private const int DefaultStartingChips = 1000;
        private const int DefaultSmallBlind = 10;
        private const int DefaultBigBlind = 20;
        private const int MinPlayers = 2;
        private const int MaxPlayers = 10;

        /// <summary>
        /// LobbyDirector 참조를 주입받고 이벤트를 구독한다.
        /// GameSessionBootstrapper 등 외부에서 호출한다.
        /// </summary>
        public void Initialize(LobbyDirector lobbyDirector)
        {
            _lobbyDirector = lobbyDirector;

            _lobbyDirector.OnValidationError += HandleValidationError;
            _lobbyDirector.OnLobbyActivated += HandleLobbyActivated;
        }

        private void Awake()
        {
            SetupSlider();
            SetupInputFields();
            HideError();

            _startGameButton.onClick.AddListener(HandleStartGameClicked);
            _playerCountSlider.onValueChanged.AddListener(HandlePlayerCountChanged);
        }

        private void OnDestroy()
        {
            if (_lobbyDirector != null)
            {
                _lobbyDirector.OnValidationError -= HandleValidationError;
                _lobbyDirector.OnLobbyActivated -= HandleLobbyActivated;
            }

            _startGameButton.onClick.RemoveListener(HandleStartGameClicked);
            _playerCountSlider.onValueChanged.RemoveListener(HandlePlayerCountChanged);
        }

        private void SetupSlider()
        {
            _playerCountSlider.minValue = MinPlayers;
            _playerCountSlider.maxValue = MaxPlayers;
            _playerCountSlider.wholeNumbers = true;
            _playerCountSlider.value = MinPlayers;
            UpdatePlayerCountLabel(MinPlayers);
        }

        private void SetupInputFields()
        {
            _startingChipsInput.contentType = TMP_InputField.ContentType.IntegerNumber;
            _smallBlindInput.contentType = TMP_InputField.ContentType.IntegerNumber;
            _bigBlindInput.contentType = TMP_InputField.ContentType.IntegerNumber;

            _startingChipsInput.text = DefaultStartingChips.ToString();
            _smallBlindInput.text = DefaultSmallBlind.ToString();
            _bigBlindInput.text = DefaultBigBlind.ToString();
        }

        private void HandlePlayerCountChanged(float value)
        {
            UpdatePlayerCountLabel((int)value);
            HideError();
        }

        private void UpdatePlayerCountLabel(int count)
        {
            _playerCountLabel.text = $"{count}";
        }

        private void HandleStartGameClicked()
        {
            HideError();

            int playerCount = (int)_playerCountSlider.value;

            if (!int.TryParse(_startingChipsInput.text, out int startingChips))
            {
                ShowError("시작 칩은 유효한 숫자를 입력해주세요.");
                return;
            }

            if (!int.TryParse(_smallBlindInput.text, out int smallBlind))
            {
                ShowError("스몰 블라인드는 유효한 숫자를 입력해주세요.");
                return;
            }

            if (!int.TryParse(_bigBlindInput.text, out int bigBlind))
            {
                ShowError("빅 블라인드는 유효한 숫자를 입력해주세요.");
                return;
            }

            _lobbyDirector.RequestStartGame(playerCount, startingChips, smallBlind, bigBlind);
        }

        private void HandleValidationError(string message)
        {
            ShowError(message);
        }

        private void HandleLobbyActivated()
        {
            gameObject.SetActive(true);
            HideError();
        }

        private void ShowError(string message)
        {
            _errorText.gameObject.SetActive(true);
            _errorText.text = message;
        }

        private void HideError()
        {
            _errorText.gameObject.SetActive(false);
            _errorText.text = string.Empty;
        }
    }
}
