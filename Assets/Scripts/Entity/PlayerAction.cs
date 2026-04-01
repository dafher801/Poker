// PlayerAction.cs
// 플레이어 한 명의 단일 행동을 나타내는 불변 값 객체.
// PlayerId, ActionType, Amount를 보유하며 ActionType별 Amount 유효성을 생성자에서 검증한다.
// - Fold, Check: Amount는 반드시 0이어야 한다.
// - Raise: Amount는 1 이상이어야 한다.
// - Call, AllIn: Amount는 0 이상이어야 한다.

using System;

namespace TexasHoldem.Entity
{
    public class PlayerAction
    {
        public string PlayerId { get; }
        public ActionType Type { get; }
        public int Amount { get; }

        public PlayerAction(string playerId, ActionType type, int amount)
        {
            switch (type)
            {
                case ActionType.Fold:
                case ActionType.Check:
                    if (amount != 0)
                        throw new ArgumentException($"Amount must be 0 for {type}.", nameof(amount));
                    break;
                case ActionType.Raise:
                    if (amount < 1)
                        throw new ArgumentException("Amount must be 1 or more for Raise.", nameof(amount));
                    break;
                case ActionType.Call:
                case ActionType.AllIn:
                    if (amount < 0)
                        throw new ArgumentException($"Amount must be 0 or more for {type}.", nameof(amount));
                    break;
            }

            PlayerId = playerId;
            Type = type;
            Amount = amount;
        }
    }
}
