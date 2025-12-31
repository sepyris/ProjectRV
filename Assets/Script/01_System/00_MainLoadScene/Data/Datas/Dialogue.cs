using Definitions;
using System.Collections.Generic;
using UnityEngine;


/// 대화 데이터를 저장하는 ScriptableObject

public class DialogueSequenceSO : ScriptableObject
{
    public List<DialogueSequence> Items = new List<DialogueSequence>();
}

[System.Serializable]
public class DialogueLine
{
    public string Text;
    public string GetSpeakerName(string npcId)
    {
        if (NPCInfoManager.Instance != null)
        {
            return NPCInfoManager.Instance.GetNPCName(npcId);
        }
        return npcId;
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