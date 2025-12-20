using UnityEngine;
using System.Collections.Generic;
using System;
using Definitions;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif

public class LocalizationManager : MonoBehaviour
{
    public enum Language
    {
        KOR, // 한국어
        ENG, // 영어
        JPN, // 일본어
        CNA  // 중국어 간체
    }

    public static LocalizationManager Instance { get; private set; }

    [Header("CSV 파일")]
    [Tooltip("localization.csv 파일을 여기에 할당하세요")]
    public TextAsset localizationCsvFile;

    [Header("Settings")]
    [SerializeField] private Language defaultLanguage = Language.KOR;

    private Language currentLanguage;
    private Dictionary<string, Dictionary<string, string>> localizedText;

    // 언어 변경 이벤트
    public static event Action<Language> OnLanguageChanged;

    [Header("Fonts (Legacy Text)")]
    public Font fontKOR;
    public Font fontENG;
    public Font fontJPN;
    public Font fontCNA;

    [Header("Fonts (TextMeshPro)")]
    public TMP_FontAsset tmpFontKOR;
    public TMP_FontAsset tmpFontENG;
    public TMP_FontAsset tmpFontJPN;
    public TMP_FontAsset tmpFontCNA;

    [Header("Auto Load Fonts From Resources / Assets/Font (Editor only)")]
    public bool autoLoadFontsFromResources = true;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            currentLanguage = defaultLanguage;

            //  TextAsset으로 직접 로드 
            if (localizationCsvFile != null)
            {
                LoadLocalizedText(localizationCsvFile.text);
            }
            else
            {
                Debug.LogError("[LocalizationManager] localizationCsvFile이 할당되지 않았습니다!");
            }

            // 폰트 자동 로드 시도
            if (autoLoadFontsFromResources)
            {
                PreloadFonts();
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// CSV 텍스트를 읽고 데이터를 Dictionary에 저장
    /// </summary>
    private void LoadLocalizedText(string csvText)
    {
        localizedText = new Dictionary<string, Dictionary<string, string>>();

        if (string.IsNullOrEmpty(csvText))
        {
            Debug.LogError("[LocalizationManager] CSV 텍스트가 비어있습니다!");
            return;
        }

        string[] lines = csvText.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length < 2)
        {
            Debug.LogError(Def_UI.LOCALIZATION_CSV_EMPTY);
            return;
        }

        // 첫 줄: 헤더 (KEY, KOR, ENG, JPN, CNA)
        string[] headers = SplitCSVLine(lines[0]);

        // 헤더 검증
        if (headers.Length < 2)
        {
            Debug.LogError(Def_UI.LOCALIZATION_HEADER_INVALid);
            return;
        }

        Debug.Log($"[LocalizationManager] 헤더 로드: {string.Join(", ", headers)}");

        // 데이터 행 파싱
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            // 주석 무시
            if (line.StartsWith("#")) continue;

            string[] values = SplitCSVLine(line);

            if (values.Length > 0 && !string.IsNullOrEmpty(values[0]))
            {
                string key = values[0].Trim();
                Dictionary<string, string> languageTexts = new Dictionary<string, string>();

                // 각 언어별 텍스트 저장
                for (int j = 1; j < headers.Length && j < values.Length; j++)
                {
                    string langCode = headers[j].Trim();
                    string text = values[j].Trim();

                    // 이스케이프 처리 (\n → 실제 줄바꿈)
                    text = text.Replace("\\n", "\n");

                    languageTexts.Add(langCode, text);
                }

                if (!localizedText.ContainsKey(key))
                {
                    localizedText.Add(key, languageTexts);
                }
                else
                {
                    Debug.LogWarning(string.Format(Def_UI.LOCALIZATION_DUPLICATE_KEY, key));
                }
            }
        }

