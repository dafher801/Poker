// Pot.cs
// 포커 게임의 팟(상금 풀)을 나타내는 엔티티.
// Amount는 0 이상이어야 하며, EligiblePlayerIds는 null 불가.
// AddAmount(int)로 팟 금액을 증가시키고, 음수 추가는 방지한다.

using System;
using System.Collections.Generic;

namespace TexasHoldem.Entity
{
    public class Pot
    {
        public int Amount { get; private set; }
        public List<string> EligiblePlayerIds { get; }

        public Pot(int amount, List<string> eligiblePlayerIds)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be 0 or more.");
            if (eligiblePlayerIds == null)
                throw new ArgumentNullException(nameof(eligiblePlayerIds), "EligiblePlayerIds must not be null.");

            Amount = amount;
            EligiblePlayerIds = eligiblePlayerIds;
        }

        public Pot() : this(0, new List<string>()) { }

        public void AddAmount(int value)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), "Cannot add a negative amount.");

            Amount += value;
        }
    }
}
