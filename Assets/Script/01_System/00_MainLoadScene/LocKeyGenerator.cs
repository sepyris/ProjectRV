#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Linq;

public static class LocKeyGenerator
{
    // CSV 파일 경로 (Resources 폴더 내)
    private const string CSV_PATH = "Assets/Script/System/CSV/localization.csv";
    // 생성될 C# 파일 경로
    private const string OUTPUT_PATH = "Assets/Script/Generated/LocKeys.cs";

    // Unity 에디터 메뉴에 버튼 추가
    [MenuItem("Tools/Localization/Generate Localization Keys")]
    public static void GenerateKeys()
    {
        // 1. CSV 파일 읽기
        if (!File.Exists(CSV_PATH))
        {
            Debug.LogError("Localization CSV file not found at: " + CSV_PATH);
            return;
        }

        string[] lines = File.ReadAllLines(CSV_PATH);
        if (lines.Length <= 1)
        {
            Debug.LogWarning("CSV file is empty or only contains headers.");
            return;
        }

        // 2. 키 목록 추출 (첫 번째 라인 건너뛰고, 각 라인의 첫 번째 항목)
        var keys = lines
            .Skip(1) // 헤더(첫 줄) 건너뛰기
            .Select(line => line.Split(',').FirstOrDefault()?.Trim()) // 쉼표로 분리 후 첫 항목(KEY) 선택
            .Where(key => !string.IsNullOrEmpty(key)) // 비어있지 않은 키만 선택
            .Distinct() // 중복 제거
            .ToList();

        // 3. C# 파일 내용 생성 (StringBuilder 사용)
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("// 이 파일은 LocKeyGenerator에 의해 자동으로 생성되었습니다.");
        sb.AppendLine("// CSV 파일이 수정되면 'Tools/Localization/Generate Localization Keys'를 실행하세요.");
        sb.AppendLine();
        sb.AppendLine("public static class LocKeys");
        sb.AppendLine("{");

        foreach (string key in keys)
        {
            // 유효한 C# 식별자인지 간단 검사(숫자로 시작하면 언더스코어 추가)
            string safeKey = key;
            if (string.IsNullOrEmpty(safeKey)) continue;
            // 공백·특수문자 제거(간단 처리)
            safeKey = new string(safeKey.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
            if (char.IsDigit(safeKey.FirstOrDefault())) safeKey = "_" + safeKey;

            sb.AppendLine($"    public const string {safeKey} = \"{key}\";");
        }

        sb.AppendLine("}");

        // 4. 파일 쓰기 및 Unity 에디터 업데이트
        string directory = Path.GetDirectoryName(OUTPUT_PATH);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(OUTPUT_PATH, sb.ToString(), Encoding.UTF8);

        Debug.Log($"Localization Keys generated successfully: {keys.Count} keys written to {OUTPUT_PATH}");

        // Unity 에디터에 파일이 새로 생성되었음을 알림
        AssetDatabase.Refresh();
    }
}
#endif