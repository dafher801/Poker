// Source: Assets/Scripts/View/CardView.cs
// CardView.cs
// 카드 한 장의 시각적 표현을 담당하는 MonoBehaviour.
// SpriteRenderer 기반으로 카드 앞면/뒷면을 표시하며,
// SetCard로 카드 데이터를 설정하고, SetFaceUp/FlipWithAnimation으로 앞뒤 전환을 제어한다.
// 초기 상태는 비활성(GameObject.SetActive(false))이다.

using UnityEngine;
using TexasHoldem.Entity;

namespace TexasHoldem.View
{
    public class CardView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _frontRenderer;
        [SerializeField] private SpriteRenderer _backRenderer;
        [SerializeField] private CardSpriteProvider _spriteProvider;

        private Rank _rank;
        private Suit _suit;
        private bool _isFaceUp;
        private bool _hasCard;
        private Coroutine _flipCoroutine;

        /// <summary>
        /// 카드 데이터를 설정한다. spriteProvider로 앞면 스프라이트를 지정하고 내부에 Rank/Suit를 저장한다.
        /// </summary>
        public void SetCard(Rank rank, Suit suit) { /* ... */ }
        {
            _rank = rank;
            _suit = suit;
            _hasCard = true;

            if (_spriteProvider != null && _frontRenderer != null)
            {
                _frontRenderer.sprite = _spriteProvider.GetFrontSprite(rank, suit);
            }

            if (_spriteProvider != null && _backRenderer != null)
            {
                _backRenderer.sprite = _spriteProvider.GetBackSprite();
            }
        }

        /// <summary>
        /// isFaceUp 상태에 따라 앞면/뒷면 활성화를 즉시 전환한다.
        /// </summary>
        public void SetFaceUp(bool faceUp) { /* ... */ }
        {
            _isFaceUp = faceUp;

            if (_frontRenderer != null)
            {
                _frontRenderer.enabled = faceUp;
            }

            if (_backRenderer != null)
            {
                _backRenderer.enabled = !faceUp;
            }
        }

        /// <summary>
        /// 뒤집기 애니메이션을 실행한다. CardAnimator에 위임하여 X축 스케일 기반으로 뒤집기를 수행한다.
        /// 중간 지점에서 SetFaceUp을 호출하여 스프라이트를 교체한다.
        /// </summary>
        public void FlipWithAnimation(bool toFaceUp, float duration = 0.3f) { /* ... */ }
        {
            if (_flipCoroutine != null)
            {
                StopCoroutine(_flipCoroutine);
            }

            _flipCoroutine = CardAnimator.AnimateFlip(
                this,
                transform,
                () => /* ... */;
                duration,
                () => /* ... */;
            );
        }

        /// <summary>
        /// 카드를 비활성 상태로 초기화한다.
        /// </summary>
        public void ResetCard() { /* ... */ }
        {
            _hasCard = false;
            _isFaceUp = false;

            if (_frontRenderer != null)
            {
                _frontRenderer.sprite = null;
                _frontRenderer.enabled = false;
            }

            if (_backRenderer != null)
            {
                _backRenderer.enabled = false;
            }

            if (_flipCoroutine != null)
            {
                StopCoroutine(_flipCoroutine);
                _flipCoroutine = null;
            }

            gameObject.SetActive(false);
        }

        public bool IsFaceUp => /* ... */;
        public bool HasCard => /* ... */;
        public Rank Rank => /* ... */;
        public Suit Suit => /* ... */;

    }
}
