// Deck.cs
// 52장의 카드로 구성된 덱 엔티티.
// 생성 시 모든 Suit × Rank 조합으로 52장의 고유한 카드를 초기화한다.
// Shuffle(IRandomSource)로 외부 셔플 전략에 셔플을 위임하고,
// Draw()로 맨 위 카드를 꺼낸다.

using System;
using System.Collections.Generic;
using TexasHoldem.Gateway;

namespace TexasHoldem.Entity
{
    public class Deck
    {
        private readonly List<Card> _cards;

        public int Remaining => _cards.Count;

        public Deck()
        {
            _cards = new List<Card>(52);

            foreach (Suit suit in Enum.GetValues(typeof(Suit)))
            {
                foreach (Rank rank in Enum.GetValues(typeof(Rank)))
                {
                    _cards.Add(new Card(suit, rank));
                }
            }

            ValidateNoDuplicates();
        }

        public void Shuffle(IRandomSource randomSource)
        {
            randomSource.Shuffle(_cards);
        }

        public Card Draw()
        {
            if (_cards.Count == 0)
                throw new InvalidOperationException("덱에 남은 카드가 없습니다.");

            Card top = _cards[_cards.Count - 1];
            _cards.RemoveAt(_cards.Count - 1);
            return top;
        }

        private void ValidateNoDuplicates()
        {
            var seen = new HashSet<Card>();
            foreach (Card card in _cards)
            {
                if (!seen.Add(card))
                    throw new InvalidOperationException($"덱에 중복 카드가 있습니다: {card}");
            }
        }
    }
}
