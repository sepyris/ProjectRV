using UnityEngine;


/// ESC/Cancel로 닫을 수 있는 UI 인터페이스

public interface IClosableUI
{
    void Close();
    GameObject GetUIPanel();
}