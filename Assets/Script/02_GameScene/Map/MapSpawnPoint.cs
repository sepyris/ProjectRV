using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MapSpawnPoint : MonoBehaviour
{
    
    [Header("Spawn Point Info")]
    public string spawnPointid;
    public string nextMapid;

    [Header("UI Settings")]
    [SerializeField] private Vector3 uiWorldOffset = Vector3.up * 0.3f;

    private void Start()
    {
        if(GetComponent<SpriteRenderer>() != null)
        {
            GetComponent<SpriteRenderer>().enabled = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (MapInfoUIManager.Instance != null && !string.IsNullOrEmpty(nextMapid))
            {
                Vector3 uiWorldPosition = transform.position + uiWorldOffset;
                MapInfoUIManager.Instance.SetMapInfo(nextMapid, uiWorldPosition);
            }
        }
    }

    // 충돌 중일 때도 위치 계속 업데이트
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (MapInfoUIManager.Instance != null && !string.IsNullOrEmpty(nextMapid))
            {
                Vector3 uiWorldPosition = transform.position + uiWorldOffset;
                MapInfoUIManager.Instance.SetWorldPosition(uiWorldPosition);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (MapInfoUIManager.Instance != null)
            {
                MapInfoUIManager.Instance.ClearMapInfo();
            }
        }
    }

    private void OnDrawGizmos()
    {
#if UNITY_EDITOR
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.7f);

        GUIStyle style = new GUIStyle();
        style.fontSize = 30;
        style.normal.textColor = Color.black;
        style.alignment = TextAnchor.MiddleCenter;
        Handles.Label(transform.position + Vector3.up * 0.5f, spawnPointid, style);
#endif
    }
}