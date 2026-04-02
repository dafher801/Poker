// AiActionDecisionUsecase.cs
// AI 액션 결정 핵심 로직 유즈케이스.
// AiDecisionContext를 입력받아 핸드 강도, 팟 오즈, 성향 프로필, 포지션 등을 종합하여
// Fold/Check/Call/Raise 중 하나를 결정한다.
// 모든 랜덤은 생성자 주입된 System.Random을 사용하여 테스트 시 시드 고정이 가능하다.

using System;
using TexasHoldem.Entity;

namespace TexasHoldem.Usecase
{
    public class AiActionDecisionUsecase
    {
        private readonly Random _random;

        public AiActionDecisionUsecase(Random random)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        /// <summary>
        /// AI 의사결정을 수행하여 액션을 반환한다.
        /// </summary>
        public AiDecisionResult Decide(AiDecisionContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var personality = context.Personality;

            // (1) 유효 핸드 강도 = handStrength + 블러프 기반 랜덤 보정
            float bluffBoost = 0f;
            if (_random.NextDouble() < personality.BluffFrequency)
            {
                bluffBoost = (float)(_random.NextDouble() * 0.2);
            }
            float effectiveStrength = Math.Min(1f, context.HandStrength + bluffBoost);

            // (5) 포지션 보정: 레이트 포지션일수록 문턱값을 낮춘다
            float positionAdjustment = 0f;
            if (context.TotalPlayers > 0)
            {
                positionAdjustment = (float)context.PositionIndex / context.TotalPlayers * 0.05f;
            }

            // (4) 레이즈 판정 문턱값
            float raiseThreshold = personality.HandThreshold * (1f + personality.AggressionFactor) - positionAdjustment;

            // 액션 결정
            ActionType action;
            int raiseAmount = 0;

            if (effectiveStrength > raiseThreshold && context.MinRaise > 0 && context.MinRaise <= context.MaxRaise)
            {
                // (4) 레이즈
                float raisePotRatio = personality.RaiseMinPotRatio +
                    (float)_random.NextDouble() * (personality.RaiseMaxPotRatio - personality.RaiseMinPotRatio);
                raiseAmount = (int)(context.PotSize * raisePotRatio);
                raiseAmount = Math.Max(context.MinRaise, Math.Min(raiseAmount, context.MaxRaise));
                action = ActionType.Raise;
            }
            else if (context.AmountToCall == 0)
            {
                // (3) 체크 가능한 상황
                action = ActionType.Check;
            }
            else if (effectiveStrength > context.PotOdds - personality.CallOddsMargin)
            {
                // (3) 콜
                action = ActionType.Call;
            }
            else
            {
                // (2) 폴드
                action = ActionType.Fold;
            }

            // (6) 최종 랜덤 퍼터베이션: 5~10% 확률로 한 단계 전환
            double perturbChance = 0.05 + _random.NextDouble() * 0.05;
            if (_random.NextDouble() < perturbChance)
            {
                action = PerturbAction(action, context, ref raiseAmount);
            }

            return new AiDecisionResult(action, raiseAmount);
        }

        /// <summary>
        /// 액션을 한 단계 공격적 또는 보수적으로 전환한다.
        /// 랜덤으로 방향을 결정한다.
        /// </summary>
        private ActionType PerturbAction(ActionType current, AiDecisionContext context, ref int raiseAmount)
        {
            bool goAggressive = _random.NextDouble() >= 0.5;

            if (goAggressive)
            {
                // 한 단계 공격적으로
                switch (current)
                {
                    case ActionType.Fold:
                        if (context.AmountToCall == 0)
                            return ActionType.Check;
                        raiseAmount = 0;
                        return ActionType.Call;

                    case ActionType.Check:
                        if (context.MinRaise > 0 && context.MinRaise <= context.MaxRaise)
                        {
                            raiseAmount = context.MinRaise;
                            return ActionType.Raise;
                        }
                        return ActionType.Check;

                    case ActionType.Call:
                        if (context.MinRaise > 0 && context.MinRaise <= context.MaxRaise)
                        {
                            raiseAmount = context.MinRaise;
                            return ActionType.Raise;
                        }
                        return ActionType.Call;

                    default:
                        return current;
                }
            }
            else
            {
                // 한 단계 보수적으로
                switch (current)
                {
                    case ActionType.Raise:
                        raiseAmount = 0;
                        if (context.AmountToCall == 0)
                            return ActionType.Check;
                        return ActionType.Call;

                    case ActionType.Call:
                        return ActionType.Fold;

                    case ActionType.Check:
                        return ActionType.Check;

                    default:
                        return current;
                }
            }
        }
    }
}
