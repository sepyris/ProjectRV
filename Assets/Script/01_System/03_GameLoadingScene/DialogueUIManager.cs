using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Definitions;


/// 대화 및 퀘스트 선택 UI 전체 관리

public class DialogueUIManager : MonoBehaviour, IClosableUI
{
    public static DialogueUIManager Instance { get; private set; }

    [Header("기본 대화 UI")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI speakerNameText;
    public TextMeshProUGUI dialogueText;

    [Header("버튼 / 진행 관련")]
    public GameObject nextButtonPanel;
    public Button nextButton; //  Button 컴포넌트 추가
    public Sprite continueSprite;
    public Sprite closeSprite;

    [Header("퀘스트 선택 UI")]
    public GameObject questSelectionPanel;
    public Transform questListContainer;
    public GameObject questButtonPrefab;
    public Button closeSelectionButton;

    [Header("퀘스트 수락/거절 패널")]
    public GameObject questChoicePanel;
    public Button acceptButton;
    public Button declineButton;

    [Header("오디오")]
    public AudioSource audioSource;
    public AudioClip dialogueAdvanceClip;

    private NPCController currentNPC;
    private string currentQuestId;
    private bool isDialogueActive = false;
    private PlayerControls playerControls; // Input Actions 참조
    public bool IsDialogueOpen => isDialogueActive;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
        dialoguePanel.SetActive(false);
        if (closeSelectionButton != null)
        {
            closeSelectionButton.onClick.AddListener(CloseInteraction);
        }
    }

    void OnDestroy()
    {
        if (playerControls != null)
        {
            playerControls.Dispose();
            playerControls = null;
        }
    }

    // ==========================================
    // NPC 상호작용 진입
    // ==========================================
    public void OpenInteraction(NPCController npc)
    {
        if (isDialogueActive || npc == null) return;

        //  이전 대화 상태 완전 초기화
        currentQuestId = null;

        //  퀘스트 목록의 모든 버튼 제거
        if (questListContainer != null)
        {
            foreach (Transform child in questListContainer)
            {
                Destroy(child.gameObject);
            }
        }

        nextButtonPanel.SetActive(false);
        questChoicePanel.SetActive(false);
        questSelectionPanel.SetActive(false);

        //  버튼 리스너 정리
        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
        }

        currentNPC = npc;
        isDialogueActive = true;
        

        // PlayerControls 참조 가져오기
        if (playerControls == null)
        {
            playerControls = new PlayerControls();
            playerControls.Enable();
        }

        if (PlayerController.Instance != null)
            PlayerController.Instance.SetActionControlsLocked(true);
        PlayerHUD.Instance.RegisterUI(this);

