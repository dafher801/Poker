// Source: Assets/Scripts/View/CardSpriteProvider.cs
// CardSpriteProvider.cs
// Card 엔티티(Rank, Suit)를 Sprite로 매핑하는 ScriptableObject.
// [CreateAssetMenu]로 에디터에서 생성 가능하며, 52장의 카드 앞면 스프라이트와 1장의 뒷면 스프라이트를 관리한다.
// 인덱스 계산: (int)suit * 13 + ((int)rank - 2)
// Suit 순서: Spade(0), Heart(1), Diamond(2), Club(3)
// Rank 순서: Two(2) ~ Ace(14)

using UnityEngine;
using TexasHoldem.Entity;

namespace TexasHoldem.View
{
    [CreateAssetMenu(fileName = "CardSpriteProvider", menuName = "TexasHoldem/CardSpriteProvider")]
    public class CardSpriteProvider : ScriptableObject
    {
        [Tooltip("카드 뒷면 스프라이트")]
        [SerializeField] private Sprite _cardBack;

        [Tooltip("52장 카드 앞면 스프라이트 배열 (Spade A~K, Heart A~K, Diamond A~K, Club A~K 순서)")]
        [SerializeField] private Sprite[] _cardFronts;

        private const int CardsPerSuit = 13;
        private const int TotalCards = 52;
        private const int RankOffset = 2;

        /// <summary>
        /// 지정한 Rank와 Suit에 해당하는 카드 앞면 스프라이트를 반환한다.
        /// </summary>
        public Sprite GetFrontSprite(Rank rank, Suit suit) { /* ... */ }
        {
            if (_cardFronts == null || _cardFronts.Length < TotalCards)
            {
                Debug.LogWarning("[CardSpriteProvider] cardFronts 배열이 null이거나 길이가 52 미만입니다.");
                return null;
            }

            int index = (int)suit * CardsPerSuit + ((int)rank - RankOffset);
            return _cardFronts[index];
        }

        /// <summary>
        /// 카드 뒷면 스프라이트를 반환한다.
        /// </summary>
        public Sprite GetBackSprite() { /* ... */ }
        {
            return _cardBack;
        }
    }
}
