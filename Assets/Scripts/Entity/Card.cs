// Card.cs
// 카드 한 장을 나타내는 불변 값 객체.
// Suit와 Rank를 조합하여 고유한 카드를 표현한다.
// IEquatable<Card>를 구현하여 동등성 비교를 지원하고,
// ToString()으로 'Ace of Spades'와 같은 가독성 있는 문자열을 반환한다.

using System;

namespace TexasHoldem.Entity
{
    public readonly struct Card : IEquatable<Card>
    {
        public Suit Suit { get; }
        public Rank Rank { get; }

        public Card(Suit suit, Rank rank)
        {
            Suit = suit;
            Rank = rank;
        }

        public bool Equals(Card other)
        {
            return Suit == other.Suit && Rank == other.Rank;
        }

        public override bool Equals(object obj)
        {
            return obj is Card other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Suit, Rank);
        }

        public static bool operator ==(Card left, Card right) => left.Equals(right);
        public static bool operator !=(Card left, Card right) => !left.Equals(right);

        public override string ToString()
        {
            return $"{Rank} of {Suit}s";
        }
    }
}
