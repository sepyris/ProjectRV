using System.Collections.Generic;
using UnityEngine;


/// 대화 데이터 관리 싱글톤

public class DialogueDataManager : MonoBehaviour
{
    public static DialogueDataManager Instance { get; private set; }

    [Header("SO 파일")]
    public DialogueSequenceSO DialogueDatabaseSO;

    private readonly List<DialogueSequence> allDialogues = new();
   
    // ==========================================
    // 초기화 메서드
    // ==========================================
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (DialogueDatabaseSO != null)
            {
                BuildDictionary(DialogueDatabaseSO);
            }
            else
            {
                Debug.LogError("[DialogueDataManager] CSV 파일이 할당되지 않았습니다.");
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// 데이터베이스 초기화 및 재구축
    void BuildDictionary(DialogueSequenceSO database)
    {
        allDialogues.Clear();
        allDialogues.AddRange(database.Items);
        Debug.Log($"[ItemDataManager] ScriptableObject에서 {allDialogues.Count}개의 아이템 로드 완료");
    }

    // ==========================================
    // 조회 메서드
    // ==========================================

    
    /// npcid와 dialogueType으로 검색
    
    public List<DialogueLine> GetDialogueSequence(string npcId, string dialogueType)
    {
        foreach (var seq in allDialogues)
        {
            if (seq.npcId == npcId &&
                seq.dialogueType == dialogueType &&
                string.IsNullOrEmpty(seq.questId))
            {
                return seq.lines;
            }
        }

        Debug.LogWarning($"[DialogueDataManager] 대화 못 찾음: NPC={npcId}, Type={dialogueType}");
        return null;
    }

    
    /// npcid와 dialogueType, questId로 검색
    
    public List<DialogueLine> GetDialogueSequence(string npcId, string dialogueType, string questId)
    {
        foreach (var seq in allDialogues)
        {
            if (seq.npcId == npcId &&
                seq.dialogueType == dialogueType &&
                seq.questId == questId)
            {
                return seq.lines;
            }
        }

        Debug.LogWarning($"[DialogueDataManager] 대화 못 찾음: NPC={npcId}, Type={dialogueType}, Questid={questId}");
        return null;
    }
}