using Definitions;
using System.Collections.Generic;
using UnityEngine;

public class DialogueSequenceSO : ScriptableObject
{
    // Dictionary 대신 직렬화 가능한 List를 사용합니다. (Unity는 Dictionary를 Inspector에서 표시하지 못함)
    // 데이터 접근을 빠르게 하기 위해 런타임에 List를 Dictionary로 변환할 것입니다.
    public List<DialogueSequence> Items = new List<DialogueSequence>();
}

[System.Serializable]
public class DialogueLine
{
    public string Text;

    //  Speaker는 런타임에 npcId로부터 NPCInfoManager를 통해 가져옴 
    public string GetSpeakerName(string npcId)
    {
        if (NPCInfoManager.Instance != null)
        {
            return NPCInfoManager.Instance.GetNPCName(npcId);
        }
        return npcId; // NPCInfoManager가 없으면 npcId 그대로 반환
    }
}

[System.Serializable]
public class DialogueSequence
{
    public string npcId;
    public string dialogueType;
    public string questId;
    public List<DialogueLine> lines = new List<DialogueLine>();
}