        Debug.Log(string.Format(Def_UI.LOCALIZATION_LOADED, localizedText.Count, currentLanguage));
    }

    /// <summary>
    /// CSV 라인을 파싱 (쉼표가 포함된 텍스트 처리)
    /// </summary>
    private string[] SplitCSVLine(string line)
    {
        List<string> result = new List<string>();
        bool inQuotes = false;
        string current = "";

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current);
                current = "";
            }
            else
            {
                current += c;
            }
        }

        result.Add(current);
        return result.ToArray();
    }

    /// <summary>
    /// 메시지 키를 사용하여 현재 언어에 맞는 텍스트를 반환합니다.
    /// </summary>
    public string GetLocalizedValue(string key)
    {
        if (localizedText == null || localizedText.Count == 0)
        {
            Debug.LogError(Def_UI.LOCALIZATION_NO_DATA);
            return $"[NO DATA: {key}]";
        }

        string langCode = currentLanguage.ToString();

        if (localizedText.ContainsKey(key))
        {
            if (localizedText[key].ContainsKey(langCode))
            {
                return localizedText[key][langCode];
            }
            else
            {
                // 현재 언어에 번역이 없으면 기본 언어로 폴백
                string defaultLangCode = defaultLanguage.ToString();
                if (localizedText[key].ContainsKey(defaultLangCode))
                {
                    Debug.LogWarning(string.Format(Def_UI.LOCALIZATION_FALLBACK_WARNING, key, langCode, defaultLangCode));
                    return localizedText[key][defaultLangCode];
                }
            }
        }

        Debug.LogError(string.Format(Def_UI.LOCALIZATION_KEY_NOT_FOUND, key));
        return $"[MISSING: {key}]";
    }

    /// <summary>
    /// 현재 언어를 설정하고 이벤트를 발생시킵니다.
    /// </summary>
    public void SetLanguage(Language lang)
    {
        if (currentLanguage == lang)
        {
            Debug.Log(string.Format(Def_UI.LOCALIZATION_ALREADY_SET, lang));
            return;
        }

        currentLanguage = lang;
        Debug.Log(string.Format(Def_UI.LOCALIZATION_LANG_CHANGED, currentLanguage));

        // 언어 변경 이벤트 발생
        OnLanguageChanged?.Invoke(currentLanguage);
    }

    /// <summary>
    /// 현재 언어를 반환합니다.
    /// </summary>
    public Language GetCurrentLanguage()
    {
        return currentLanguage;
    }

    /// <summary>
    /// 키가 존재하는지 확인합니다.
    /// </summary>
    public bool HasKey(string key)
    {
        return localizedText != null && localizedText.ContainsKey(key);
    }

    /// <summary>
    /// 로드된 모든 키 목록을 반환합니다 (디버깅용).
    /// </summary>
    public List<string> GetAllKeys()
    {
        if (localizedText == null) return new List<string>();
        return new List<string>(localizedText.Keys);
    }

    // ===== 폰트 조회 헬퍼 (자동 로드 포함) =====

    private void PreloadFonts()
    {
        // Resources에서 우선 로드 시도
        if (fontKOR == null) fontKOR = TryLoadFont(Def_Name.FONT_KOR) ?? TryLoadFont(Def_Name.FONT_KOR_SDF);
        if (fontENG == null) fontENG = TryLoadFont(Def_Name.FONT_ENG) ?? TryLoadFont(Def_Name.FONT_ENG_SDF);
        if (fontJPN == null) fontJPN = TryLoadFont(Def_Name.FONT_JPN) ?? TryLoadFont(Def_Name.FONT_JPN_SDF);
        if (fontCNA == null) fontCNA = TryLoadFont(Def_Name.FONT_CNA) ?? TryLoadFont(Def_Name.FONT_CNA_SDF);

        if (tmpFontKOR == null) tmpFontKOR = TryLoadTMP(Def_Name.FONT_KOR_SDF) ?? TryLoadTMP(Def_Name.FONT_KOR);
        if (tmpFontENG == null) tmpFontENG = TryLoadTMP(Def_Name.FONT_ENG_SDF) ?? TryLoadTMP(Def_Name.FONT_ENG);
        if (tmpFontJPN == null) tmpFontJPN = TryLoadTMP(Def_Name.FONT_JPN_SDF) ?? TryLoadTMP(Def_Name.FONT_JPN);
        if (tmpFontCNA == null) tmpFontCNA = TryLoadTMP(Def_Name.FONT_CNA_SDF) ?? TryLoadTMP(Def_Name.FONT_CNA);

#if UNITY_EDITOR
        // Editor 플레이 시 Assets/Font 폴더에서 자동으로 찾아 할당
        TryLoadFontsFromAssetsFolder("Assets/Font");
#endif
    }

    public Font GetFontForLanguage(Language lang)
    {
        switch (lang)
        {
            case Language.KOR: return fontKOR ?? fontENG;
            case Language.ENG: return fontENG ?? fontKOR;
            case Language.JPN: return fontJPN ?? fontENG;
            case Language.CNA: return fontCNA ?? fontENG;
            default: return fontENG;
        }
    }

    public TMP_FontAsset GetTMPFontForLanguage(Language lang)
    {
        switch (lang)
        {
            case Language.KOR: return tmpFontKOR ?? tmpFontENG;
            case Language.ENG: return tmpFontENG ?? tmpFontKOR;
            case Language.JPN: return tmpFontJPN ?? tmpFontENG;
            case Language.CNA: return tmpFontCNA ?? tmpFontENG;
            default: return tmpFontENG;
        }
    }

    // ===== 리소스 로드 헬퍼 =====

    private Font TryLoadFont(string resourceName)
    {
        if (string.IsNullOrEmpty(resourceName)) return null;
        try
        {
            var f = Resources.Load<Font>(resourceName);
            if (f == null)
            {
                Debug.LogFormat("[Localization] Resources에서 Font '{0}'을(를) 찾을 수 없습니다.", resourceName);
            }
            return f;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Localization] Font 로드 중 예외: {e.Message}");
            return null;
        }
    }

    private TMP_FontAsset TryLoadTMP(string resourceName)
    {
        if (string.IsNullOrEmpty(resourceName)) return null;
        try
        {
            var t = Resources.Load<TMP_FontAsset>(resourceName);
            if (t == null)
            {
                Debug.LogFormat("[Localization] Resources에서 TMP_FontAsset '{0}'을(를) 찾을 수 없습니다.", resourceName);
            }
            return t;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Localization] TMP_FontAsset 로드 중 예외: {e.Message}");
            return null;
        }
    }

