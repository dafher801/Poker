// SessionFlowUsecase.cs
// 핸드 간 세션 흐름 로직을 담당하는 유스케이스.
// 핸드 결과에 따른 칩 갱신·탈락 처리, 딜러 버튼 이동, 세션 종료 판정, 최종 결과 생성을 수행한다.
// 사용법: SessionDirector에서 핸드 종료 시마다 ProcessHandResult → AdvanceDealerButton → ShouldEndSession 순으로 호출하고,
//         세션 종료 시 GetSessionResult로 최종 결과를 얻는다.
using System.Collections.Generic;
using System.Linq;
using Poker.Entity;
using TexasHoldem.Entity;

namespace Poker.Usecase
{
    public class SessionFlowUsecase
    {
        /// <summary>
        /// 핸드 결과를 반영하여 각 플레이어의 칩을 갱신하고, 칩이 0 이하인 플레이어를 탈락 처리한다.
        /// </summary>
        public void ProcessHandResult(SessionState state, RoundResult handResult)
        {
            foreach (var potResult in handResult.PotResults)
            {
                foreach (int seatIndex in potResult.WinnerSeatIndices)
                {
                    string playerId = state.PlayerIds[seatIndex];
                    int currentChips = state.Chips[playerId];
                    state.SetChips(playerId, currentChips + potResult.AwardPerWinner);
                }

                if (potResult.Remainder > 0)
                {
                    int firstWinnerSeat = potResult.WinnerSeatIndices[0];
                    string firstWinnerId = state.PlayerIds[firstWinnerSeat];
                    int currentChips = state.Chips[firstWinnerId];
                    state.SetChips(firstWinnerId, currentChips + potResult.Remainder);
                }
            }

            state.HandCount++;

            foreach (string playerId in state.PlayerIds)
            {
                if (!state.Eliminated[playerId] && state.Chips[playerId] <= 0)
                {
                    state.EliminatePlayer(playerId);
                }
            }
        }

        /// <summary>
        /// 딜러 인덱스를 다음 활성 플레이어로 이동시킨다(탈락자 건너뛰기, 순환).
        /// </summary>
        public void AdvanceDealerButton(SessionState state)
        {
            int playerCount = state.PlayerIds.Count;
            int nextIndex = (state.DealerSeatIndex + 1) % playerCount;

            while (state.Eliminated[state.PlayerIds[nextIndex]])
            {
                nextIndex = (nextIndex + 1) % playerCount;
            }

            state.DealerSeatIndex = nextIndex;
        }

        /// <summary>
        /// 활성 플레이어가 1명 이하이거나 humanPlayerId가 탈락했으면 true를 반환한다.
        /// </summary>
        public bool ShouldEndSession(SessionState state, string humanPlayerId)
        {
            if (state.IsSessionOver())
                return true;

            if (state.Eliminated.ContainsKey(humanPlayerId) && state.Eliminated[humanPlayerId])
                return true;

            return false;
        }

        /// <summary>
        /// 최종 순위(탈락 순서 역순)·우승자 ID·각 플레이어 최종 칩을 포함하는 결과를 생성한다.
        /// </summary>
        public SessionResult GetSessionResult(SessionState state)
        {
            var activePlayers = new List<string>();
            var eliminatedPlayers = new List<(string playerId, int eliminatedAtHand)>();

            foreach (string playerId in state.PlayerIds)
            {
                if (state.Eliminated[playerId])
                {
                    int hand = state.EliminatedAtHand.ContainsKey(playerId)
                        ? state.EliminatedAtHand[playerId]
                        : 0;
                    eliminatedPlayers.Add((playerId, hand));
                }
                else
                {
                    activePlayers.Add(playerId);
                }
            }

            // 활성 플레이어: 칩 내림차순 정렬
            activePlayers = activePlayers
                .OrderByDescending(id => state.Chips[id])
                .ToList();

            // 탈락 플레이어: 나중에 탈락한 순서가 높은 순위 (탈락 핸드 내림차순)
            eliminatedPlayers = eliminatedPlayers
                .OrderByDescending(e => e.eliminatedAtHand)
                .ToList();

            var rankings = new List<PlayerRanking>();
            int rank = 1;

            foreach (string playerId in activePlayers)
            {
                rankings.Add(new PlayerRanking(rank, playerId, state.Chips[playerId], null));
                rank++;
            }

            foreach (var (playerId, eliminatedAtHand) in eliminatedPlayers)
            {
                rankings.Add(new PlayerRanking(rank, playerId, state.Chips[playerId], eliminatedAtHand));
                rank++;
            }

            string winnerId = rankings[0].PlayerId;

            return new SessionResult(winnerId, rankings);
        }
    }
}
