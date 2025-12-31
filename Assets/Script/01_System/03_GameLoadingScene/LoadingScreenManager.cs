using UnityEngine;
using System.Collections;

public class LoadingScreenManager : MonoBehaviour
{
    public static LoadingScreenManager Instance { get; private set; }

    [Header("Global Loading (전체 화면)")]
    public GameObject globalLoadingPanel;
    public CanvasGroup loadingCanvasGroup; // 페이드 효과용

    public bool IsLoading { get; private set; } = false;
    private Coroutine autoHideCoroutine;
    private Coroutine fadeCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            if (globalLoadingPanel != null)
            {
                globalLoadingPanel.SetActive(true);
            }

            // CanvasGroup 자동 설정
            if (loadingCanvasGroup == null && globalLoadingPanel != null)
            {
                loadingCanvasGroup = globalLoadingPanel.GetComponent<CanvasGroup>();
                if (loadingCanvasGroup == null)
                {
                    loadingCanvasGroup = globalLoadingPanel.AddComponent<CanvasGroup>();
                }
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    
    /// 전역 로딩 화면 표시 (페이드 인)
    
    public void ShowGlobalLoading()
    {
        // 기존 자동 숨김 코루틴 정지
        if (autoHideCoroutine != null)
        {
            StopCoroutine(autoHideCoroutine);
            autoHideCoroutine = null;
        }

        // 기존 페이드 코루틴 정지
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        if (globalLoadingPanel != null)
        {
            globalLoadingPanel.SetActive(true);
            IsLoading = true;

            Debug.Log("[Loading] 전역 로딩 화면 표시.");
        }
    }

    
    /// 전역 로딩 화면 숨김 (페이드 아웃)
    
    public void HideGlobalLoading()
    {
        // 기존 페이드 코루틴 정지
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        // 페이드 없이 즉시 숨김
        if (globalLoadingPanel != null)
        {
            globalLoadingPanel.SetActive(false);
        }
        IsLoading = false;

        // 안전장치 코루틴도 정지
        if (autoHideCoroutine != null)
        {
            StopCoroutine(autoHideCoroutine);
            autoHideCoroutine = null;
        }
        Debug.Log("[Loading] 전역 로딩 화면 숨김.");
    }

    
    /// 안전장치 - 일정 시간 후 강제로 로딩 숨김
    
    public void ShowGlobalLoadingWithAutoHide(float maxDuration = 3f)
    {
        ShowGlobalLoading();

        // 기존 코루틴 정지
        if (autoHideCoroutine != null)
        {
            StopCoroutine(autoHideCoroutine);
        }

        // 새 코루틴 시작
        autoHideCoroutine = StartCoroutine(AutoHideRoutine(maxDuration));
    }

    private IEnumerator AutoHideRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (IsLoading)
        {
            Debug.LogWarning($"[Loading] {delay}초 경과. 강제로 로딩 화면 숨김.");
            HideGlobalLoading();
        }
    }

    
    /// 즉시 로딩 상태 해제 (긴급용)
    
    public void ForceStopLoading()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        if (globalLoadingPanel != null)
        {
            globalLoadingPanel.SetActive(false);
        }

        if (loadingCanvasGroup != null)
        {
            loadingCanvasGroup.alpha = 0f;
        }

        IsLoading = false;

        if (autoHideCoroutine != null)
        {
            StopCoroutine(autoHideCoroutine);
            autoHideCoroutine = null;
        }

        Debug.LogWarning("[Loading] 강제로 로딩 상태 해제!");
    }
}