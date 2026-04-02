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
    }
}
