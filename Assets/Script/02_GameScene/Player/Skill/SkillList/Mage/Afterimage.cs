using UnityEngine;
using System.Collections;

public class Afterimage : ActiveSkillBase
{
    private const string EFFECT_PATH = "Effects/TeleportEffect";
    private const float DASH_DISTANCE = 6f;
    private const float DASH_DURATION = 0.15f;  // 매우 빠름
    private const float AFTERIMAGE_DURATION = 2f;  // 잔상 지속시간

    public Afterimage(SkillData data, int level = 1) : base(data, level)
    {
    }

    protected override void Execute(Transform caster, Vector3 targetPosition, Transform targetTransform)
    {
        PlayerController player = caster.GetComponent<PlayerController>();
        PlayerMovement movement = player?.GetComponent<PlayerController>()?.movement;

        if (player == null)
        {
            Debug.LogError("[Afterimage] PlayerController 없음");
            return;
        }

        // 바라보는 방향 가져오기
        Vector2 dashDirection;
        if (movement != null)
        {
            dashDirection = movement.LastMoveDirection;
        }
        else
        {
            dashDirection = Vector2.right;  // fallback
        }

        // 원래 위치 저장
        Vector3 originalPosition = caster.position;

        // 원래 위치에 잔상 생성
        if (player is MonoBehaviour mono)
        {
            CreateAfterimage(caster, originalPosition, AFTERIMAGE_DURATION, mono);
        }

        // 시작 이펙트
        SpawnEffect(EFFECT_PATH, originalPosition, Quaternion.identity);

        // 대쉬 실행
        player.Dash(
            dashDirection,
            DASH_DISTANCE,
            DASH_DURATION,
            null,  // 충돌 체크 없음
            () => {
                // 도착 위치 이펙트
                SpawnEffect(EFFECT_PATH, caster.position, Quaternion.identity);
            }
        );

        Debug.Log($"[Afterimage] 잔상 생성! 방향: {dashDirection}");
    }

    /// <summary>
    /// 잔상 생성
    /// </summary>
    private void CreateAfterimage(Transform caster, Vector3 position, float duration, MonoBehaviour runner)
    {
        SpriteRenderer playerSprite = caster.GetComponent<SpriteRenderer>();

        if (playerSprite == null)
        {
            Debug.LogWarning("[Afterimage] SpriteRenderer 없음");
            return;
        }

        // 잔상 오브젝트 생성
        GameObject afterimageObj = new GameObject("Afterimage");
        afterimageObj.transform.position = position;
        afterimageObj.transform.rotation = caster.rotation;
        afterimageObj.transform.localScale = caster.localScale;

        // 스프라이트 복사
        SpriteRenderer afterimageSprite = afterimageObj.AddComponent<SpriteRenderer>();
        afterimageSprite.sprite = playerSprite.sprite;
        afterimageSprite.flipX = playerSprite.flipX;
        afterimageSprite.sortingLayerName = playerSprite.sortingLayerName;
        afterimageSprite.sortingOrder = playerSprite.sortingOrder - 1;  // 플레이어 뒤에

        // 반투명 시작
        Color startColor = playerSprite.color;
        startColor.a = 0.6f;
        afterimageSprite.color = startColor;

        // 페이드 아웃 코루틴 시작
        runner.StartCoroutine(FadeOutAfterimage(afterimageSprite, duration));

        Debug.Log($"[Afterimage] 잔상 생성: {duration}초 동안 유지");
    }

    /// <summary>
    /// 잔상 페이드 아웃
    /// </summary>
    private IEnumerator FadeOutAfterimage(SpriteRenderer sprite, float duration)
    {
        float elapsed = 0f;
        Color startColor = sprite.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // 서서히 투명해짐
            Color newColor = startColor;
            newColor.a = Mathf.Lerp(0.6f, 0f, t);
            sprite.color = newColor;

            yield return null;
        }

        // 잔상 제거
        Object.Destroy(sprite.gameObject);
        Debug.Log("[Afterimage] 잔상 소멸");
    }
}