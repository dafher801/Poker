// Source: Assets/Scripts/Usecase/BlindPostingUsecase.cs
// BlindPostingUsecase.cs
// SB/BB 좌석에서 블라인드 금액을 징수하여 팟에 추가하는 유스케이스.
// 플레이어의 잔여 칩이 블라인드 금액보다 적으면 올인 처리하여 보유 칩 전액을 징수한다.
// 사용법: var updatedState = BlindPostingUsecase.PostBlinds(state, ledger, sbAmount, bbAmount);

using System;
using TexasHoldem.Entity;
using TexasHoldem.Gateway;

namespace TexasHoldem.Usecase
{
    public static class BlindPostingUsecase
    {
        /// <summary>
        /// SB/BB 좌석에서 블라인드를 징수하여 팟에 추가한다.
        /// 잔여 칩이 블라인드보다 적으면 올인 처리(보유 칩 전액 징수).
        /// </summary>
        /// <param name="state">현재 라운드 상태</param>
        /// <param name="ledger">칩 관리 인터페이스</param>
        /// <param name="sbAmount">스몰 블라인드 금액</param>
        /// <param name="bbAmount">빅 블라인드 금액</param>
        /// <returns>potTotal, currentBet이 갱신된 state</returns>
        public static GameRoundState PostBlinds(GameRoundState state, IChipLedger ledger, int sbAmount, int bbAmount) { /* ... */ }
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (ledger == null)
                throw new ArgumentNullException(nameof(ledger));
            if (sbAmount <= 0)
                throw new ArgumentOutOfRangeException(nameof(sbAmount), "스몰 블라인드 금액은 1 이상이어야 합니다.");
            if (bbAmount <= 0)
                throw new ArgumentOutOfRangeException(nameof(bbAmount), "빅 블라인드 금액은 1 이상이어야 합니다.");

            // SB 징수: 잔여 칩이 부족하면 올인
            int sbChips = ledger.GetChips(state.SbSeatIndex);
            int sbActual = Math.Min(sbChips, sbAmount);
            ledger.DeductChips(state.SbSeatIndex, sbActual);

            // BB 징수: 잔여 칩이 부족하면 올인
            int bbChips = ledger.GetChips(state.BbSeatIndex);
            int bbActual = Math.Min(bbChips, bbAmount);
            ledger.DeductChips(state.BbSeatIndex, bbActual);

            // 팟 및 현재 베팅 갱신
            state.PotTotal += sbActual + bbActual;
            state.CurrentBet = bbActual;

            return state;
        }
    }
}
