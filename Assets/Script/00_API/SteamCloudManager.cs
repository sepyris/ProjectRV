// SteamCloudManager.cs
using Steamworks;
using UnityEngine;

public class SteamCloudManager : MonoBehaviour
{
    // 콜백 핸들러 정의
    protected Callback<RemoteStorageFileWriteAsyncComplete_t> m_FileWriteAsyncComplete;

    void Start()
    {
        // 콜백 등록
        m_FileWriteAsyncComplete = Callback<RemoteStorageFileWriteAsyncComplete_t>.Create(OnFileWriteAsyncComplete);
    }

    //  추가: 콜백 해제 (메모리 누수 방지)
    void OnDestroy()
    {
        if (m_FileWriteAsyncComplete != null)
        {
            m_FileWriteAsyncComplete.Dispose();
            m_FileWriteAsyncComplete = null;
        }
    }

    // 파일 쓰기 완료 시 호출되는 함수
    private void OnFileWriteAsyncComplete(RemoteStorageFileWriteAsyncComplete_t pCallback)
    {
        if (pCallback.m_eResult == EResult.k_EResultOK)
        {
            Debug.Log("클라우드 동기화 성공!");
        }
        else
        {
            Debug.LogError($"클라우드 동기화 실패: Result: {pCallback.m_eResult}");
        }
    }

    // SecureSaveLoad에서 파일 저장 후 이 함수를 호출하여 클라우드에 업로드 요청
    public static void InitiateCloudSave(string fileName, byte[] data)
    {
        if (data == null || data.Length == 0)
        {
            Debug.LogError("[SteamCloud] 저장할 데이터가 없습니다.");
            return;
        }

        //  추가: 스팀 초기화 확인
        if (!SteamManager.Initialized)
        {
            Debug.LogWarning("[SteamCloud] Steam이 초기화되지 않아 클라우드 저장을 건너뜁니다.");
            return;
        }

        SteamRemoteStorage.FileWriteAsync(fileName, data, (uint)data.Length);
        Debug.Log($"[SteamCloud] 클라우드 업로드 요청: {fileName} ({data.Length} bytes)");
    }
}