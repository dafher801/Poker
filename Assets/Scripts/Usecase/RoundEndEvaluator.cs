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
        // 베팅 라운드가 완료되었는지 판정한다.
        // 모든 Active 플레이어가 한 번 이상 행동(hasActed == true)했고,
        // 각 Active 플레이어의 CurrentBet이 highestBet과 같으면 true를 반환한다.
        // AllIn 또는 Folded 상태의 플레이어는 검사에서 제외된다.
        public bool IsBettingRoundComplete(IReadOnlyList<PlayerData> players, int highestBet, bool[] hasActed)
        {
            for (int i = 0; i < players.Count; i++)
            {
                PlayerData player = players[i];

                if (player.Status != PlayerStatus.Active)
                    continue;

                if (!hasActed[i])
                    return false;

                if (player.CurrentBet != highestBet)
                    return false;
            }

            return true;
        }

        // Folded가 아닌 플레이어가 1명만 남았는지 판정한다.
        // true를 반환할 경우 winningSeatIndex에 해당 플레이어의 인덱스를 설정한다.
        // AllIn 플레이어도 Folded가 아니므로 카운트에 포함된다.
        public bool IsOnlyOnePlayerRemaining(IReadOnlyList<PlayerData> players, out int winningSeatIndex)
        {
            winningSeatIndex = -1;
            int remainingCount = 0;

            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].Status != PlayerStatus.Folded && players[i].Status != PlayerStatus.Eliminated)
                {
                    remainingCount++;
                    winningSeatIndex = i;
                }
            }

            if (remainingCount == 1)
                return true;

            winningSeatIndex = -1;
            return false;
        }
    }
}
