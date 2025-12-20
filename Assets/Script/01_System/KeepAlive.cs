using UnityEngine;

public class KeepAlive : MonoBehaviour
{
    public static KeepAlive Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject); // 중복 제거
        }
    }
}
