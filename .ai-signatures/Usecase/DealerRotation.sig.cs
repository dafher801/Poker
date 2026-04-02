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
        /// <summary>
        /// 현재 DealerIndex 기준으로 다음 딜러(Eliminated가 아닌 플레이어)의 인덱스를 반환한다.
        /// </summary>
        public static int GetNextDealer(GameState state) { /* ... */ }
        {
            var players = state.Players;
            int count = players.Count;

            for (int i = 1; i <= count; i++)
            {
                int index = (state.DealerIndex + i) % count;
                if (players[index].Status != PlayerStatus.Eliminated)
                    return index;
            }

            throw new InvalidOperationException("No active players available for dealer.");
        }

        /// <summary>
        /// 딜러 인덱스와 플레이어 목록을 기반으로 SB/BB 포지션을 결정한다.
        /// 헤즈업(활성 2명)일 때 딜러=SB, 3명 이상일 때 딜러 다음=SB.
        /// </summary>
        public static (int SB, int BB) GetBlindPositions(int dealerIndex, List<PlayerData> players) { /* ... */ }
        {
            int count = players.Count;

            // 활성 플레이어 수 계산
            int activeCount = 0;
            for (int i = 0; i < count; i++)
            {
                if (players[i].Status != PlayerStatus.Eliminated)
                    activeCount++;
            }

            if (activeCount < 2)
                throw new InvalidOperationException("At least 2 active players are required for blinds.");

            if (activeCount == 2)
            {
                // 헤즈업: 딜러 = SB
                int sb = dealerIndex;
                int bb = FindNextActive(dealerIndex, players);
                return (sb, bb);
            }
            else
            {
                // 3명 이상: 딜러 다음 = SB, SB 다음 = BB
                int sb = FindNextActive(dealerIndex, players);
                int bb = FindNextActive(sb, players);
                return (sb, bb);
            }
        }

        /// <summary>
        /// 주어진 인덱스 다음 위치부터 순환하며 Eliminated가 아닌 첫 플레이어 인덱스를 반환한다.
        /// </summary>
        private static int FindNextActive(int fromIndex, List<PlayerData> players) { /* ... */ }
        {
            int count = players.Count;
            for (int i = 1; i <= count; i++)
            {
                int index = (fromIndex + i) % count;
                if (players[index].Status != PlayerStatus.Eliminated)
                    return index;
            }

            throw new InvalidOperationException("No active players found.");
        }
    }
}
