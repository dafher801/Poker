// AiPersonalityProfile.cs
// AI 플레이어의 성향을 정의하는 불변 데이터 클래스.
// 핸드 참여 문턱값, 공격성 계수, 블러프 확률, 레이즈 팟 비율 범위, 콜 팟오즈 마진을 포함한다.
// 모든 필드는 0~1 범위로 클램핑된다.
// 정적 팩토리 메서드 CreateTightPassive(), CreateLooseAggressive()로 프리셋을 제공한다.

using System;

namespace TexasHoldem.Entity
{
    public class AiPersonalityProfile
    {
        /// <summary>핸드 참여 문턱값 (0~1). 낮을수록 넓은 핸드 레인지.</summary>
        public float HandThreshold { get; }

        /// <summary>공격성 계수 (0~1). 높을수록 레이즈 빈도 증가.</summary>
        public float AggressionFactor { get; }

        /// <summary>블러프 확률 (0~1).</summary>
        public float BluffFrequency { get; }

        /// <summary>레이즈 시 팟 대비 최소 비율 (0~1).</summary>
        public float RaiseMinPotRatio { get; }

        /// <summary>레이즈 시 팟 대비 최대 비율 (0~1).</summary>
        public float RaiseMaxPotRatio { get; }

        /// <summary>콜 팟오즈 마진 (0~1). 필요 팟오즈 대비 여유 마진.</summary>
        public float CallOddsMargin { get; }

        public AiPersonalityProfile(
            float handThreshold,
            float aggressionFactor,
            float bluffFrequency,
            float raiseMinPotRatio,
            float raiseMaxPotRatio,
            float callOddsMargin)
        {
            HandThreshold = Clamp01(handThreshold);
            AggressionFactor = Clamp01(aggressionFactor);
            BluffFrequency = Clamp01(bluffFrequency);
            RaiseMinPotRatio = Clamp01(raiseMinPotRatio);
            RaiseMaxPotRatio = Clamp01(raiseMaxPotRatio);
            CallOddsMargin = Clamp01(callOddsMargin);
        }

        /// <summary>보수적(Tight-Passive) 프리셋. 좁은 핸드 레인지, 낮은 공격성, 낮은 블러프.</summary>
        public static AiPersonalityProfile CreateTightPassive()
        {
            return new AiPersonalityProfile(
                handThreshold: 0.7f,
                aggressionFactor: 0.2f,
                bluffFrequency: 0.05f,
                raiseMinPotRatio: 0.3f,
                raiseMaxPotRatio: 0.5f,
                callOddsMargin: 0.05f
            );
        }

        /// <summary>공격적(Loose-Aggressive) 프리셋. 넓은 핸드 레인지, 높은 공격성, 높은 블러프.</summary>
        public static AiPersonalityProfile CreateLooseAggressive()
        {
            return new AiPersonalityProfile(
                handThreshold: 0.35f,
                aggressionFactor: 0.8f,
                bluffFrequency: 0.25f,
                raiseMinPotRatio: 0.5f,
                raiseMaxPotRatio: 1.0f,
                callOddsMargin: 0.15f
            );
        }

        private static float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            if (value > 1f) return 1f;
            return value;
        }
    }
}
