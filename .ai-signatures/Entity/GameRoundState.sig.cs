// Source: Assets/Scripts/Entity/GameRoundState.cs
// GameRoundState.cs
// 한 핸드(게임 라운드) 진행 중 필요한 모든 상태를 보관하는 데이터 클래스.
// 딜러/SB/BB 좌석, 현재 페이즈, 커뮤니티 카드, 활성 좌석, 현재 베팅, 팟 합계를 관리한다.
// 생성 시 기본값 검증(communityCards 5장 이하 등)을 수행한다.

using System;
using System.Collections.Generic;

namespace TexasHoldem.Entity
{
    public class GameRoundState
    {
        public int DealerSeatIndex { get; set; }
        public int SbSeatIndex { get; set; }
        public int BbSeatIndex { get; set; }
        public RoundPhase CurrentPhase { get; set; }
        public List<Card> CommunityCards { get; }
        public List<int> ActiveSeatIndices { get; }
        public int CurrentBet { get; set; }
        public int PotTotal { get; set; }

        public GameRoundState(
            int dealerSeatIndex,
            int sbSeatIndex,
            int bbSeatIndex,
            RoundPhase currentPhase,
            List<Card> communityCards,
            List<int> activeSeatIndices,
            int currentBet,
            int potTotal)
        {
            if (communityCards != null && communityCards.Count > 5)
            {
                throw new ArgumentException("커뮤니티 카드는 5장을 초과할 수 없습니다.", nameof(communityCards));
            }

            if (activeSeatIndices == null || activeSeatIndices.Count < 1)
            {
                throw new ArgumentException("활성 좌석은 최소 1개 이상이어야 합니다.", nameof(activeSeatIndices));
            }

            if (currentBet < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(currentBet), "현재 베팅 금액은 음수일 수 없습니다.");
            }

            if (potTotal < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(potTotal), "팟 합계는 음수일 수 없습니다.");
            }

            DealerSeatIndex = dealerSeatIndex;
            SbSeatIndex = sbSeatIndex;
            BbSeatIndex = bbSeatIndex;
            CurrentPhase = currentPhase;
            CommunityCards = communityCards ?? new List<Card>();
            ActiveSeatIndices = activeSeatIndices;
            CurrentBet = currentBet;
            PotTotal = potTotal;
        }
    }
}
