// Source: Assets/Scripts/View/HoleCardsView.cs
// HoleCardsView.cs
// 로컬 플레이어와 상대 플레이어의 홀카드 표시 로직을 통합 관리하는 MonoBehaviour.
// 로컬 플레이어의 홀카드는 앞면으로, 상대 플레이어의 홀카드는 뒷면으로 표시한다.
// 쇼다운 시 RevealForShowdown으로 상대 홀카드를 공개할 수 있다.
// PlayerSlotView[] 참조는 GameTableView로부터 주입받는다.

using System.Collections;
using UnityEngine;
using TexasHoldem.Entity;

namespace TexasHoldem.View
{
    public class HoleCardsView : MonoBehaviour
    {
        private PlayerSlotView[] _playerSlots;
        private int _localSeatIndex;

        /// <summary>
        /// 덱 위치 (화면 밖 좌상단). CardAnimator 구현 전까지 기본값으로 사용한다.
        /// </summary>
        private static readonly Vector3 DeckPosition = new Vector3(-8f, 5f, 0f);

        [SerializeField] private float _dealDuration = 0.4f;
        [SerializeField] private float _cardDelay = 0.15f;

        /// <summary>
        /// PlayerSlotView 배열을 외부(GameTableView)로부터 주입받는다.
        /// </summary>
        public void SetPlayerSlots(PlayerSlotView[] playerSlots) { /* ... */ }
        {
            _playerSlots = playerSlots;
        }

        /// <summary>
        /// 로컬 플레이어 좌석 인덱스를 지정한다.
        /// </summary>
        public void SetLocalSeat(int seatIndex) { /* ... */ }
        {
            _localSeatIndex = seatIndex;
        }

        /// <summary>
        /// 해당 좌석에 홀카드를 딜한다.
        /// 로컬 플레이어(localSeatIndex)이면 앞면, 아니면 뒷면으로 표시한다.
        /// 딜 애니메이션을 적용한다.
        /// </summary>
        public void DealHoleCards(int seatIndex, Card[] cards, bool isFaceUp) { /* ... */ }
        {
            if (_playerSlots == null || seatIndex < 0 || seatIndex >= _playerSlots.Length)
            {
                return;
            }

            PlayerSlotView slot = _playerSlots[seatIndex];
            if (slot == null || cards == null)
            {
                return;
            }

            bool faceUp = seatIndex == _localSeatIndex || isFaceUp;
            slot.ShowHoleCards(cards, faceUp);

            StartCoroutine(DealAnimationCoroutine(slot, faceUp));
        }

        /// <summary>
        /// 쇼다운 시 해당 좌석의 홀카드를 실제 카드로 설정 후 플립 애니메이션으로 공개한다.
        /// </summary>
        public void RevealForShowdown(int seatIndex, Card[] cards) { /* ... */ }
        {
            if (_playerSlots == null || seatIndex < 0 || seatIndex >= _playerSlots.Length)
            {
                return;
            }

            PlayerSlotView slot = _playerSlots[seatIndex];
            if (slot == null || cards == null)
            {
                return;
            }

            // 실제 카드 데이터를 설정 (뒷면 상태로)
            slot.ShowHoleCards(cards, false);
            // 플립 애니메이션으로 앞면 공개
            slot.RevealHoleCards();
        }

        /// <summary>
        /// 모든 좌석의 홀카드를 초기화한다.
        /// </summary>
        public void ClearAllHoleCards() { /* ... */ }
        {
            if (_playerSlots == null)
            {
                return;
            }

            for (int i = 0; i < _playerSlots.Length; i++)
            {
                if (_playerSlots[i] == null)
                {
                    continue;
                }

                CardView[] holeCards = _playerSlots[i].HoleCards;
                if (holeCards == null)
                {
                    continue;
                }

                for (int j = 0; j < holeCards.Length; j++)
                {
                    if (holeCards[j] != null)
                    {
                        holeCards[j].ResetCard();
                    }
                }
            }
        }

        /// <summary>
        /// 홀카드 딜 애니메이션 코루틴. 각 카드를 덱 위치에서 슬롯 위치로 이동시킨다.
        /// </summary>
        private IEnumerator DealAnimationCoroutine(PlayerSlotView slot, bool faceUp) { /* ... */ }
        {
            CardView[] holeCards = slot.HoleCards;
            if (holeCards == null)
            {
                yield break;
            }

            for (int i = 0; i < holeCards.Length; i++)
            {
                if (holeCards[i] == null || !holeCards[i].gameObject.activeSelf)
                {
                    continue;
                }

                StartCoroutine(AnimateSingleCard(holeCards[i], faceUp));
                yield return new WaitForSeconds(_cardDelay);
            }
        }

        /// <summary>
        /// 카드 한 장의 딜 애니메이션. 덱 위치에서 목표 위치로 이동 + 스케일 팝업.
        /// </summary>
        private IEnumerator AnimateSingleCard(CardView cardView, bool faceUp) { /* ... */ }
        {
            Transform cardTransform = cardView.transform;
            Vector3 targetPosition = cardTransform.position;
            Vector3 targetScale = cardTransform.localScale;

            cardTransform.position = DeckPosition;
            cardTransform.localScale = Vector3.zero;

            // 애니메이션 시작 전 뒷면으로 설정
            cardView.SetFaceUp(false);

            float elapsed = 0f;
            while (elapsed < _dealDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _dealDuration);
                float smoothT = t * t * (3f - 2f * t);

                cardTransform.position = Vector3.Lerp(DeckPosition, targetPosition, smoothT);
                cardTransform.localScale = Vector3.Lerp(Vector3.zero, targetScale, smoothT);
                yield return null;
            }

            cardTransform.position = targetPosition;
            cardTransform.localScale = targetScale;

            // 앞면이어야 하면 플립 애니메이션으로 공개
            if (faceUp)
            {
                cardView.FlipWithAnimation(true);
            }
        }
    }
}
