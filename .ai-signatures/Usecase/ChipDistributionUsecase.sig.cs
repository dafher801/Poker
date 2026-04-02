// Source: Assets/Scripts/Usecase/ChipDistributionUsecase.cs
// ChipDistributionUsecase.cs
// 쇼다운 또는 얼리윈 후 각 PotResult를 기반으로 승자에게 칩을 지급하는 유스케이스.
// 사용 방법:
//   ChipDistributionUsecase.DistributeChips(potResults, ledger);
// 각 PotResult의 승자에게 awardPerWinner만큼 칩을 지급하고,
// remainder가 있으면 딜러 좌측 첫 번째 승자(WinnerSeatIndices[0])에게 추가 지급한다.
// 총 분배 금액이 총 팟 금액과 일치하지 않으면 InvalidOperationException을 던진다.

using System;
using System.Collections.Generic;
using TexasHoldem.Entity;
using TexasHoldem.Gateway;

namespace TexasHoldem.Usecase
{
    public static class ChipDistributionUsecase
    {
        /// <summary>
        /// 각 PotResult를 순회하며 승자에게 칩을 지급한다.
        /// remainder가 0보다 크면 WinnerSeatIndices[0](딜러 좌측 첫 번째 승자)에게 추가 지급한다.
        /// 총 분배 금액이 총 팟 금액과 일치하지 않으면 InvalidOperationException을 던진다.
        /// </summary>
        public static void DistributeChips(List<PotResult> potResults, IChipLedger ledger) { /* ... */ }
        {
            if (potResults == null)
                throw new ArgumentNullException(nameof(potResults));
            if (ledger == null)
                throw new ArgumentNullException(nameof(ledger));

            int totalPotAmount = 0;
            int totalDistributed = 0;

            foreach (var potResult in potResults)
            {
                totalPotAmount += potResult.PotAmount;

                foreach (var seatIndex in potResult.WinnerSeatIndices)
                {
                    ledger.AddChips(seatIndex, potResult.AwardPerWinner);
                    totalDistributed += potResult.AwardPerWinner;
                }

                if (potResult.Remainder > 0)
                {
                    int prioritySeat = potResult.WinnerSeatIndices[0];
                    ledger.AddChips(prioritySeat, potResult.Remainder);
                    totalDistributed += potResult.Remainder;
                }
            }

            if (totalDistributed != totalPotAmount)
            {
                throw new InvalidOperationException(
                    $"Chip distribution mismatch: total pot amount is {totalPotAmount} but distributed {totalDistributed}.");
            }
        }
    }
}
