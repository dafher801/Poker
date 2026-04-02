// Source: Assets/Scripts/Usecase/ActionExecutor.cs
// ActionExecutor.cs
// 플레이어 액션(Fold, Check, Call, Raise, AllIn)을 GameState에 적용한다.
// 사용 방법: ActionExecutor.Execute(state, action)을 호출하면
// 해당 플레이어의 Chips, CurrentBet, Status와 GameState의 LastRaiseSize가 갱신된다.
// 각 액션 실행 시 투입된 칩은 즉시 메인 팟(Pots[0])에 누적된다.
// ActionValidator로 합법성이 검증된 액션만 전달되어야 한다.

using System;
using System.Linq;
using TexasHoldem.Entity;

namespace TexasHoldem.Usecase
{
    public class ActionExecutor
    {
        // 플레이어 액션을 GameState에 적용하여 상태를 갱신한다.
        // Fold: player.Status = Folded
        // Check: 상태 변경 없음
        // Call: 콜 금액만큼 Chips 차감, CurrentBet 증가, Pot에 추가
        // Raise: action.Amount(목표 총 베팅액)까지 Chips 차감, LastRaiseSize 갱신, Pot에 추가
        // AllIn: 남은 Chips 전부 투입, Status = AllIn, 최고 베팅 초과 시 LastRaiseSize 갱신
        public void Execute(GameState state, PlayerAction action) { /* ... */ }
    }
}
