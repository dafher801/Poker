// Source: Assets/Scripts/Usecase/RoundEndEvaluator.cs
// RoundEndEvaluator.cs
// 베팅 라운드의 종료 조건과 핸드 즉시 종료(1명만 남은 경우)를 판정하는 순수 C# 클래스.
// 사용 방법:
//   var evaluator = new RoundEndEvaluator();
//   bool roundDone = evaluator.IsBettingRoundComplete(state.Players, highestBet, hasActed);
//   bool handDone = evaluator.IsOnlyOnePlayerRemaining(state.Players, out int winnerIndex);

using System.Collections.Generic;
using TexasHoldem.Entity;

namespace TexasHoldem.Usecase
{
    public class RoundEndEvaluator
    {
        // 모든 Active 플레이어가 행동을 완료했고 CurrentBet이 highestBet과 같으면 true.
        // AllIn, Folded 상태는 검사에서 제외된다.
        public bool IsBettingRoundComplete(IReadOnlyList<PlayerData> players, int highestBet, bool[] hasActed) { /* ... */ }

        // Folded/Eliminated가 아닌 플레이어가 1명만 남으면 true.
        // winningSeatIndex에 해당 플레이어의 인덱스를 설정한다.
        public bool IsOnlyOnePlayerRemaining(IReadOnlyList<PlayerData> players, out int winningSeatIndex) { /* ... */ }
    }
}
