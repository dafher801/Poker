// Source: Assets/Scripts/Usecase/PotCalculator.cs
// PotCalculator.cs
// 핵심 팟 분배 알고리즘을 담당하는 정적/순수 클래스.
// List<PlayerBetInfo>를 받아 올인 금액 기준으로 메인 팟과 사이드 팟을 계산하여 List<Pot>을 반환한다.
// 폴드한 플레이어의 베팅액은 팟 총액에 포함되지만 EligiblePlayerIds에서는 제외된다.

using System.Collections.Generic;
using TexasHoldem.Entity;

namespace TexasHoldem.Usecase
{
    public static class PotCalculator
    {
        // 플레이어 베팅 정보 리스트를 받아 메인 팟과 사이드 팟 목록을 계산하여 반환한다.
        public static List<Pot> CalculatePots(List<PlayerBetInfo> bets) { /* ... */ }
    }
}
