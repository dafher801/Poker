// Source: Assets/Scripts/View/CardAnimator.cs
// CardAnimator.cs
// 카드 딜 및 뒤집기 애니메이션을 담당하는 유틸리티 클래스.
// 코루틴 기반으로 딜 애니메이션(이동 + 스케일 팝업)과 뒤집기 애니메이션(X축 스케일 기반)을 제공한다.
// MonoBehaviour 인스턴스가 필요하며, StartCoroutine 호출을 위해 호출자의 MonoBehaviour를 전달받는다.
// 덱 위치(화면 밖 좌상단)를 기본 상수로 정의한다.

using System;
using System.Collections;
using UnityEngine;

namespace TexasHoldem.View
{
    public static class CardAnimator
    {
        /// <summary>
        /// 덱 위치 (화면 밖 좌상단). 카드 딜 애니메이션의 기본 출발 위치.
        /// </summary>
        public static readonly Vector3 DefaultDeckPosition = new Vector3(-8f, 5f, 0f);

        /// <summary>
        /// 카드 딜 애니메이션을 실행한다. fromPosition에서 toPosition으로 이동하며 scale 0→1 팝업 효과를 적용한다.
        /// </summary>
        /// <param name="runner">코루틴 실행을 위한 MonoBehaviour</param>
        /// <param name="card">애니메이션 대상 Transform</param>
        /// <param name="fromPosition">시작 위치</param>
        /// <param name="toPosition">도착 위치</param>
        /// <param name="duration">애니메이션 시간(초)</param>
        /// <param name="onComplete">완료 시 콜백</param>
        /// <returns>실행된 코루틴</returns>
        public static Coroutine AnimateDeal(MonoBehaviour runner, Transform card, Vector3 fromPosition, Vector3 toPosition, float duration = 0.4f, Action onComplete = null) { /* ... */ }
        {
            return runner.StartCoroutine(DealCoroutine(card, fromPosition, toPosition, duration, onComplete));
        }

        /// <summary>
        /// 카드 뒤집기 애니메이션을 실행한다. X축 스케일 기반으로 1→0(중간에 onHalfway 콜백)→0→1 복귀.
        /// onHalfway 콜백에서 스프라이트 교체를 수행한다.
        /// </summary>
        /// <param name="runner">코루틴 실행을 위한 MonoBehaviour</param>
        /// <param name="card">애니메이션 대상 Transform</param>
        /// <param name="onHalfway">중간 지점(스프라이트 교체 시점) 콜백</param>
        /// <param name="duration">애니메이션 시간(초)</param>
        /// <param name="onComplete">완료 시 콜백</param>
        /// <returns>실행된 코루틴</returns>
        public static Coroutine AnimateFlip(MonoBehaviour runner, Transform card, Action onHalfway, float duration = 0.3f, Action onComplete = null) { /* ... */ }
        {
            return runner.StartCoroutine(FlipCoroutine(card, onHalfway, duration, onComplete));
        }

        /// <summary>
        /// 딜 코루틴. fromPosition에서 toPosition으로 이동하며 스케일 0→1 팝업 애니메이션을 수행한다.
        /// </summary>
        public static IEnumerator DealCoroutine(Transform card, Vector3 fromPosition, Vector3 toPosition, float duration = 0.4f, Action onComplete = null) { /* ... */ }
        {
            card.position = fromPosition;
            Vector3 targetScale = Vector3.one;
            card.localScale = Vector3.zero;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // EaseOutBack 보간으로 팝업 효과
                float eased = EaseOutBack(t);

                card.position = Vector3.Lerp(fromPosition, toPosition, t);
                card.localScale = Vector3.Lerp(Vector3.zero, targetScale, eased);

                yield return null;
            }

            card.position = toPosition;
            card.localScale = targetScale;

            onComplete?.Invoke();
        }

        /// <summary>
        /// 뒤집기 코루틴. X축 스케일 기반으로 1→0→1 애니메이션. 중간 지점에서 onHalfway 콜백을 실행한다.
        /// </summary>
        public static IEnumerator FlipCoroutine(Transform card, Action onHalfway, float duration = 0.3f, Action onComplete = null) { /* ... */ }
        {
            float halfDuration = duration * 0.5f;
            Vector3 originalScale = Vector3.one;
            Vector3 flatScale = new Vector3(0f, originalScale.y, originalScale.z);

            // 첫 번째 절반: 현재 면을 접는다
            float elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                card.localScale = Vector3.Lerp(originalScale, flatScale, t);
                yield return null;
            }

            card.localScale = flatScale;

            // 중간 지점: 스프라이트 교체 콜백
            onHalfway?.Invoke();

            // 두 번째 절반: 새 면을 펼친다
            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                card.localScale = Vector3.Lerp(flatScale, originalScale, t);
                yield return null;
            }

            card.localScale = originalScale;

            onComplete?.Invoke();
        }

        /// <summary>
        /// EaseOutBack 보간 함수. 도착 지점에서 약간 넘어갔다 돌아오는 팝업 효과.
        /// </summary>
        private static float EaseOutBack(float t) { /* ... */ }
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3) + c1 * Mathf.Pow(t - 1f, 2);
        }
    }
}
