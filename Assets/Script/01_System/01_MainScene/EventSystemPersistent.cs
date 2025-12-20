using UnityEngine;
using UnityEngine.EventSystems;

public class EventSystemPersistent : MonoBehaviour
{
    void Awake()
    {
        // 이미 다른 EventSystem이 존재하면 자신을 제거
        if (FindObjectsOfType<EventSystem>().Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
    }
}