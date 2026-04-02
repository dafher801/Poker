// Pot.cs
// 포커 게임의 단일 팟(상금 풀)을 나타내는 엔티티.
// Amount는 0 이상이어야 하며, EligiblePlayerIds는 중복 없는 플레이어 ID 목록이다.
// AddAmount(int)로 팟 금액을 증가시키고, AddEligiblePlayer(string)로 자격 플레이어를 추가한다.
// RemovePlayer(string)로 특정 플레이어를 자격 목록에서 제거한다.

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
            EligiblePlayerIds = new List<string>();
            foreach (var id in eligiblePlayerIds)
            {
                if (!EligiblePlayerIds.Contains(id))
                    EligiblePlayerIds.Add(id);
            }
        }

        public Pot() : this(0, new List<string>()) { }

        public void AddAmount(int value)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), "Cannot add a negative amount.");

            Amount += value;
        }

        public void AddEligiblePlayer(string playerId)
        {
            if (playerId == null)
                throw new ArgumentNullException(nameof(playerId));
            if (!EligiblePlayerIds.Contains(playerId))
                EligiblePlayerIds.Add(playerId);
        }

        public void RemovePlayer(string playerId)
        {
            EligiblePlayerIds.Remove(playerId);
        }
    }
}
