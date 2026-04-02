// PotOddsCalculationUsecase.cs
// 팟 오즈 계산 유즈케이스.
// 현재 팟 크기와 콜/레이즈 금액을 기반으로 팟 오즈(필요 승률) 및 레이즈 팟 비율을 계산한다.
// 순수 계산 로직만 포함하며 외부 의존이 없다.

namespace Poker.Usecase
{
    public class PotOddsCalculationUsecase
    {
        /// <summary>
        /// 팟 오즈(필요 승률)를 계산한다.
        /// 반환값: amountToCall / (currentPotSize + amountToCall), 0~1 범위.
        /// amountToCall이 0이면(체크 가능) 0을 반환한다.
        /// </summary>
        public float CalculatePotOdds(int currentPotSize, int amountToCall)
        {
            if (amountToCall <= 0)
                return 0f;

            return (float)amountToCall / (currentPotSize + amountToCall);
        }

        /// <summary>
        /// 레이즈 금액이 팟 대비 몇 배인지 반환한다.
        /// currentPotSize가 0이면 0을 반환한다.
        /// </summary>
        public float CalculateRaisePotRatio(int currentPotSize, int raiseAmount)
        {
            if (currentPotSize <= 0)
                return 0f;

            return (float)raiseAmount / currentPotSize;
        }
    }
}
