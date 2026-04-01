// HandRank.cs
// 텍사스 홀덤 족보 등급을 나타내는 enum.
// 정수 값의 대소 비교만으로 족보 등급의 우열을 판단할 수 있다.
// 예: HandRank.Flush > HandRank.Straight → (int)Flush=5 > (int)Straight=4

namespace Entity
{
    public enum HandRank
    {
        HighCard       = 0,
        OnePair        = 1,
        TwoPair        = 2,
        ThreeOfAKind   = 3,
        Straight       = 4,
        Flush          = 5,
        FullHouse      = 6,
        FourOfAKind    = 7,
        StraightFlush  = 8,
        RoyalFlush     = 9
    }
}
