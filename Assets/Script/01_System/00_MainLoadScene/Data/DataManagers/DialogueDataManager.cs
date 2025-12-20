using System.Collections.Generic;
using UnityEngine;

public class DialogueDataManager : MonoBehaviour
{
    public static DialogueDataManager Instance { get; private set; }

    [Header("SO 파일")]
    public DialogueSequenceSO DialogueDatabaseSO;

    private readonly List<DialogueSequence> allDialogues = new();

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

    void BuildDictionary(DialogueSequenceSO database)
    {
        allDialogues.Clear();
        allDialogues.AddRange(database.Items);
        Debug.Log($"[ItemDataManager] ScriptableObject에서 {allDialogues.Count}개의 아이템 로드 완료");
    }

    // questId 없이 검색 (기본 대화용)
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

    // questId와 함께 검색 (퀘스트 관련 대화용)
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