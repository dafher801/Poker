// CommunityCardsView.cs
// 테이블 중앙 커뮤니티 카드 5장 영역을 관리하는 MonoBehaviour.
// DealFlop/DealTurn/DealRiver로 각 페이즈별 카드를 딜하며,
// 코루틴 기반 딜 애니메이션(이동 + 앞면 공개)을 포함한다.
// 초기 상태는 5장 모두 비활성이다.

using System.Collections;
using UnityEngine;
using TexasHoldem.Entity;

namespace TexasHoldem.View
{
    public class CommunityCardsView : MonoBehaviour
    {
        [SerializeField] private CardView[] _communityCards = new CardView[5];
        [SerializeField] private Transform[] _cardSlotPositions = new Transform[5];
        [SerializeField] private float _dealDuration = 0.4f;
        [SerializeField] private float _flopCardDelay = 0.2f;

        /// <summary>
        /// 덱 위치 (화면 밖 좌상단). CardAnimator 구현 전까지 기본값으로 사용한다.
        /// </summary>
        private static readonly Vector3 DeckPosition = new Vector3(-8f, 5f, 0f);

        /// <summary>
        /// index번째 CardView에 카드를 설정하고 딜 애니메이션 후 앞면을 공개한다.
        /// </summary>
        public void DealCard(int index, Card card)
        {
            if (index < 0 || index >= _communityCards.Length)
            {
                return;
            }

            CardView cardView = _communityCards[index];
            if (cardView == null)
            {
                return;
            }

            cardView.SetCard(card.Rank, card.Suit);
            cardView.SetFaceUp(false);
            cardView.gameObject.SetActive(true);

            Vector3 targetPosition = _cardSlotPositions != null && index < _cardSlotPositions.Length && _cardSlotPositions[index] != null
                ? _cardSlotPositions[index].position
                : cardView.transform.position;

            StartCoroutine(DealCoroutine(cardView, targetPosition));
        }

        /// <summary>
        /// 플롭: 0~2번 카드를 연속 딜한다 (각 0.2초 딜레이).
        /// </summary>
        public void DealFlop(Card[] threeCards)
        {
            if (threeCards == null || threeCards.Length < 3)
            {
                return;
            }

            StartCoroutine(DealFlopCoroutine(threeCards));
        }

        /// <summary>
        /// 턴: 3번 카드를 딜한다.
        /// </summary>
        public void DealTurn(Card card)
        {
            DealCard(3, card);
        }

        /// <summary>
        /// 리버: 4번 카드를 딜한다.
        /// </summary>
        public void DealRiver(Card card)
        {
            DealCard(4, card);
        }

        /// <summary>
        /// 5장 모두 ResetCard 후 비활성화한다.
        /// </summary>
        public void ClearAll()
        {
            for (int i = 0; i < _communityCards.Length; i++)
            {
                if (_communityCards[i] != null)
                {
                    _communityCards[i].ResetCard();
                }
            }
        }

        private IEnumerator DealFlopCoroutine(Card[] threeCards)
        {
            for (int i = 0; i < 3; i++)
            {
                DealCard(i, threeCards[i]);
                yield return new WaitForSeconds(_flopCardDelay);
            }
        }

        private IEnumerator DealCoroutine(CardView cardView, Vector3 targetPosition)
        {
            Transform cardTransform = cardView.transform;
            cardTransform.position = DeckPosition;
            cardTransform.localScale = Vector3.zero;

            Vector3 targetScale = Vector3.one;
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

            cardView.FlipWithAnimation(true);
        }
    }
}
