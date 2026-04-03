// ChipAnimator.cs
// 칩 이동 애니메이션을 담당하는 유틸리티 클래스.
// 코루틴 기반으로 플레이어 베팅 위치에서 팟으로, 팟에서 승자 위치로의 칩 이동 애니메이션을 제공한다.
// 애니메이션 완료 시 칩 오브젝트를 자동 파괴하는 콜백을 포함한다.
// MonoBehaviour 인스턴스가 필요하며, StartCoroutine 호출을 위해 호출자의 MonoBehaviour를 전달받는다.

using System;
using System.Collections;
using UnityEngine;

namespace TexasHoldem.View
{
    public static class ChipAnimator
    {
        /// <summary>
        /// 칩을 플레이어 베팅 위치에서 팟 위치로 이동시킨 후 페이드아웃하는 애니메이션을 실행한다.
        /// 애니메이션 완료 시 칩 오브젝트를 자동 파괴한다.
        /// </summary>
        /// <param name="runner">코루틴 실행을 위한 MonoBehaviour</param>
        /// <param name="chipIcon">애니메이션 대상 칩 Transform</param>
        /// <param name="fromPosition">시작 위치 (플레이어 베팅 위치)</param>
        /// <param name="potPosition">도착 위치 (팟 위치)</param>
        /// <param name="duration">애니메이션 시간(초)</param>
        /// <param name="onComplete">완료 시 콜백 (자동 파괴 전 호출)</param>
        /// <returns>실행된 코루틴</returns>
        public static Coroutine AnimateChipToPot(MonoBehaviour runner, Transform chipIcon, Vector3 fromPosition, Vector3 potPosition, float duration = 0.35f, Action onComplete = null)
        {
            return runner.StartCoroutine(ChipToPotCoroutine(chipIcon, fromPosition, potPosition, duration, onComplete));
        }

        /// <summary>
        /// 칩을 팟 위치에서 승자 위치로 이동시키는 애니메이션을 실행한다.
        /// 애니메이션 완료 시 칩 오브젝트를 자동 파괴한다.
        /// </summary>
        /// <param name="runner">코루틴 실행을 위한 MonoBehaviour</param>
        /// <param name="chipIcon">애니메이션 대상 칩 Transform</param>
        /// <param name="potPosition">시작 위치 (팟 위치)</param>
        /// <param name="toPosition">도착 위치 (승자 위치)</param>
        /// <param name="duration">애니메이션 시간(초)</param>
        /// <param name="onComplete">완료 시 콜백 (자동 파괴 전 호출)</param>
        /// <returns>실행된 코루틴</returns>
        public static Coroutine AnimateChipToPlayer(MonoBehaviour runner, Transform chipIcon, Vector3 potPosition, Vector3 toPosition, float duration = 0.35f, Action onComplete = null)
        {
            return runner.StartCoroutine(ChipToPlayerCoroutine(chipIcon, potPosition, toPosition, duration, onComplete));
        }

        /// <summary>
        /// 칩을 플레이어 위치에서 팟으로 이동 후 페이드아웃하는 코루틴.
        /// EaseInQuad 보간으로 자연스러운 가속 이동을 제공한다.
        /// </summary>
        public static IEnumerator ChipToPotCoroutine(Transform chipIcon, Vector3 fromPosition, Vector3 potPosition, float duration = 0.35f, Action onComplete = null)
        {
            chipIcon.position = fromPosition;

            SpriteRenderer spriteRenderer = chipIcon.GetComponent<SpriteRenderer>();
            Color originalColor = spriteRenderer != null ? spriteRenderer.color : Color.white;

            float moveDuration = duration * 0.7f;
            float fadeDuration = duration * 0.3f;

            // 이동 단계
            float elapsed = 0f;
            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / moveDuration);
                float eased = EaseInQuad(t);

                chipIcon.position = Vector3.Lerp(fromPosition, potPosition, eased);

                yield return null;
            }

            chipIcon.position = potPosition;

            // 페이드아웃 단계
            if (spriteRenderer != null)
            {
                elapsed = 0f;
                while (elapsed < fadeDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / fadeDuration);

                    Color fadedColor = originalColor;
                    fadedColor.a = Mathf.Lerp(1f, 0f, t);
                    spriteRenderer.color = fadedColor;

                    yield return null;
                }

                spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
            }

            onComplete?.Invoke();
            UnityEngine.Object.Destroy(chipIcon.gameObject);
        }

        /// <summary>
        /// 칩을 팟에서 승자 위치로 이동하는 코루틴.
        /// EaseOutQuad 보간으로 자연스러운 감속 이동을 제공한다.
        /// </summary>
        public static IEnumerator ChipToPlayerCoroutine(Transform chipIcon, Vector3 potPosition, Vector3 toPosition, float duration = 0.35f, Action onComplete = null)
        {
            chipIcon.position = potPosition;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = EaseOutQuad(t);

                chipIcon.position = Vector3.Lerp(potPosition, toPosition, eased);

                yield return null;
            }

            chipIcon.position = toPosition;

            onComplete?.Invoke();
            UnityEngine.Object.Destroy(chipIcon.gameObject);
        }

        /// <summary>
        /// EaseInQuad 보간 함수. 시작이 느리고 끝이 빠른 가속 효과.
        /// </summary>
        private static float EaseInQuad(float t)
        {
            return t * t;
        }

        /// <summary>
        /// EaseOutQuad 보간 함수. 시작이 빠르고 끝이 느린 감속 효과.
        /// </summary>
        private static float EaseOutQuad(float t)
        {
            return 1f - (1f - t) * (1f - t);
        }
    }
}
