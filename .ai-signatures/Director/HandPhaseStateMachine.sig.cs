// Source: Assets/Scripts/Director/HandPhaseStateMachine.cs
// HandPhaseStateMachine.cs
// 한 판의 페이즈 전이를 관리하는 상태 머신.
// MonoBehaviour를 상속하지 않는 순수 C# 클래스.
// 상태: None(Init) → PreFlop → Flop → Turn → River → Showdown → Complete(End).
// 각 상태에서 실행할 콜백(Func<Task>)을 등록할 수 있다.
// TransitionToNext()로 다음 상태 전이, SkipToShowdown()으로 중간 페이즈 건너뛰기,
// ForceEnd()로 즉시 Complete 상태로 전이(올폴드 시).
// 사용법: 콜백 등록 후 TransitionToNext()를 호출하여 페이즈를 진행한다.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TexasHoldem.Entity;

namespace TexasHoldem.Director
{
    public class HandPhaseStateMachine
    {
        private static readonly RoundPhase[] PhaseOrder =
        {
            RoundPhase.None,
            RoundPhase.PreFlop,
            RoundPhase.Flop,
            RoundPhase.Turn,
            RoundPhase.River,
            RoundPhase.Showdown,
            RoundPhase.Complete
        };

        private readonly Dictionary<RoundPhase, Func<Task>> _phaseCallbacks = new Dictionary<RoundPhase, Func<Task>>();

        public RoundPhase CurrentPhase { get; private set; } = RoundPhase.None;

        public event Action<RoundPhase, RoundPhase> OnPhaseChanged;

        public void RegisterCallback(RoundPhase phase, Func<Task> callback) { /* ... */ }
        {
            _phaseCallbacks[phase] = callback;
        }

        public async Task TransitionToNext() { /* ... */ }
        {
            int currentIndex = Array.IndexOf(PhaseOrder, CurrentPhase);
            if (currentIndex < 0 || currentIndex >= PhaseOrder.Length - 1)
            {
                throw new InvalidOperationException($"Cannot transition from {CurrentPhase}: already at final phase or invalid state.");
            }

            RoundPhase previousPhase = CurrentPhase;
            RoundPhase nextPhase = PhaseOrder[currentIndex + 1];
            CurrentPhase = nextPhase;

            OnPhaseChanged?.Invoke(previousPhase, nextPhase);

            if (_phaseCallbacks.TryGetValue(nextPhase, out var callback))
            {
                await callback();
            }
        }

        public async Task SkipToShowdown() { /* ... */ }
        {
            if (CurrentPhase == RoundPhase.Showdown || CurrentPhase == RoundPhase.Complete)
            {
                throw new InvalidOperationException($"Cannot skip to showdown from {CurrentPhase}.");
            }

            RoundPhase previousPhase = CurrentPhase;
            CurrentPhase = RoundPhase.Showdown;

            OnPhaseChanged?.Invoke(previousPhase, RoundPhase.Showdown);

            if (_phaseCallbacks.TryGetValue(RoundPhase.Showdown, out var callback))
            {
                await callback();
            }
        }

        public async Task ForceEnd() { /* ... */ }
        {
            if (CurrentPhase == RoundPhase.Complete)
            {
                throw new InvalidOperationException("Already at Complete phase.");
            }

            RoundPhase previousPhase = CurrentPhase;
            CurrentPhase = RoundPhase.Complete;

            OnPhaseChanged?.Invoke(previousPhase, RoundPhase.Complete);

            if (_phaseCallbacks.TryGetValue(RoundPhase.Complete, out var callback))
            {
                await callback();
            }
        }
    }
}
