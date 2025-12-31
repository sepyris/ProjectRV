using System.Collections.Generic;
using System.Linq;


/// 전체 캐릭터 슬롯 데이터

[System.Serializable]
public class AllCharactersSaveData
{
    public List<CharacterSlotData> characterSlots = new List<CharacterSlotData>();
    public string lastSelectedCharacterid;

    
    /// 캐릭터 추가
    
    public void AddCharacter(CharacterSlotData character)
    {
        if (characterSlots.Count < 3)
        {
            characterSlots.Add(character);
        }
    }

    
    /// 캐릭터 삭제
    
    public void RemoveCharacter(string characterid)
    {
        characterSlots.RemoveAll(c => c.characterid == characterid);
    }

    
    /// 캐릭터 가져오기
    
    public CharacterSlotData GetCharacter(string characterid)
    {
        return characterSlots.FirstOrDefault(c => c.characterid == characterid);
    }

    
    /// 슬롯에 캐릭터가 있는지 확인
    
    public bool HasCharacterInSlot(int slotIndex)
    {
        return characterSlots.Any(c => c.slotIndex == slotIndex);
    }

    
    /// 슬롯의 캐릭터 가져오기
    
    public CharacterSlotData GetCharacterInSlot(int slotIndex)
    {
        return characterSlots.FirstOrDefault(c => c.slotIndex == slotIndex);
    }

    
    /// 사용 가능한 슬롯 번호 가져오기
    
    public int GetAvailableSlot()
    {
        for (int i = 0; i < 3; i++)
        {
            if (!HasCharacterInSlot(i))
                return i;
        }
        return -1;
    }

    
    /// 캐릭터 개수
    
    public int CharacterCount => characterSlots.Count;

    
    /// 슬롯이 가득 찼는지
    
    public bool IsFull => characterSlots.Count >= 3;
}