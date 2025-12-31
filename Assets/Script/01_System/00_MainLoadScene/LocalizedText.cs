// LocalizedText.cs
//  신규: Text 컴포넌트에 붙여서 언어 변경 시 자동 업데이트

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Definitions;


/// Text 컴포넌트에 다국어 키를 연결하여 자동으로 번역을 적용합니다.
/// 언어가 변경되면 자동으로 텍스트가 업데이트되고 폰트도 언어에 맞게 변경됩니다.

public class LocalizedText : MonoBehaviour
{
    [Header("Localization")]
    [Tooltip("LocKeys에 정의된 다국어 키를 입력하세요")]
    public string localizationKey;

    [Header("Text Component (자동 감지)")]
    private Text uiText;
    private TextMeshProUGUI tmpText;

    void Awake()
    {
        // Text 컴포넌트 자동 감지
        uiText = GetComponent<Text>();
        tmpText = GetComponent<TextMeshProUGUI>();

        if (uiText == null && tmpText == null)
        {
            Debug.LogError(string.Format(Def_UI.LOCALIZEDTEXT_NO_COMPONENT, gameObject.name));
            enabled = false;
        }
    }

    void OnEnable()
    {
        // 언어 변경 이벤트 구독
        LocalizationManager.OnLanguageChanged += OnLanguageChanged;

        // 초기 텍스트 설정
        UpdateText();
    }

    void OnDisable()
    {
        // 이벤트 구독 해제
        LocalizationManager.OnLanguageChanged -= OnLanguageChanged;
    }

    
    /// 언어가 변경되었을 때 호출됩니다.
    
    private void OnLanguageChanged(LocalizationManager.Language newLanguage)
    {
        UpdateText();
    }

    
    /// 현재 언어에 맞는 텍스트로 업데이트합니다.
    /// 폰트가 할당되어 있으면 legacy Text / TMP 둘 다 적용합니다.
    
    public void UpdateText()
    {
        if (string.IsNullOrEmpty(localizationKey))
        {
            Debug.LogWarning(string.Format(Def_UI.LOCALIZEDTEXT_EMPTY_KEY, gameObject.name));
            return;
        }

        if (LocalizationManager.Instance == null)
        {
            Debug.LogWarning(Def_UI.LOCALIZEDTEXT_MANAGER_NOT_INIT);
            return;
        }

        string localizedValue = LocalizationManager.Instance.GetLocalizedValue(localizationKey);

        // 폰트 적용
        var lang = LocalizationManager.Instance.GetCurrentLanguage();
        var fontForLang = LocalizationManager.Instance.GetFontForLanguage(lang);
        var tmpFontForLang = LocalizationManager.Instance.GetTMPFontForLanguage(lang);

        // 적절한 Text 컴포넌트에 적용
        if (uiText != null)
        {
            if (fontForLang != null)
            {
                uiText.font = fontForLang;
            }
            uiText.text = localizedValue;
        }
        else if (tmpText != null)
        {
            if (tmpFontForLang != null)
            {
                tmpText.font = tmpFontForLang;
            }
            tmpText.text = localizedValue;
            tmpText.ForceMeshUpdate();
        }
    }
}