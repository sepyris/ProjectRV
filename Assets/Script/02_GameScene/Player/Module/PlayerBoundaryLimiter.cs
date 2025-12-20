using Definitions;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 플레이어가 'WorldBorder' 태그의 Collider2D 영역을 벗어나지 못하게 하는 모듈.
/// 플레이어의 BoxCollider2D 크기까지 고려하여 경계에서 자연스럽게 멈춤.
/// </summary>
public class PlayerBoundaryLimiter
{
    private Rigidbody2D rb;
    private Collider2D worldBorder;
    private BoxCollider2D playerCollider;

    public PlayerBoundaryLimiter(Rigidbody2D rb, BoxCollider2D playerCollider = null)
    {
        this.rb = rb;
        this.playerCollider = playerCollider;
        FindWorldBorder();

        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name.StartsWith(Def_Name.SCENE_NAME_START_MAP))
            FindWorldBorder();
    }

    private void FindWorldBorder()
    {
        GameObject borderObj = GameObject.FindGameObjectWithTag(Def_Name.WORLD_BORDER_TAG);
        if (borderObj != null)
        {
            worldBorder = borderObj.GetComponent<Collider2D>();
            if (worldBorder == null)
                Debug.LogWarning($"[PlayerBoundaryLimiter] {Def_Name.WORLD_BORDER_TAG} 태그 오브젝트에 Collider2D가 없습니다!");
        }
        else
        {
            worldBorder = null;
            Debug.LogWarning($"[PlayerBoundaryLimiter] {Def_Name.WORLD_BORDER_TAG} 태그가 없습니다!");
        }
    }

    public void ApplyBoundaryLimit()
    {
        if (rb == null || worldBorder == null)
            return;

        Bounds borderBounds = worldBorder.bounds;
        Vector2 pos = rb.position;

        // 플레이어 콜라이더 크기 고려
        Vector2 halfSize = Vector2.zero;
        Vector2 offset = Vector2.zero;

        if (playerCollider != null)
        {
            // 콜라이더 크기와 오프셋 적용
            halfSize = playerCollider.size * 0.5f;
            offset = playerCollider.offset;
        }

        // 경계 보정 (플레이어 콜라이더의 반지름만큼 여유)
        float minX = borderBounds.min.x + halfSize.x - offset.x;
        float maxX = borderBounds.max.x - halfSize.x - offset.x;
        float minY = borderBounds.min.y + halfSize.y - offset.y;
        float maxY = borderBounds.max.y - halfSize.y - offset.y;

        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);

        rb.position = pos;
    }

    public void Cleanup()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
