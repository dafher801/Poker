// Source: Assets/Scripts/Gateway/IChipLedger.cs
// IChipLedger.cs
// 좌석별 칩 잔액을 관리하는 게이트웨이 인터페이스.
// GetChips로 잔액 조회, DeductChips로 차감, AddChips로 지급한다.
// BlindPostingUsecase, ChipDistributionUsecase 등에서 사용된다.

namespace TexasHoldem.Gateway
{
    public interface IChipLedger
    {
        /// <summary>
        /// 해당 좌석의 현재 칩 잔액을 반환한다.
        /// </summary>
        int GetChips(int seatIndex);

        /// <summary>
        /// 해당 좌석에서 지정한 금액만큼 칩을 차감한다.
        /// </summary>
        void DeductChips(int seatIndex, int amount);

        /// <summary>
        /// 해당 좌석에 지정한 금액만큼 칩을 지급한다.
        /// </summary>
        void AddChips(int seatIndex, int amount);
    }
}
