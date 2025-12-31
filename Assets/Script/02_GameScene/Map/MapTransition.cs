using Definitions;
using UnityEngine;
using UnityEngine.EventSystems;


/// 맵 전환 트리거
/// MapLoadingScene을 통해 맵 이동

public class MapTransition : MonoBehaviour, IPointerDownHandler
{
    [Header("Transition Settings")]
    [SerializeField] private string targetMapId;  // targetSceneId → targetMapId로 변경
    [SerializeField] private string targetSpawnPointid;

    [Header("Visual Feedback")]
    [SerializeField] private GameObject transitionEffect;

    private bool debug_ini_flag = true;

    
    /// UI 클릭으로 전환 (미니맵 등)
    
    public void OnPointerDown(PointerEventData eventData)
    {
        GoToNewScene();
    }

    
    /// Collider 트리거로 전환 (포탈, 문 등)
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(Def_Name.PLAYER_TAG) || other.GetComponentInParent<PlayerController>() != null)
        {
            GoToNewScene();
        }
    }

    
    /// 맵 전환 실행
    
    private void GoToNewScene()
    {
        // 맵 ID로부터 실제 씬 이름 생성
        string targetSceneName = MapInfoManager.Instance.GetSceneName(targetMapId);

        // 유효성 검사
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogError($"[MapTransition] 맵 ID '{targetMapId}'로부터 씬 이름을 생성할 수 없습니다!");
            return;
        }

        if (string.IsNullOrEmpty(targetSpawnPointid))
        {
            Debug.LogWarning("[MapTransition] 스폰 포인트 id가 설정되지 않았습니다. 기본 위치로 이동합니다.");
        }

        Debug.Log($"[MapTransition] 맵 전환: MapID={targetMapId} → SceneName={targetSceneName} (Spawn: {targetSpawnPointid})");

        // 전환 이펙트 재생
        if (transitionEffect != null)
        {
            Instantiate(transitionEffect, transform.position, Quaternion.identity);
        }

        // 캐릭터 상태 저장 (선택사항)
        SavePlayerState();

        MapLoadingManager.LoadMap(targetSceneName, targetSpawnPointid);
    }

    
    /// 플레이어 상태 저장
    
    private void SavePlayerState()
    {
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            player.SaveStateBeforeDeactivation();
            Debug.Log("[MapTransition] 플레이어 상태 저장 완료");
        }
    }

    
    /// 외부에서 맵 전환 호출 (코드로 전환할 때)
    
    public void TriggerTransition()
    {
        GoToNewScene();
    }

    
    /// Gizmo로 전환 지점 표시 (에디터에서만)
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(transform.position, new Vector2(transform.localScale.x, transform.localScale.y));
#if UNITY_EDITOR
        if(debug_ini_flag)
        {
            // 맵 ID로부터 씬 이름 생성해서 표시
            string sceneName = MapInfoManager.Instance?.GetSceneName(targetMapId) ?? "Unknown Scene";
            string mapName = MapInfoManager.Instance?.GetMapName(targetMapId) ?? "Unknown Map";

            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 2f,
                $"→ {mapName}\n씬: {sceneName}\n스폰: [{targetSpawnPointid}]"
            );
            debug_ini_flag = false;
        }
        
#endif
    }
}