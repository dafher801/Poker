// PlayerBetInfo.cs
// 한 베팅 라운드에서 개별 플레이어의 베팅 정보를 담는 데이터 클래스.
// PlayerId로 플레이어를 식별하고, BetAmount(이번 라운드 누적 베팅액), IsAllIn, IsFolded 상태를 보유한다.
// BetAmount는 0 미만이 될 수 없도록 보호한다.

namespace TexasHoldem.Entity
{
    public class PlayerBetInfo
    {
        public string PlayerId { get; }
        public int BetAmount { get; private set; }
        public bool IsAllIn { get; }
        public bool IsFolded { get; }

        public PlayerBetInfo(string playerId, int betAmount, bool isAllIn, bool isFolded)
        {
            PlayerId = playerId;
            BetAmount = betAmount < 0 ? 0 : betAmount;
            IsAllIn = isAllIn;
            IsFolded = isFolded;
        }
    }
}
