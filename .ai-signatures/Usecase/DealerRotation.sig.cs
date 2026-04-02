// Source: Assets/Scripts/Usecase/DealerRotation.cs
// DealerRotation.cs
// 딜러 버튼 이동 및 블라인드(SB/BB) 포지션 결정 로직을 담당하는 정적 유틸리티 클래스.
// GetNextDealer로 다음 딜러를 결정하고, GetBlindPositions로 SB/BB 위치를 반환한다.
// 헤즈업(2인) 시 딜러=SB 특수 규칙을 적용한다.

using System;
using System.Collections.Generic;
using TexasHoldem.Entity;

namespace TexasHoldem.Usecase
{
    public static class DealerRotation
    {
        // 현재 DealerIndex 기준으로 다음 딜러(Eliminated가 아닌 플레이어)의 인덱스를 반환한다.
        // 모든 플레이어가 Eliminated이면 InvalidOperationException을 던진다.
        public static int GetNextDealer(GameState state) { /* ... */ }

        // 딜러 인덱스와 플레이어 목록을 기반으로 SB/BB 포지션을 결정한다.
        // 헤즈업(활성 2명): 딜러=SB, 딜러 다음=BB
        // 3명 이상: 딜러 다음=SB, SB 다음=BB
        // Eliminated 플레이어는 건너뛴다.
        public static (int SB, int BB) GetBlindPositions(int dealerIndex, List<PlayerData> players) { /* ... */ }
    }
}
