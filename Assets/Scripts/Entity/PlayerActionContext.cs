// Source: Assets/Scripts/Entity/PlayerActionContext.cs
// PlayerActionContext.cs
// 로컬 플레이어에게 턴 결정을 요청할 때 전달하는 맥락 데이터 클래스.
// 현재 콜 금액, 레이즈 범위, 잔여 칩, 팟 합계, 허용 액션 집합을 불변으로 보유한다.
// View에서 버튼 활성화 여부와 레이블 텍스트를 결정하는 데 사용된다.

using System.Collections.Generic;

namespace TexasHoldem.Entity
{
    public class PlayerActionContext
    {
        /// <summary>콜에 필요한 추가 금액</summary>
        public int CurrentBetToCall { get; }

        /// <summary>최소 레이즈 총액</summary>
        public int MinRaiseAmount { get; }

        /// <summary>최대 레이즈 총액 (올인 시 총액)</summary>
        public int MaxRaiseAmount { get; }

        /// <summary>플레이어 잔여 칩</summary>
        public int PlayerChips { get; }

        /// <summary>현재 팟 합계</summary>
        public int PotTotal { get; }

        /// <summary>현 상황에서 허용되는 액션 타입 집합</summary>
        public HashSet<ActionType> ValidActions { get; }

        public PlayerActionContext(
            int currentBetToCall,
            int minRaiseAmount,
            int maxRaiseAmount,
            int playerChips,
            int potTotal,
            HashSet<ActionType> validActions)
        {
            CurrentBetToCall = currentBetToCall;
            MinRaiseAmount = minRaiseAmount;
            MaxRaiseAmount = maxRaiseAmount;
            PlayerChips = playerChips;
            PotTotal = potTotal;
            ValidActions = validActions;
        }
    }
}
