// BlindPostingUsecase.cs
// SB/BB 좌석에서 블라인드 금액을 징수하여 팟에 추가하는 유스케이스.
// 플레이어의 잔여 칩이 블라인드 금액보다 적으면 올인 처리하여 보유 칩 전액을 징수한다.
// 징수 결과와 BlindPostedEvent 목록을 BlindResult로 반환한다.
// 사용법: var result = BlindPostingUsecase.PostBlinds(state, ledger, sbAmount, bbAmount, handId);

using System;
using System.Collections.Generic;
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
        /// <param name="handId">현재 핸드 식별자</param>
        /// <returns>갱신된 state, 실제 징수 금액, BlindPostedEvent 목록을 담은 BlindResult</returns>
        public static BlindResult PostBlinds(GameRoundState state, IChipLedger ledger, int sbAmount, int bbAmount, string handId)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (ledger == null)
                throw new ArgumentNullException(nameof(ledger));
            if (sbAmount <= 0)
                throw new ArgumentOutOfRangeException(nameof(sbAmount), "스몰 블라인드 금액은 1 이상이어야 합니다.");
            if (bbAmount <= 0)
                throw new ArgumentOutOfRangeException(nameof(bbAmount), "빅 블라인드 금액은 1 이상이어야 합니다.");

            var events = new List<BlindPostedEvent>();
            long timestamp = DateTime.UtcNow.Ticks;

            // SB 징수: 잔여 칩이 부족하면 올인
            int sbChips = ledger.GetChips(state.SbSeatIndex);
            int sbActual = Math.Min(sbChips, sbAmount);
            ledger.DeductChips(state.SbSeatIndex, sbActual);
            events.Add(new BlindPostedEvent(timestamp, handId, state.SbSeatIndex, sbActual, BlindType.Small));

            // BB 징수: 잔여 칩이 부족하면 올인
            int bbChips = ledger.GetChips(state.BbSeatIndex);
            int bbActual = Math.Min(bbChips, bbAmount);
            ledger.DeductChips(state.BbSeatIndex, bbActual);
            events.Add(new BlindPostedEvent(timestamp, handId, state.BbSeatIndex, bbActual, BlindType.Big));

            // 팟 및 현재 베팅 갱신
            state.PotTotal += sbActual + bbActual;
            state.CurrentBet = bbActual;

            return new BlindResult(state, sbActual, bbActual, events);
        }
    }
}
