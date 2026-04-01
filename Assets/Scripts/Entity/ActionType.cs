// ActionType.cs
// 플레이어가 취할 수 있는 행동의 종류를 정의하는 열거형.
// Fold, Check, Call, Raise, AllIn 다섯 가지 값을 갖는다.

namespace TexasHoldem.Entity
{
    public enum ActionType
    {
        Fold,
        Check,
        Call,
        Raise,
        AllIn
    }
}
