// Source: Assets/Scripts/View/PlayerSlotView.cs
// PlayerSlotView.cs
// 플레이어 슬롯 하나의 시각적 표현을 담당하는 MonoBehaviour.
// 이름, 칩 스택, 베팅액, 딜러 버튼, Fold/AllIn/Active 상태, 홀카드 2장을 표시한다.
// GameTableView가 이벤트를 수신하여 각 슬롯의 public 메서드를 호출하는 방식으로 사용한다.

using TMPro;
using UnityEngine;
using TexasHoldem.Entity;

namespace TexasHoldem.View
{
    public class PlayerSlotView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _nameLabel;
        [SerializeField] private TextMeshProUGUI _chipStackLabel;
        [SerializeField] private TextMeshProUGUI _betAmountLabel;
        [SerializeField] private GameObject _dealerButtonIcon;
        [SerializeField] private GameObject _foldOverlay;
        [SerializeField] private GameObject _allInBadge;
        [SerializeField] private CardView[] _holeCards;

        private int _seatIndex;
        private bool _isOccupied;

        public int SeatIndex => /* ... */;
        public CardView[] HoleCards => /* ... */;

        /// <summary>
        /// 좌석 인덱스를 설정하고 슬롯을 빈 초기 상태로 리셋한다.
        /// </summary>
        public void Initialize(int seatIndex) { /* ... */ }
        {
            _seatIndex = seatIndex;
            ClearSlot();
        }

        /// <summary>
        /// 플레이어 이름, 칩 스택, 딜러 버튼 표시를 갱신한다.
        /// </summary>
        public void UpdatePlayerInfo(string name, int chipStack, bool isDealer) { /* ... */ }
        {
            _isOccupied = true;

            if (_nameLabel != null)
            {
                _nameLabel.text = name;
                _nameLabel.gameObject.SetActive(true);
            }

            if (_chipStackLabel != null)
            {
                _chipStackLabel.text = chipStack.ToString();
                _chipStackLabel.gameObject.SetActive(true);
            }

            if (_dealerButtonIcon != null)
            {
                _dealerButtonIcon.SetActive(isDealer);
            }
        }

        /// <summary>
        /// 베팅액 텍스트를 갱신한다. 0이면 숨긴다.
        /// </summary>
        public void UpdateBetAmount(int amount) { /* ... */ }
        {
            if (_betAmountLabel == null) return;

            if (amount > 0)
            {
                _betAmountLabel.text = amount.ToString();
                _betAmountLabel.gameObject.SetActive(true);
            }
            else
            {
                _betAmountLabel.text = "";
                _betAmountLabel.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Fold 오버레이, AllIn 배지, 활성 하이라이트 상태를 토글한다.
        /// </summary>
        public void UpdateStatus(bool isFolded, bool isAllIn, bool isActive) { /* ... */ }
        {
            if (_foldOverlay != null)
            {
                _foldOverlay.SetActive(isFolded);
            }

            if (_allInBadge != null)
            {
                _allInBadge.SetActive(isAllIn);
            }
        }

        /// <summary>
        /// 홀카드에 카드를 설정하고 표시한다.
        /// faceUp이 true이면 앞면, false이면 뒷면으로 표시한다.
        /// </summary>
        public void ShowHoleCards(Card[] cards, bool faceUp) { /* ... */ }
        {
            if (_holeCards == null || cards == null) return;

            int count = Mathf.Min(cards.Length, _holeCards.Length);
            for (int i = 0; i < count; i++)
            {
                _holeCards[i].gameObject.SetActive(true);
                _holeCards[i].SetCard(cards[i].Rank, cards[i].Suit);
                _holeCards[i].SetFaceUp(faceUp);
            }
        }

        /// <summary>
        /// 뒷면이던 홀카드를 FlipWithAnimation으로 앞면 공개한다.
        /// </summary>
        public void RevealHoleCards() { /* ... */ }
        {
            if (_holeCards == null) return;

            for (int i = 0; i < _holeCards.Length; i++)
            {
                if (_holeCards[i].HasCard && !_holeCards[i].IsFaceUp)
                {
                    _holeCards[i].FlipWithAnimation(true);
                }
            }
        }

        /// <summary>
        /// 슬롯을 빈 상태로 리셋한다.
        /// </summary>
        public void ClearSlot() { /* ... */ }
        {
            _isOccupied = false;

            if (_nameLabel != null)
            {
                _nameLabel.text = "";
                _nameLabel.gameObject.SetActive(false);
            }

            if (_chipStackLabel != null)
            {
                _chipStackLabel.text = "";
                _chipStackLabel.gameObject.SetActive(false);
            }

            if (_betAmountLabel != null)
            {
                _betAmountLabel.text = "";
                _betAmountLabel.gameObject.SetActive(false);
            }

            if (_dealerButtonIcon != null)
            {
                _dealerButtonIcon.SetActive(false);
            }

            if (_foldOverlay != null)
            {
                _foldOverlay.SetActive(false);
            }

            if (_allInBadge != null)
            {
                _allInBadge.SetActive(false);
            }

            if (_holeCards != null)
            {
                for (int i = 0; i < _holeCards.Length; i++)
                {
                    _holeCards[i].ResetCard();
                }
            }
        }
    }
}