        var normalDialogue = DialogueDataManager.Instance.GetDialogueSequence(npc.npcId, Def_Dialogue.TYPE_NORMAL);
        if (normalDialogue != null && normalDialogue.Count > 0)
        {
            StartCoroutine(RunDialogueSequence(normalDialogue, showChoicesAfter: true, questId: null));
        }
        else
        {
            ShowQuestOrDailySelection(npc);
        }
    }

    // ==========================================
    // 대화 시퀀스 실행
    // ==========================================
    private void StartDialogueSequence(string npcId, string type, bool showChoicesAfter, string questId)
    {
        List<DialogueLine> sequence;
        if (!string.IsNullOrEmpty(questId))
        {
            sequence = DialogueDataManager.Instance.GetDialogueSequence(npcId, type, questId);
            Debug.Log($"[DialogueUI] 대화 검색: NPC={npcId}, Type={type}, QuestId={questId}");
        }
        else
        {
            sequence = DialogueDataManager.Instance.GetDialogueSequence(npcId, type);
            Debug.Log($"[DialogueUI] 대화 검색: NPC={npcId}, Type={type}");
        }

        if (sequence != null && sequence.Count > 0)
        {
            StartCoroutine(RunDialogueSequence(sequence, showChoicesAfter, questId));
        }
        else
        {
            Debug.LogWarning($"{Def_Dialogue.DIALOGUE_NOT_FOUND} NPC: {npcId}, Type: {type}, QuestId: {questId}");
            CloseInteraction();
        }
    }

    private IEnumerator RunDialogueSequence(List<DialogueLine> lines, bool showChoicesAfter, string questId)
    {
        //  시작 전 모든 패널 명확하게 초기화
        nextButtonPanel.SetActive(false);
        questChoicePanel.SetActive(false);
        questSelectionPanel.SetActive(false);

        dialoguePanel.SetActive(true);

        for (int i = 0; i < lines.Count; i++)
        {
            //  코루틴이 중단되었는지 체크 (ESC 대응)
            if (!isDialogueActive || currentNPC == null)
            {
                yield break;
            }

            var line = lines[i];

            //  NPC 이름을 NPCInfoManager에서 가져오기 
            if (currentNPC != null)
            {
                speakerNameText.text = line.GetSpeakerName(currentNPC.npcId);
            }

            dialogueText.text = line.Text;

            bool isLastLine = (i == lines.Count - 1);
            nextButton.image.sprite = isLastLine && !showChoicesAfter ? closeSprite : continueSprite;

            nextButtonPanel.SetActive(true);

            yield return StartCoroutine((WaitForAdvanceKey()));

            //  각 대화 후 버튼 패널 비활성화
            nextButtonPanel.SetActive(false);
        }

        //  코루틴 종료 전 다시 한 번 체크
        if (!isDialogueActive || currentNPC == null)
        {
            yield break;
        }

        if (showChoicesAfter)
        {
            if (!string.IsNullOrEmpty(questId))
            {
                ShowQuestChoicePanel(questId);
            }
            else
            {
                ShowQuestOrDailySelection(currentNPC, questId);
            }
        }
        else
        {
            CloseInteraction();
        }
    }
    private IEnumerator WaitForAdvanceKey()
    {
        yield return new WaitForSeconds(0.2f);

        //  E키(Interact)와 버튼 클릭 모두 대기
        bool advanceTriggered = false;

        //  기존 리스너 먼저 정리
        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
        }

        // E키가 눌려있으면 떼질 때까지 대기
        while (playerControls != null && playerControls.Player.Interact.ReadValue<float>() > 0.1f)
        {
            yield return null;
        }

        // 버튼 클릭 리스너 등록
        if (nextButton != null)
        {
            nextButton.onClick.AddListener(() =>
            {
                advanceTriggered = true;
                if (audioSource && dialogueAdvanceClip)
                    audioSource.PlayOneShot(dialogueAdvanceClip);
            });
        }

        // E키 입력 또는 버튼 클릭 대기
        while (!advanceTriggered)
        {
            //  대화가 비활성화되면 코루틴 종료 (ESC 대응)
            if (!isDialogueActive)
            {
                if (nextButton != null)
                    nextButton.onClick.RemoveAllListeners();
                yield break;
            }

            // Interact 액션으로 진행 가능
            if (playerControls != null && playerControls.Player.Interact.triggered)
            {
                advanceTriggered = true;
                if (audioSource && dialogueAdvanceClip)
                    audioSource.PlayOneShot(dialogueAdvanceClip);
            }
            yield return null;
        }

        // 리스너 정리
        if (nextButton != null)
            nextButton.onClick.RemoveAllListeners();
    }

    private IEnumerator RefreshLayout()
    {
        yield return null;
        LayoutRebuilder.ForceRebuildLayoutImmediate(questListContainer as RectTransform);
        Canvas.ForceUpdateCanvases();
    }

    // ==========================================
    // 퀘스트 수락/거절 패널
    // ==========================================
    private void ShowQuestChoicePanel(string questId)
    {
        Debug.Log($"[DialogueUI] ShowQuestChoicePanel 호출: {questId}");
        currentQuestId = questId;

        questChoicePanel.SetActive(true);

        acceptButton.onClick.RemoveAllListeners();
        declineButton.onClick.RemoveAllListeners();

        acceptButton.onClick.AddListener(OnAcceptQuest);
        declineButton.onClick.AddListener(OnDeclineQuest);
    }

    private void OnAcceptQuest()
    {
        questChoicePanel.SetActive(false);

        if (!string.IsNullOrEmpty(currentQuestId))
        {
            QuestManager.Instance.AcceptQuest(currentQuestId);
            currentNPC.UpdateStatusIcon();
            PlayFollowupDialogue(currentNPC.npcId, Def_Dialogue.TYPE_QUEST_ACCEPT);
        }
        else CloseInteraction();
    }

    private void OnDeclineQuest()
    {
        questChoicePanel.SetActive(false);

        if (!string.IsNullOrEmpty(currentQuestId))
        {
            QuestData quest = QuestManager.Instance.GetQuestData(currentQuestId);
            if (quest != null) quest.status = QuestStatus.None;
            PlayFollowupDialogue(currentNPC.npcId, Def_Dialogue.TYPE_QUEST_DECLINE);
        }
        else CloseInteraction();
    }

    private void PlayFollowupDialogue(string npcId, string type)
    {
        var lines = DialogueDataManager.Instance.GetDialogueSequence(npcId, type, currentQuestId);
        if (lines != null && lines.Count > 0)
            StartCoroutine(RunDialogueSequence(lines, false, null));
        else
            CloseInteraction();
    }

    // ==========================================
    // 종료 처리
    // ==========================================
    public void CloseInteraction()
    {
        isDialogueActive = false;
        currentQuestId = null;

        //  퀘스트 목록의 모든 버튼 제거 (중요!)
        if (questListContainer != null)
        {
            foreach (Transform child in questListContainer)
            {
                Destroy(child.gameObject);
            }
        }

        //  모든 패널 완전 초기화 (순서 중요!)
        nextButtonPanel.SetActive(false);
        questChoicePanel.SetActive(false);
        questSelectionPanel.SetActive(false);
        dialoguePanel.SetActive(false);

        //  버튼 리스너 정리
        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
        }
        if (acceptButton != null)
        {
            acceptButton.onClick.RemoveAllListeners();
        }
        if (declineButton != null)
        {
            declineButton.onClick.RemoveAllListeners();
        }
        if (PlayerController.Instance != null)
            PlayerController.Instance.SetControlsLocked(false);

        PlayerHUD.Instance.UnregisterUI(this);
        if (currentNPC != null)
        {
            currentNPC.OnInteractionClosed();
            currentNPC = null;
        }
    }

    public void ForceCloseInteraction()
    {
        if (isDialogueActive)
        {
            //  코루틴 중단 전에 먼저 패널과 버튼 정리
            StopAllCoroutines();

            //  명시적으로 모든 상태 초기화
            CloseInteraction();
        }
    }

    
    /// 퀘스트 상태 아이콘 가져오기
    
    private string GetQuestStatusIcon(QuestStatus status, QuestData questData = null)
    {
        switch (status)
        {
            case QuestStatus.Completed:
                return "[완료 가능]";

            case QuestStatus.Accepted:
                //  Accepted 상태에서도 목표가 모두 완료되었는지 확인 
                if (questData != null && questData.IsCompleted())
                {
                    return "[완료 가능]";
                }
                return "[진행중]";

            case QuestStatus.Offered:
            case QuestStatus.None:
                return "[새 퀘스트]";

            case QuestStatus.Rewarded:
                return "[완료]";

            default:
                return "";
        }
    }

    
    /// 퀘스트 선택 & 일상 대화 통합 (수정된 버전)
    
    private void ShowQuestOrDailySelection(NPCController npc, string afterQuestId = null)
    {
        List<string> activeQuests = npc.GetActiveQuests();

        // 퀘스트가 전혀 없으면 바로 일상 대화로 전환
        if (activeQuests == null || activeQuests.Count == 0)
        {
            var dailyDialogue = DialogueDataManager.Instance.GetDialogueSequence(npc.npcId, Def_Dialogue.TYPE_DAILY);
            if (dailyDialogue != null && dailyDialogue.Count > 0)
                StartCoroutine(RunDialogueSequence(dailyDialogue, showChoicesAfter: false, questId: null));
            else
                CloseInteraction();
            return;
        }

        questSelectionPanel.SetActive(true);

        foreach (Transform child in questListContainer)
            Destroy(child.gameObject);

        // 퀘스트 버튼 생성
        for (int i = 0; i < activeQuests.Count; i++)
        {
            string questId = activeQuests[i];
            QuestData questData = QuestManager.Instance.GetQuestData(questId);
            if (questData == null) continue;

            GameObject buttonObj = Instantiate(questButtonPrefab, questListContainer);
            Button button = buttonObj.GetComponent<Button>();
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();

            QuestStatus status = questData.status;

            // 상태 아이콘 표시
            string statusIcon = GetQuestStatusIcon(status, questData);

            if (buttonText != null)
                buttonText.text = $"{statusIcon} {questData.questName}";

            string capturedQuestId = questId;
            button.onClick.AddListener(() =>
            {
                Debug.Log($"[DialogueUI] 버튼 클릭됨: {capturedQuestId}");
                questSelectionPanel.SetActive(false);
                HandleQuestInteraction(npc, capturedQuestId);
            });
        }

        // 일상 대화 버튼 항상 추가
        GameObject dailyButtonObj = Instantiate(questButtonPrefab, questListContainer);
        Button dailyButton = dailyButtonObj.GetComponent<Button>();
        TextMeshProUGUI dailyText = dailyButtonObj.GetComponentInChildren<TextMeshProUGUI>();
        if (dailyText != null) dailyText.text = "일상 대화";

        dailyButton.onClick.AddListener(() =>
        {
            questSelectionPanel.SetActive(false);
            var dailyDialogue = DialogueDataManager.Instance.GetDialogueSequence(npc.npcId, Def_Dialogue.TYPE_DAILY);
            if (dailyDialogue != null && dailyDialogue.Count > 0)
                StartCoroutine(RunDialogueSequence(dailyDialogue, showChoicesAfter: false, questId: null));
            else
                CloseInteraction();
        });

        StartCoroutine(RefreshLayout());
    }

    
    /// 퀘스트 상태별 처리 (수정된 버전)
    ///  Accepted → Completed → Rewarded 흐름으로 수정 
    
    private void HandleQuestInteraction(NPCController npc, string questId)
    {
        Debug.Log($"[DialogueUI] HandleQuestInteraction 호출: {questId}");
        currentQuestId = questId;

        QuestData questData = QuestManager.Instance.GetQuestData(questId);
        if (questData == null)
        {
            Debug.LogError($"[DialogueUI] 퀘스트 데이터를 찾을 수 없음: {questId}");
            CloseInteraction();
            return;
        }

        QuestStatus status = questData.status;
        Debug.Log($"[DialogueUI] 퀘스트 상태: {status}");

        switch (status)
        {
            case QuestStatus.None:
            case QuestStatus.Offered:
                // QuestOffer 대화 후 수락/거절 패널 표시
                StartDialogueSequence(npc.npcId, Def_Dialogue.TYPE_QUEST_OFFER, showChoicesAfter: true, questId: questId);
                break;

            case QuestStatus.Accepted:
                //  목표 완료 여부에 따라 다른 대화 
                if (questData.IsCompleted())
                {
                    // 목표 완료 → Completed 상태로 전환
                    Debug.Log($"[DialogueUI] 퀘스트 목표 완료 확인: {questId}");
                    QuestManager.Instance.ConfirmedQuestCompletion(questId);
                    npc.UpdateStatusIcon();

                    // Complete 대화 (완료 보고) + 보상 지급
                    StartDialogueSequence(npc.npcId, Def_Dialogue.TYPE_QUEST_COMPLETE, showChoicesAfter: false, questId: questId);

                    // 보상 지급 및 Rewarded 상태로 전환
                    QuestManager.Instance.FinalizeQuest(questId);
                    npc.UpdateStatusIcon();
                }
                else
                {
                    // 진행 중 → Progress 대화
                    StartDialogueSequence(npc.npcId, Def_Dialogue.TYPE_QUEST_PROGRESS, showChoicesAfter: false, questId: questId);
                }
                break;

            case QuestStatus.Completed:
                //  Completed 상태: 목표는 달성했고, 이제 보상을 받는 단계 
                Debug.Log($"[DialogueUI] 퀘스트 보상 지급: {questId}");

                // Complete 대화 + 보상 지급
                StartDialogueSequence(npc.npcId, Def_Dialogue.TYPE_QUEST_COMPLETE, showChoicesAfter: false, questId: questId);

                // 보상 지급 및 Rewarded 상태로 전환
                QuestManager.Instance.FinalizeQuest(questId);
                npc.UpdateStatusIcon();
                break;

            case QuestStatus.Rewarded:
                // 보상 완료된 퀘스트는 일상 대화로
                var dailyDialogue = DialogueDataManager.Instance.GetDialogueSequence(npc.npcId, Def_Dialogue.TYPE_DAILY);
                if (dailyDialogue != null && dailyDialogue.Count > 0)
                    StartCoroutine(RunDialogueSequence(dailyDialogue, showChoicesAfter: false, questId: null));
                else
                    CloseInteraction();
                break;
        }
    }

    public void Close()
    {
        CloseInteraction();
    }

    public GameObject GetUIPanel()
    {
        return dialoguePanel;
    }
}