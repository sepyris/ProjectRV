using Steamworks; // Steamworks.NET 라이브러리 사용
using UnityEngine;

using static LocKeys;
using static DebugDisplayManager;
public class SteamManager : MonoBehaviour
{
    private static SteamManager s_Instance;
    public static bool Initialized { get; private set; } = false;
    void Awake()
    {
        // 싱글톤 패턴으로 단일 인스턴스 보장
        if (s_Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        s_Instance = this;
        DontDestroyOnLoad(gameObject);

        if (!SteamAPI.Init())
        {
            Initialized = false;
            // 2. 화면에 오류 메시지 출력
            DisplayError(STEME_CONNECT_FAIL);
        }
        else
        {
            Initialized = true;
            // 2. 화면에 성공 메시지 출력
            DisplaySuccess(STEME_CONNECT_SUCCESS);
        }
    }

    void Update()
    {
        if (Initialized)
        {
            if (SteamManager.s_Instance != null)
            {
                SteamAPI.RunCallbacks();
            }
        }
        
    }
    void OnDestroy()
    {
        if (s_Instance == this)
        {
        }
    }

    void OnApplicationQuit()
    {
        if (Initialized)
        {
            SteamAPI.Shutdown();
            // 2. 화면에 성공 메시지 출력
            DisplaySuccess(STEME_DISCONNECT);
        }
    }


}