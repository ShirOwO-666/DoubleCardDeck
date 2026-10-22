using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardAttackAnimator : MonoBehaviour
{
    // 撞击的距离（沿 Z 轴，根据你的摄像机视角，如果是向上冲就改 Y 轴）
    public float thrustDistance = 0.5f;
    public float thrustDuration = 0.15f;

    private Vector3 originalPosition;
    private bool isAnimating = false;

    public void PlayAttackAnimation(Vector3 direction)
    {
        if (isAnimating) return;
        StartCoroutine(ThrustRoutine(direction));
    }

    private IEnumerator ThrustRoutine(Vector3 direction)
    {
        isAnimating = true;
        originalPosition = transform.position;

        // 1. 向前冲
        Vector3 targetPos = originalPosition + direction * thrustDistance;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / thrustDuration;
            transform.position = Vector3.Lerp(originalPosition, targetPos, t);
            yield return null;
        }

        // 2. 停顿一下（打击感）
        yield return new WaitForSeconds(0.05f);

        // 3. 弹回原位
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / thrustDuration;
            transform.position = Vector3.Lerp(targetPos, originalPosition, t);
            yield return null;
        }

        transform.position = originalPosition;
        isAnimating = false;
    }
}