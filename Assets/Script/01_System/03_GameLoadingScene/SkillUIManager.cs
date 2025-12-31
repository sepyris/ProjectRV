using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SkillUIManager : MonoBehaviour,IClosableUI
{
    // Singleton instance
    public static SkillUIManager Instance { get; private set; }

    [Header("메인 패널")]
    private GameObject skillUIPanel;
    private Button SkillUiCloseButton;

    [Header("탭버튼")]
    private Button ActiveSkillTabButton;

    public GameObject skillUIPrepabs;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        skillUIPanel.SetActive(false);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Close()
    {
        throw new System.NotImplementedException();
    }

    public GameObject GetUIPanel()
    {
        throw new System.NotImplementedException();
    }
}
