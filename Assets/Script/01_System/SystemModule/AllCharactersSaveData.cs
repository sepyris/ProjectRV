using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 전체 캐릭터 슬롯 데이터
/// </summary>
[System.Serializable]
public class AllCharactersSaveData
{
    public List<CharacterSlotData> characterSlots = new List<CharacterSlotData>();
    public string lastSelectedCharacterid;

    /// <summary>
    /// 캐릭터 추가
    /// </summary>
    public void AddCharacter(CharacterSlotData character)
    {
        if (characterSlots.Count < 3)
        {
            characterSlots.Add(character);
        }
    }

    /// <summary>
    /// 캐릭터 삭제
    /// </summary>
    public void RemoveCharacter(string characterid)
    {
        characterSlots.RemoveAll(c => c.characterid == characterid);
    }

    /// <summary>
    /// 캐릭터 가져오기
    /// </summary>
    public CharacterSlotData GetCharacter(string characterid)
    {
        return characterSlots.FirstOrDefault(c => c.characterid == characterid);
    }

    /// <summary>
    /// 슬롯에 캐릭터가 있는지 확인
    /// </summary>
    public bool HasCharacterInSlot(int slotIndex)
    {
        return characterSlots.Any(c => c.slotIndex == slotIndex);
    }

    /// <summary>
    /// 슬롯의 캐릭터 가져오기
    /// </summary>
    public CharacterSlotData GetCharacterInSlot(int slotIndex)
    {
        return characterSlots.FirstOrDefault(c => c.slotIndex == slotIndex);
    }

    /// <summary>
    /// 사용 가능한 슬롯 번호 가져오기
    /// </summary>
    public int GetAvailableSlot()
    {
        for (int i = 0; i < 3; i++)
        {
            if (!HasCharacterInSlot(i))
                return i;
        }
        return -1;
    }

    /// <summary>
    /// 캐릭터 개수
    /// </summary>
    public int CharacterCount => characterSlots.Count;

    /// <summary>
    /// 슬롯이 가득 찼는지
    /// </summary>
    public bool IsFull => characterSlots.Count >= 3;
}