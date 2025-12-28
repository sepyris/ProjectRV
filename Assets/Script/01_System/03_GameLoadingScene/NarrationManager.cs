using Definitions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 나레이션 시스템 전체 관리
/// - 대화 데이터는 DialogueDataManager에서 가져옴
/// - ESC로 닫히지 않음
/// - 플레이어 이동/조작 가능
/// - F키 1.5초 홀드로 스킵
/// - 타이핑 효과 지원
/// </summary>
public class NarrationManager : MonoBehaviour
{
    public static NarrationManager Instance { get; private set; }

    [Header("UI 참조")]
    [SerializeField] private NarrationUI narrationUI;

    [Header("입력 설정")]
    private PlayerControls playerControls;

    [Header("상태")]
    private bool isNarrationActive = false;
    private NarrationConfig currentConfig;
    private List<DialogueLine> currentLines;
    private int currentLineIndex = 0;
    private Coroutine narrationCoroutine;

    // F키 홀드 스킵 관련
    private float skipHoldTime = 0f;
    private bool isSkipHolding = false;

    // 조건 충족 플래그
    private bool conditionMet = false;

    public bool IsNarrationActive => isNarrationActive;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // PlayerControls 초기화
        playerControls = new PlayerControls();
    }

    private void OnEnable()
    {
        playerControls?.Enable();
    }

    private void OnDisable()
    {
        playerControls?.Disable();
    }

    private void OnDestroy()
    {
        if (playerControls != null)
        {
            playerControls.Dispose();
            playerControls = null;
        }
    }

    private void Update()
    {
        if (!isNarrationActive) return;

        // F키 홀드 스킵 처리
        if (currentConfig != null && currentConfig.canSkip)
        {
            HandleSkipInput();
        }

        // Conditional 모드일 때 조건 체크
        if (currentConfig != null && currentConfig.mode == NarrationMode.Conditional)
        {
            CheckCondition();
        }
    }

    /// <summary>
    /// F키 홀드 스킵 입력 처리
    /// </summary>
    private void HandleSkipInput()
    {
        float interactValue = playerControls.Player.Interact.ReadValue<float>();
        bool isHolding = interactValue > 0.1f;

        if (isHolding)
        {
            if (!isSkipHolding)
            {
                isSkipHolding = true;
                skipHoldTime = 0f;
            }

            skipHoldTime += Time.deltaTime;
            float progress = Mathf.Clamp01(skipHoldTime / currentConfig.skipHoldDuration);

            // UI 진행바 업데이트
            narrationUI.UpdateSkipProgress(progress);

            // 홀드 완료
            if (skipHoldTime >= currentConfig.skipHoldDuration)
            {
                narrationUI.HideProgress();
                Debug.Log("[NarrationManager] 스킵 완료");
                StopNarration();
                return;
            }
        }
        else
        {
            // F키를 뗐을 때
            if (isSkipHolding)
            {
                isSkipHolding = false;
                skipHoldTime = 0f;
                narrationUI.UpdateSkipProgress(0f);
            }
        }
    }

    /// <summary>
    /// 나레이션 재생 시작
    /// </summary>
    public void PlayNarration(string narrationId, NarrationConfig config)
    {
        if (isNarrationActive)
        {
            Debug.LogWarning("[NarrationManager] 이미 나레이션이 재생 중입니다.");
            return;
        }

        // DialogueDataManager에서 데이터 가져오기
        var lines = DialogueDataManager.Instance.GetDialogueSequence(
            narrationId,
            Def_Dialogue.TYPE_NARRATION
        );

        if (lines == null || lines.Count == 0)
        {
            Debug.LogError($"[NarrationManager] 나레이션 데이터를 찾을 수 없습니다: {narrationId}");
            return;
        }

        currentConfig = config;
        currentLines = lines;
        currentLineIndex = 0;
        isNarrationActive = true;
        skipHoldTime = 0f;
        isSkipHolding = false;

        Debug.Log($"[NarrationManager] 나레이션 시작: {narrationId} ({lines.Count}개 대사)");

        narrationCoroutine = StartCoroutine(RunNarrationSequence());
    }

    /// <summary>
    /// 나레이션 시퀀스 실행
    /// </summary>
    private IEnumerator RunNarrationSequence()
    {
        while (currentLineIndex < currentLines.Count)
        {
            if (!isNarrationActive) yield break;

            var line = currentLines[currentLineIndex];
            narrationUI.Show(line.Text, currentConfig);
            //나레이션 UI설정될때까지 조금 대기
            yield return new WaitForSeconds(1f);

            // 모드에 따라 대기
            if (currentConfig.mode == NarrationMode.Auto)
            {
                Debug.Log($"[NarrationManager] 나레이션 중: {currentConfig.narrationId} ({currentLines.Count}개 대사)");
                // 타이핑 효과가 완료될 때까지 대기
                yield return new WaitUntil(() =>
                    narrationUI.IsTypingComplete() || !isNarrationActive
                );

                if (!isNarrationActive) yield break;
                Debug.Log($"타이핑완료");
                // 타이핑 완료 후 추가 대기
                yield return new WaitForSeconds(currentConfig.delayAfterTyping);
            }
            else if (currentConfig.mode == NarrationMode.Conditional)
            {
                // 타이핑 완료 대기
                yield return new WaitUntil(() =>
                    narrationUI.IsTypingComplete() || !isNarrationActive
                );

                if (!isNarrationActive) yield break;

                // 조건 충족 대기
                conditionMet = false;
                yield return new WaitUntil(() => conditionMet || !isNarrationActive);

                if (!isNarrationActive) yield break;
            }

            currentLineIndex++;
        }
        isNarrationActive = false;

        //튜토리얼 나레이션 종료
        if(currentConfig.narrationId == "System_End")
        {
            TutorialManager.CheckLastNarration();
            narrationUI.HidePanel(instant: true);
        }
    }

    /// <summary>
    /// 나레이션 중단
    /// </summary>
    public void StopNarration()
    {
        isNarrationActive = false;
        currentLines = null;
        currentLineIndex = 0;
        skipHoldTime = 0f;
        isSkipHolding = false;

        //TODO 현재는 튜토리얼에서만 사용하기에 튜토리얼내용만 사용
        //스킵 혹은 마지막이면 마지막 나레이션이 나오도록 함
        var config = new NarrationConfig()
        {
            narrationId = "System_End",
            mode = NarrationMode.Auto,
            conditionType = NarrationConditionType.None,
            conditionData = "System_End",
            typingSpeed = 0.15f,
            canSkip = false,
        };
        //
        PlayNarration("System_End", config);
    }

    /// <summary>
    /// 조건 충족 여부 체크
    /// </summary>
    private void CheckCondition()
    {
        if (currentConfig == null || conditionMet) return;

        switch (currentConfig.conditionType)
        {
            case NarrationConditionType.Move:
                // 플레이어가 이동 중인지 체크
                var moveInput = playerControls.Player.Move.ReadValue<Vector2>();
                if (moveInput.sqrMagnitude > 0.01f)
                {
                    conditionMet = true;
                    Debug.Log("[NarrationManager] 조건 충족: 이동");
                }
                break;

            case NarrationConditionType.OpenInventory:
                // 인벤토리 열기 체크
                if (playerControls.Player.ToggleInventory.triggered)
                {
                    conditionMet = true;
                    Debug.Log("[NarrationManager] 조건 충족: 인벤토리 열기");
                }
                break;
            case NarrationConditionType.OpenEquipment:
                // 인벤토리 열기 체크
                if (playerControls.Player.ToggleEquipment.triggered)
                {
                    conditionMet = true;
                    Debug.Log("[NarrationManager] 조건 충족: 장비 열기");
                }
                break;
            case NarrationConditionType.OpenQuest:
                // 인벤토리 열기 체크
                if (playerControls.Player.ToggleQuest.triggered)
                {
                    conditionMet = true;
                    Debug.Log("[NarrationManager] 조건 충족: 퀘스트 열기");
                }
                break;
            case NarrationConditionType.OpenStat:
                // 인벤토리 열기 체크
                if (playerControls.Player.ToggleStats.triggered)
                {
                    conditionMet = true;
                    Debug.Log("[NarrationManager] 조건 충족: 스텟 열기");
                }
                break;
        }
    }

    /// <summary>
    /// 외부에서 조건 충족을 알림 (아이템 획득, NPC 대화 등)
    /// </summary>
    public void TriggerCondition(NarrationConditionType conditionType, string data = "")
    {
        if (!isNarrationActive || currentConfig == null) return;

        if (currentConfig.conditionType == conditionType)
        {
            // conditionData가 있으면 비교
            if (!string.IsNullOrEmpty(currentConfig.conditionData))
            {
                if (currentConfig.conditionData == data)
                {
                    conditionMet = true;
                    Debug.Log($"[NarrationManager] 조건 충족: {conditionType} ({data})");
                }
            }
            else
            {
                conditionMet = true;
                Debug.Log($"[NarrationManager] 조건 충족: {conditionType}");
            }
        }
    }

    public bool IsNarrationCompleted(NarrationConditionType conditionType, string data = "")
    {
        if (currentConfig == null) return true;
        return !isNarrationActive && (currentConfig.conditionType == conditionType 
                                      && (string.IsNullOrEmpty(currentConfig.conditionData) || 
                                          currentConfig.conditionData == data));
    }

    /// <summary>
    /// 간편 호출: narrationId만으로 기본 설정 사용
    /// </summary>
    public void PlayNarration(string narrationId)
    {
        var config = new NarrationConfig
        {
            narrationId = narrationId,
            mode = NarrationMode.Auto,
            useTypingEffect = true,
            typingSpeed = 0.05f,
            delayAfterTyping = 1.5f,
            canSkip = false,
            skipHoldDuration = 1.5f,
            conditionData = "Basic",
        };

        PlayNarration(narrationId, config);
    }
}