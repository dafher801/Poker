// PotCalculator.cs
// 핵심 팟 분배 알고리즘을 담당하는 정적/순수 클래스.
// List<PlayerBetInfo>를 받아 올인 금액 기준으로 메인 팟과 사이드 팟을 계산하여 List<Pot>을 반환한다.
// 폴드한 플레이어의 베팅액은 팟 총액에 포함되지만 EligiblePlayerIds에서는 제외된다.

using System.Collections.Generic;
using System.Linq;
using TexasHoldem.Entity;

namespace TexasHoldem.Usecase
{
    public static class PotCalculator
    {
        public static List<Pot> CalculatePots(List<PlayerBetInfo> bets)
        {
            if (bets == null || bets.Count == 0)
                return new List<Pot>();

            var activeBets = bets.Where(b => !b.IsFolded).ToList();
            bool hasAllIn = activeBets.Any(b => b.IsAllIn);

            if (!hasAllIn)
            {
                int totalAmount = bets.Sum(b => b.BetAmount);
                if (totalAmount == 0)
                    return new List<Pot>();

                var eligibleIds = activeBets.Select(b => b.PlayerId).ToList();
                return new List<Pot> { new Pot(totalAmount, eligibleIds) };
            }

            var allInAmounts = activeBets
                .Where(b => b.IsAllIn)
                .Select(b => b.BetAmount)
                .Distinct()
                .OrderBy(a => a)
                .ToList();

            var remaining = bets.ToDictionary(b => b.PlayerId, b => b.BetAmount);
            var pots = new List<Pot>();
            int previousLevel = 0;

            foreach (int allInAmount in allInAmounts)
            {
                int levelContribution = allInAmount - previousLevel;
                if (levelContribution <= 0)
                    continue;

                int potAmount = 0;
                var eligibleIds = new List<string>();

                foreach (var bet in bets)
                {
                    int canContribute = System.Math.Min(remaining[bet.PlayerId], levelContribution);
                    if (canContribute > 0)
                    {
                        potAmount += canContribute;
                        remaining[bet.PlayerId] -= canContribute;
                    }

                    if (!bet.IsFolded && canContribute > 0)
                    {
                        eligibleIds.Add(bet.PlayerId);
                    }
                }

                if (potAmount > 0)
                {
                    pots.Add(new Pot(potAmount, eligibleIds));
                }

                previousLevel = allInAmount;
            }

            int lastPotAmount = 0;
            var lastEligibleIds = new List<string>();

            foreach (var bet in bets)
            {
                if (remaining[bet.PlayerId] > 0)
                {
                    lastPotAmount += remaining[bet.PlayerId];
                    if (!bet.IsFolded)
                    {
                        lastEligibleIds.Add(bet.PlayerId);
                    }
                    remaining[bet.PlayerId] = 0;
                }
            }

            if (lastPotAmount > 0)
            {
                pots.Add(new Pot(lastPotAmount, lastEligibleIds));
            }

            return pots;
        }
    }
}
