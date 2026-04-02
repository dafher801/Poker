// Source: Assets/Scripts/Entity/LegalActionSet.cs
// LegalActionSet.cs
// ActionValidator가 반환하는 합법 액션 정보를 담는 구조체.
// 특정 플레이어가 현재 취할 수 있는 액션 종류와 금액 범위를 포함한다.

using System.Collections.Generic;

namespace TexasHoldem.Entity
{
    public struct LegalActionSet
    {
        // 수행 가능한 액션 종류 목록
        public List<ActionType> AvailableActions;

        // 콜에 필요한 금액. 콜 불가 시 0
        public int CallAmount;

        // 최소 레이즈 총액 (자신의 CurrentBet 기준 총 베팅 금액)
        public int MinRaiseAmount;

        // 최대 레이즈 총액 (올인 금액)
        public int MaxRaiseAmount;
    }
}
