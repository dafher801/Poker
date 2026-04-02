// AiDecisionContext.cs
// AI 액션 결정 유즈케이스의 입력 파라미터 DTO.
// 핸드 강도, 팟 오즈, 팟 크기, 콜 비용, 칩, 레이즈 범위, 포지션, 성향 등을 포함한다.

namespace TexasHoldem.Entity
{
    public class AiDecisionContext
    {
        /// <summary>0~1 정규화된 핸드 강도.</summary>
        public float HandStrength { get; }

        /// <summary>0~1 팟 오즈(필요 승률).</summary>
        public float PotOdds { get; }

        /// <summary>현재 팟 크기.</summary>
        public int PotSize { get; }

        /// <summary>콜에 필요한 금액.</summary>
        public int AmountToCall { get; }

        /// <summary>플레이어의 남은 칩.</summary>
        public int PlayerChips { get; }

        /// <summary>최소 레이즈 금액.</summary>
        public int MinRaise { get; }

        /// <summary>최대 레이즈 금액.</summary>
        public int MaxRaise { get; }

        /// <summary>포지션 인덱스 (0=얼리, max=레이트).</summary>
        public int PositionIndex { get; }

        /// <summary>전체 플레이어 수.</summary>
        public int TotalPlayers { get; }

        /// <summary>AI 성향 프로필.</summary>
        public AiPersonalityProfile Personality { get; }

        public AiDecisionContext(
            float handStrength,
            float potOdds,
            int potSize,
            int amountToCall,
            int playerChips,
            int minRaise,
            int maxRaise,
            int positionIndex,
            int totalPlayers,
            AiPersonalityProfile personality)
        {
            HandStrength = handStrength;
            PotOdds = potOdds;
            PotSize = potSize;
            AmountToCall = amountToCall;
            PlayerChips = playerChips;
            MinRaise = minRaise;
            MaxRaise = maxRaise;
            PositionIndex = positionIndex;
            TotalPlayers = totalPlayers;
            Personality = personality;
        }
    }
}
