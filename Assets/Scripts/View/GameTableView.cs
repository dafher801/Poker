// GameTableView.cs
// 게임 테이블 전체 View의 루트 오브젝트이자 이벤트 구독 허브.
// IGameEventBroadcaster의 모든 View 이벤트를 구독하고,
// 수신한 이벤트 데이터를 해당 하위 View(PlayerSlotView, CommunityCardsView,
// HoleCardsView, PotDisplayView)의 public 메서드로 라우팅한다.
// Director로부터 IGameEventBroadcaster 참조를 주입받아 사용한다.

using System.Linq;
using UnityEngine;
using TexasHoldem.Director;
using TexasHoldem.Entity;

namespace TexasHoldem.View
{
    public class GameTableView : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField] private TableLayoutManager _layoutManager;

        [Header("Sub Views")]
        [SerializeField] private PlayerSlotView[] _playerSlots = new PlayerSlotView[10];
        [SerializeField] private CommunityCardsView _communityCards;
        [SerializeField] private HoleCardsView _holeCards;
        [SerializeField] private PotDisplayView _potDisplay;

        [Header("Chip Animation")]
        [Tooltip("칩 아이콘 프리팹 (ChipAnimator용)")]
        [SerializeField] private GameObject _chipIconPrefab;

        private IGameEventBroadcaster _broadcaster;

        /// <summary>
        /// IGameEventBroadcaster 참조를 외부(Director)로부터 주입받는다.
        /// </summary>
        public void SetBroadcaster(IGameEventBroadcaster broadcaster)
        {
            if (_broadcaster != null)
            {
                UnsubscribeEvents();
            }

            _broadcaster = broadcaster;

            if (_broadcaster != null && enabled)
            {
                SubscribeEvents();
            }
        }

        /// <summary>
        /// PlayerSlotView 배열을 반환한다. HoleCardsView 등 하위 View 주입용.
        /// </summary>
        public PlayerSlotView[] PlayerSlots => _playerSlots;

        private void OnEnable()
        {
            if (_broadcaster != null)
            {
                SubscribeEvents();
            }

            if (_holeCards != null && _playerSlots != null)
            {
                _holeCards.SetPlayerSlots(_playerSlots);
            }
        }

        private void OnDisable()
        {
            if (_broadcaster != null)
            {
                UnsubscribeEvents();
            }
        }

        private void SubscribeEvents()
        {
            _broadcaster.Subscribe<PlayerSeatUpdatedEvent>(HandlePlayerSeatUpdated);
            _broadcaster.Subscribe<HoleCardsDealtEvent>(HandleHoleCardsDealt);
            _broadcaster.Subscribe<CommunityCardDealtEvent>(HandleCommunityCardDealt);
            _broadcaster.Subscribe<PlayerActionDisplayEvent>(HandlePlayerActionDisplay);
            _broadcaster.Subscribe<ViewPotUpdatedEvent>(HandlePotUpdated);
            _broadcaster.Subscribe<ShowdownRevealEvent>(HandleShowdownReveal);
            _broadcaster.Subscribe<RoundResultEvent>(HandleRoundResult);
        }

        private void UnsubscribeEvents()
        {
            _broadcaster.Unsubscribe<PlayerSeatUpdatedEvent>(HandlePlayerSeatUpdated);
            _broadcaster.Unsubscribe<HoleCardsDealtEvent>(HandleHoleCardsDealt);
            _broadcaster.Unsubscribe<CommunityCardDealtEvent>(HandleCommunityCardDealt);
            _broadcaster.Unsubscribe<PlayerActionDisplayEvent>(HandlePlayerActionDisplay);
            _broadcaster.Unsubscribe<ViewPotUpdatedEvent>(HandlePotUpdated);
            _broadcaster.Unsubscribe<ShowdownRevealEvent>(HandleShowdownReveal);
            _broadcaster.Unsubscribe<RoundResultEvent>(HandleRoundResult);
        }

        private void HandlePlayerSeatUpdated(PlayerSeatUpdatedEvent e)
        {
            if (!IsValidSeatIndex(e.SeatIndex)) return;

            PlayerSlotView slot = _playerSlots[e.SeatIndex];
            slot.UpdatePlayerInfo(e.PlayerName, e.ChipStack, e.IsDealer);
            slot.UpdateStatus(e.IsFolded, e.IsAllIn, e.IsActive);
        }

        private void HandleHoleCardsDealt(HoleCardsDealtEvent e)
        {
            if (_holeCards == null) return;

            Card[] cards = e.Cards.ToArray();
            _holeCards.DealHoleCards(e.SeatIndex, cards, e.IsFaceUp);
        }

        private void HandleCommunityCardDealt(CommunityCardDealtEvent e)
        {
            if (_communityCards == null) return;

            _communityCards.DealCard(e.CardIndex, e.Card);
        }

        private void HandlePlayerActionDisplay(PlayerActionDisplayEvent e)
        {
            if (!IsValidSeatIndex(e.SeatIndex)) return;

            _playerSlots[e.SeatIndex].UpdateBetAmount(e.BetAmount);
        }

        private void HandlePotUpdated(ViewPotUpdatedEvent e)
        {
            if (_potDisplay == null) return;

            int[] sidePots = e.SidePots.ToArray();
            _potDisplay.UpdatePot(e.MainPot, sidePots);
        }

        private void HandleShowdownReveal(ShowdownRevealEvent e)
        {
            if (_holeCards == null) return;

            Card[] cards = e.Cards.ToArray();
            _holeCards.RevealForShowdown(e.SeatIndex, cards);
        }

        private void HandleRoundResult(RoundResultEvent e)
        {
            if (e.WinningSeatIndices == null || e.Amounts == null) return;

            Vector3 potPosition = _layoutManager != null
                ? _layoutManager.transform.position
                : transform.position;

            for (int i = 0; i < e.WinningSeatIndices.Count; i++)
            {
                int winnerSeat = e.WinningSeatIndices[i];
                if (!IsValidSeatIndex(winnerSeat)) continue;

                // 칩 아이콘을 생성하여 팟에서 승자 위치로 이동 애니메이션
                if (_chipIconPrefab != null && _layoutManager != null)
                {
                    GameObject chipObj = Instantiate(_chipIconPrefab, potPosition, Quaternion.identity, transform);
                    Vector3 winnerPosition = _layoutManager.GetSeatPosition(winnerSeat);

                    ChipAnimator.AnimateChipToPlayer(this, chipObj.transform, potPosition, winnerPosition);
                }
            }
        }

        private bool IsValidSeatIndex(int seatIndex)
        {
            return _playerSlots != null && seatIndex >= 0 && seatIndex < _playerSlots.Length && _playerSlots[seatIndex] != null;
        }
    }
}
