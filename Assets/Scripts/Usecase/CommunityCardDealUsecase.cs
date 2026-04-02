// CommunityCardDealUsecase.cs
// 커뮤니티 카드를 딜하는 유스케이스.
// targetPhase에 따라 번(burn) 카드를 소비한 뒤 Flop은 3장, Turn/River는 1장을 state.CommunityCards에 추가한다.
// 사용법: var updatedState = CommunityCardDealUsecase.DealCommunityCards(deck, state, RoundPhase.Flop);

using System;
using TexasHoldem.Entity;

namespace TexasHoldem.Usecase
{
    public static class CommunityCardDealUsecase
    {
        /// <summary>
        /// 지정된 페이즈에 맞춰 커뮤니티 카드를 딜한다.
        /// Flop: 1장 번 후 3장 추가. Turn/River: 1장 번 후 1장 추가.
        /// </summary>
        /// <param name="deck">카드를 뽑을 덱</param>
        /// <param name="state">현재 게임 라운드 상태</param>
        /// <param name="targetPhase">딜할 페이즈 (Flop, Turn, River만 허용)</param>
        /// <returns>커뮤니티 카드가 추가된 갱신된 state</returns>
        public static GameRoundState DealCommunityCards(Deck deck, GameRoundState state, RoundPhase targetPhase)
        {
            if (deck == null)
                throw new ArgumentNullException(nameof(deck));

            if (state == null)
                throw new ArgumentNullException(nameof(state));

            int cardsToDeal;
            switch (targetPhase)
            {
                case RoundPhase.Flop:
                    cardsToDeal = 3;
                    break;
                case RoundPhase.Turn:
                case RoundPhase.River:
                    cardsToDeal = 1;
                    break;
                default:
                    throw new ArgumentException(
                        $"커뮤니티 카드 딜은 Flop, Turn, River 페이즈에서만 가능합니다. 전달된 페이즈: {targetPhase}",
                        nameof(targetPhase));
            }

            // 번(burn) 카드 1장 소비
            deck.Draw();

            // 커뮤니티 카드 추가
            for (int i = 0; i < cardsToDeal; i++)
            {
                state.CommunityCards.Add(deck.Draw());
            }

            state.CurrentPhase = targetPhase;

            return state;
        }
    }
}
