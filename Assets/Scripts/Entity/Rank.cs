// Rank.cs
// 카드의 숫자(rank)를 나타내는 열거형.
// Two(2)부터 Ace(14)까지 13개의 값을 정의하며, 정수 값을 명시적으로 할당하여 비교 연산에 활용할 수 있다.

namespace TexasHoldem.Entity
{
    public enum Rank
    {
        Two = 2,
        Three = 3,
        Four = 4,
        Five = 5,
        Six = 6,
        Seven = 7,
        Eight = 8,
        Nine = 9,
        Ten = 10,
        Jack = 11,
        Queen = 12,
        King = 13,
        Ace = 14
    }
}
