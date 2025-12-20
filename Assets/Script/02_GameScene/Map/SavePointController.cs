using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 세이브 포인트 - 특정 오브젝트와 상호작용하여 게임 저장
/// - 저장 중 게임 일시정지
/// - 저장 전 HP 완전 회복
/// </summary>
public class SavePointController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private string savePointName = "Save Point";

    [Header("Save Settings")]
    [Tooltip("저장 중 게임 일시정지")]
    [SerializeField] private bool pauseWhileSaving = true;

    [Tooltip("저장 전 HP 회복")]
    [SerializeField] private bool healBeforeSave = true;

    [Tooltip("저장 애니메이션 시간 (초)")]
    [SerializeField] private float saveAnimationDuration = 1.5f;

    [Header("UI")]
    [SerializeField] private GameObject interactionUI;

    [Header("Effects")]
    [SerializeField] private GameObject saveEffectPrefab;
    [SerializeField] private AudioClip saveSound;

    [Header("Heal Effect")]
    [SerializeField] private GameObject healEffectPrefab;
    [SerializeField] private AudioClip healSound;

    private bool isPlayerNear = false;
    private bool isSaving = false; // 저장 중인지 체크

    private AudioSource audioSource;

    void Start()
    {
        if (interactionUI != null)
            interactionUI.SetActive(false);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (saveSound != null || healSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    void Update()
    {
        // 이제 PlayerController의 Interact 액션으로 처리됨
    }

    /// <summary>
    /// PlayerController의 Interact 액션에서 호출되는 메서드
    /// </summary>
    public void TryInteract()
    {
        if (isPlayerNear && !isSaving)
        {
            StartCoroutine(SaveGameRoutine());
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerNear = true;
            if (interactionUI != null)
                interactionUI.SetActive(true);

            Debug.Log($"[SavePoint] 플레이어 {savePointName}에 접근");
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerNear = false;
            if (interactionUI != null)
                interactionUI.SetActive(false);

            Debug.Log($"[SavePoint] 플레이어 {savePointName}에서 벗어남");
        }
    }

    /// <summary>
    /// 게임 저장 루틴 (코루틴)
    /// </summary>
    private IEnumerator SaveGameRoutine()
    {
        // 저장 시작
        isSaving = true;

        // CharacterSaveManager 확인
        if (CharacterSaveManager.Instance == null)
        {
            Debug.LogError("[SavePoint] CharacterSaveManager가 없습니다!");
            isSaving = false;
            yield break;
        }

        // 현재 캐릭터 확인
        if (CharacterSaveManager.Instance.CurrentCharacter == null)
        {
            Debug.LogWarning("[SavePoint] 선택된 캐릭터가 없습니다!");
            isSaving = false;
            yield break;
        }

        // ===== 1. 게임 일시정지 =====
        float originalTimeScale = Time.timeScale;
        if (pauseWhileSaving)
        {
            Time.timeScale = 0f;
            Debug.Log("[SavePoint] 게임 일시정지");
        }
        // ==========================

        // ===== 2. HP 회복 =====
        if (healBeforeSave)
        {
            HealPlayer();
            yield return new WaitForSecondsRealtime(0.5f); // 회복 이펙트 표시 시간
        }
        // ====================

        // ===== 3. 저장 메시지 표시 =====
        FloatingNotificationManager.Instance.ShowNotification("기록 중...");
        yield return new WaitForSecondsRealtime(0.3f);
        // =============================

        // ===== 4. 실제 저장 수행 =====
        bool success = CharacterSaveManager.Instance.SaveCurrentCharacterGameData();
        // ==========================

        // ===== 5. 저장 애니메이션 대기 =====
        yield return new WaitForSecondsRealtime(saveAnimationDuration - 0.3f);
        // =================================

        // ===== 6. 결과 처리 =====
        if (success)
        {
            string characterName = CharacterSaveManager.Instance.CurrentCharacter.stats.characterName;

            Debug.Log($"[SavePoint] 게임 저장 완료: {savePointName} - {characterName}");

            // 성공 피드백
            FloatingNotificationManager.Instance.ShowNotification("기록을 완료하였습니다.");
            PlaySaveEffect();
            PlaySaveSound();
        }
        else
        {
            Debug.LogWarning("[SavePoint] 게임 저장 실패");
            FloatingNotificationManager.Instance.ShowNotification("기록 실패!");
        }
        // ======================

        // ===== 7. 게임 재개 =====
        if (pauseWhileSaving)
        {
            yield return new WaitForSecondsRealtime(0.5f); // 메시지 읽을 시간
            Time.timeScale = originalTimeScale;
            Debug.Log("[SavePoint] 게임 재개");
        }
        // ======================

        isSaving = false;
    }

    /// <summary>
    /// 플레이어 HP 회복
    /// </summary>
    private void HealPlayer()
    {
        if (PlayerController.Instance == null) return;

        var playerStats = PlayerController.Instance.GetComponent<PlayerStatsComponent>();
        if (playerStats != null && playerStats.Stats != null)
        {
            int healAmount = playerStats.Stats.maxHP - playerStats.Stats.currentHP;

            if (healAmount > 0)
            {
                playerStats.Stats.FullRecover();
                Debug.Log($"[SavePoint] 플레이어 HP 회복: {healAmount}");

                // 회복 이펙트
                PlayHealEffect();
                PlayHealSound();

                FloatingNotificationManager.Instance?.ShowNotification($"HP 회복 +{healAmount}");
            }
        }
    }

    /// <summary>
    /// 회복 이펙트 재생
    /// </summary>
    private void PlayHealEffect()
    {
        if (healEffectPrefab != null && PlayerController.Instance != null)
        {
            GameObject effect = Instantiate(healEffectPrefab, PlayerController.Instance.transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }
    }

    /// <summary>
    /// 회복 사운드 재생
    /// </summary>
    private void PlayHealSound()
    {
        if (audioSource != null && healSound != null)
        {
            audioSource.PlayOneShot(healSound);
        }
    }

    /// <summary>
    /// 저장 완료 이펙트 재생
    /// </summary>
    private void PlaySaveEffect()
    {
        if (saveEffectPrefab != null)
        {
            GameObject effect = Instantiate(saveEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }
    }

    /// <summary>
    /// 저장 사운드 재생
    /// </summary>
    private void PlaySaveSound()
    {
        if (audioSource != null && saveSound != null)
        {
            audioSource.PlayOneShot(saveSound);
        }
    }

    /// <summary>
    /// Gizmo로 세이브 포인트 위치 표시 (에디터에서만)
    /// </summary>
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 1f);

#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f, savePointName);
#endif
    }

    // ===== 추가: 외부에서 저장 가능하도록 =====
    /// <summary>
    /// 외부에서 저장 실행 (UI 버튼 등)
    /// </summary>
    public void TriggerSave()
    {
        if (!isSaving)
        {
            StartCoroutine(SaveGameRoutine());
        }
    }
    // ========================================
}