#if UNITY_EDITOR
    // Editor 전용: Assets/Font 폴더에서 폰트 에셋을 찾아 할당
    private void TryLoadFontsFromAssetsFolder(string folderPath)
    {
        if (!Directory.Exists(folderPath)) return;

        if (fontKOR == null) fontKOR = LoadFontAssetFromFolder(folderPath, Def_Name.FONT_KOR) ?? LoadFontAssetFromFolder(folderPath, Def_Name.FONT_KOR_SDF);
        if (fontENG == null) fontENG = LoadFontAssetFromFolder(folderPath, Def_Name.FONT_ENG) ?? LoadFontAssetFromFolder(folderPath, Def_Name.FONT_ENG_SDF);
        if (fontJPN == null) fontJPN = LoadFontAssetFromFolder(folderPath, Def_Name.FONT_JPN) ?? LoadFontAssetFromFolder(folderPath, Def_Name.FONT_JPN_SDF);
        if (fontCNA == null) fontCNA = LoadFontAssetFromFolder(folderPath, Def_Name.FONT_CNA) ?? LoadFontAssetFromFolder(folderPath, Def_Name.FONT_CNA_SDF);

        if (tmpFontKOR == null) tmpFontKOR = LoadTMPAssetFromFolder(folderPath, Def_Name.FONT_KOR_SDF) ?? LoadTMPAssetFromFolder(folderPath, Def_Name.FONT_KOR);
        if (tmpFontENG == null) tmpFontENG = LoadTMPAssetFromFolder(folderPath, Def_Name.FONT_ENG_SDF) ?? LoadTMPAssetFromFolder(folderPath, Def_Name.FONT_ENG);
        if (tmpFontJPN == null) tmpFontJPN = LoadTMPAssetFromFolder(folderPath, Def_Name.FONT_JPN_SDF) ?? LoadTMPAssetFromFolder(folderPath, Def_Name.FONT_JPN);
        if (tmpFontCNA == null) tmpFontCNA = LoadTMPAssetFromFolder(folderPath, Def_Name.FONT_CNA_SDF) ?? LoadTMPAssetFromFolder(folderPath, Def_Name.FONT_CNA);
    }

    private Font LoadFontAssetFromFolder(string folder, string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        string[] guids = AssetDatabase.FindAssets(name + " t:Font", new[] { folder });
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<Font>(path);
        }

        string[] allGuids = AssetDatabase.FindAssets("t:Font", new[] { folder });
        foreach (var g in allGuids)
        {
            string p = AssetDatabase.GUIDToAssetPath(g);
            string file = Path.GetFileNameWithoutExtension(p);
            if (file.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return AssetDatabase.LoadAssetAtPath<Font>(p);
            }
        }
        return null;
    }

    private TMP_FontAsset LoadTMPAssetFromFolder(string folder, string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        string[] guids = AssetDatabase.FindAssets(name + " t:TMP_FontAsset", new[] { folder });
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        }

        string[] allGuids = AssetDatabase.FindAssets("t:TMP_FontAsset", new[] { folder });
        foreach (var g in allGuids)
        {
            string p = AssetDatabase.GUIDToAssetPath(g);
            string file = Path.GetFileNameWithoutExtension(p);
            if (file.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(p);
            }
        }
        return null;
    }
#endif
}