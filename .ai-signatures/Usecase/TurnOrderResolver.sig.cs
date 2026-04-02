// Source: Assets/Scripts/Usecase/TurnOrderResolver.cs
// TurnOrderResolver.cs
// 베팅 라운드에서 액션해야 할 플레이어의 순서를 결정하는 순수 C# 클래스.
// 사용 방법: TurnOrderResolver.ResolveOrder(players, dealerIndex, phase)를 호출하면
// 해당 라운드에서 액션해야 할 플레이어 인덱스의 순서 리스트를 반환한다.
// PreFlop: UTG(BB 다음)부터 시계 방향, BB가 마지막.
// PostFlop: 딜러 다음부터 시계 방향.
// 헤즈업(2인) 특수 규칙도 처리한다.

using System.Collections.Generic;
using TexasHoldem.Entity;

namespace TexasHoldem.Usecase
{
    public class TurnOrderResolver
    {
        // 해당 라운드에서 액션해야 할 플레이어 인덱스의 순서 리스트를 반환한다.
        // Folded, AllIn 상태인 플레이어는 제외한다.
        public List<int> ResolveOrder(IReadOnlyList<PlayerData> players, int dealerIndex, GamePhase phase) { /* ... */ }
        {
            int count = players.Count;
            var order = new List<int>();

            int startIndex;

            if (phase == GamePhase.PreFlop)
            {
                startIndex = GetPreflopStartIndex(count, dealerIndex);
            }
            else
            {
                startIndex = GetPostflopStartIndex(count, dealerIndex);
            }

            // startIndex부터 시계 방향으로 한 바퀴 순회
            for (int i = 0; i < count; i++)
            {
                int idx = (startIndex + i) % count;
                PlayerData player = players[idx];

                if (player.Status == PlayerStatus.Active)
                {
                    order.Add(idx);
                }
            }

            return order;
        }

        // PreFlop 시작 인덱스를 결정한다.
        // 헤즈업: 딜러(=SB)가 먼저 액션.
        // 3인 이상: UTG(BB 다음)부터 시작.
        private int GetPreflopStartIndex(int playerCount, int dealerIndex) { /* ... */ }
        {
            if (playerCount == 2)
            {
                // 헤즈업: 딜러(=SB)가 먼저
                return dealerIndex;
            }

            // SB = 딜러 다음, BB = SB 다음, UTG = BB 다음
            int sbIndex = (dealerIndex + 1) % playerCount;
            int bbIndex = (sbIndex + 1) % playerCount;
            int utgIndex = (bbIndex + 1) % playerCount;
            return utgIndex;
        }

        // PostFlop 시작 인덱스를 결정한다.
        // 헤즈업: 딜러가 아닌 쪽(=BB)이 먼저.
        // 3인 이상: 딜러 다음부터 시작.
        private int GetPostflopStartIndex(int playerCount, int dealerIndex) { /* ... */ }
        {
            if (playerCount == 2)
            {
                // 헤즈업: 딜러가 아닌 쪽이 먼저
                return (dealerIndex + 1) % playerCount;
            }

            // 딜러 다음부터
            return (dealerIndex + 1) % playerCount;
        }
    }
}
