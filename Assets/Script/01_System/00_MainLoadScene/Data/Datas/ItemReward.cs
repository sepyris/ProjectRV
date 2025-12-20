// Script/System/Data/Common/ItemReward.cs
using System;
using UnityEngine;

namespace GameData.Common
{
    /// <summary>
    /// 아이템 보상/드롭 공통 데이터
    /// 퀘스트 보상, 몬스터 드롭, 채집물 등에서 공통으로 사용
    /// </summary>
    [Serializable]
    public class ItemReward
    {
        [Tooltip("아이템 id")]
        public string itemId;

        [Tooltip("지급/드롭 개수")]
        public int quantity = 1;

        [Tooltip("드롭 확률 (0~100), 퀘스트 보상은 100")]
        [Range(0f, 100f)]
        public float dropRate = 100f;

        // 생성자
        public ItemReward() { }

        public ItemReward(string itemId, int quantity, float dropRate = 100f)
        {
            this.itemId = itemId;
            this.quantity = quantity;
            this.dropRate = dropRate;
        }
        //파싱을 해서 바로 생성 할수 있도록 설정
        public ItemReward(string str, float defaultDropRate = 100f)
        {
            if (string.IsNullOrEmpty(str)) return;

            var parts = str.Split(':');
            if (parts.Length < 2) return;

            string itemId = parts[0].Trim();

            if (!int.TryParse(parts[1].Trim(), out int quantity))
                quantity = 1;

            float dropRate = defaultDropRate;
            if (parts.Length >= 3 && float.TryParse(parts[2].Trim(), out float parsedRate))
            {
                dropRate = Mathf.Clamp(parsedRate, 0f, 100f);
            }

            this.itemId = itemId;
            this.quantity = quantity;
            this.dropRate = dropRate;
        }

        // 퀘스트 보상용 (확정 지급)
        public static ItemReward CreateReward(string itemId, int quantity)
        {
            return new ItemReward(itemId, quantity, 100f);
        }

        // 드롭용 (확률 적용)
        public static ItemReward CreateDrop(string itemId, int quantity, float dropRate)
        {
            return new ItemReward(itemId, quantity, dropRate);
        }

        // 드롭 성공 여부 판정
        public bool RollDrop()
        {
            return UnityEngine.Random.Range(0f, 100f) <= dropRate;
        }
        public string GetItemName()
        {
            var itemData = ItemDataManager.Instance.GetItemData(itemId);
            return itemData != null ? itemData.itemName : "Unknown Item";
        }
    }
}