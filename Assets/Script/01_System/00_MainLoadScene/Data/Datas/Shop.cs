using System.Collections.Generic;
using UnityEngine;


/// 상점 데이터를 저장하는 ScriptableObject
public class ShopDataSO : ScriptableObject
{
    public List<ShopData> Items = new List<ShopData>(); // 다른 SO들과 동일하게 Items 사용
}

/// 상점에서 판매하는 아이템 정보

[System.Serializable]
public class ShopItemData
{
    public string itemid;
    public int limitedStock; // -1이면 무제한, 0 이상이면 제한 수량

    public ShopItemData(string itemid, int limitedStock = -1)
    {
        this.itemid = itemid;
        this.limitedStock = limitedStock;
    }
}


/// 상점 데이터

[System.Serializable]
public class ShopData
{
    public string shopid;
    public List<ShopItemData> items = new List<ShopItemData>();

    public ShopData(string shopid)
    {
        this.shopid = shopid;
    }
}
