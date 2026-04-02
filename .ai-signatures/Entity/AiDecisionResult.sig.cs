// Source: Assets/Scripts/Entity/AiDecisionResult.cs
// AiDecisionResult.cs
// AI 액션 결정 유즈케이스의 반환 DTO.
// 결정된 액션 타입(Fold/Check/Call/Raise)과 레이즈 금액을 포함한다.

namespace TexasHoldem.Entity
{
    public class AiDecisionResult
    {
        /// <summary>결정된 액션 타입.</summary>
        public ActionType Action { get; }

        /// <summary>레이즈 금액. Raise가 아닌 경우 0.</summary>
        public int RaiseAmount { get; }

        public AiDecisionResult(ActionType action, int raiseAmount) { /* ... */ }
        {
            Action = action;
            RaiseAmount = raiseAmount;
        }
    }
}
