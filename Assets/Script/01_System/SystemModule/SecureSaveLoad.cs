// SecureSaveLoad.cs
using System.IO;
using System.Text;
using System.Security.Cryptography;
using Definitions;
using UnityEngine;
using Steamworks;

public static class SecureSaveLoad
{
    private const string KEY_FILE_NAME = "user_encryption_key.dat";
    private static byte[] _encryptionKeyBytes = null;

    // 자동 클라우드 동기화 옵션
    private const bool AUTO_CLOUD_SYNC = true;

    
    /// 사용자별 암호화 키를 생성하고 저장 (최초 실행 시)
    
    private static bool GenerateAndSaveNewKey()
    {
        _encryptionKeyBytes = new byte[16];
        using (var rng = new RNGCryptoServiceProvider())
        {
            rng.GetBytes(_encryptionKeyBytes);
        }

        // Steam Cloud 사용 시에만 클라우드에 저장
        if (Def_System.USING_STEAM_CLOUD && SteamManager.Initialized)
        {
            if (SteamRemoteStorage.FileWrite(KEY_FILE_NAME, _encryptionKeyBytes, _encryptionKeyBytes.Length))
            {
                Debug.Log("[SecureSave] 새로운 고유 암호화 키를 생성하고 스팀 클라우드에 저장했습니다.");
                return true;
            }
            else
            {
                Debug.LogError("[SecureSave] 고유 암호화 키를 스팀 클라우드에 저장하는데 실패했습니다.");
                _encryptionKeyBytes = null;
                return false;
            }
        }
        else
        {
            // Steam 미사용 시 로컬에 키 저장
            try
            {
                string keyPath = Path.Combine(Def_System.SavePath, KEY_FILE_NAME);
                File.WriteAllBytes(keyPath, _encryptionKeyBytes);
                Debug.Log("[SecureSave] 새로운 암호화 키를 로컬에 생성했습니다.");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SecureSave] 로컬 키 저장 실패: {e.Message}");
                _encryptionKeyBytes = null;
                return false;
            }
        }
    }

    
    /// 사용자별 암호화 키 로드
    
    private static bool LoadKey()
    {
        if (Def_System.USING_STEAM_CLOUD && SteamManager.Initialized)
        {
            // Steam Cloud에서 키 로드
            int fileSize = SteamRemoteStorage.GetFileSize(KEY_FILE_NAME);

            if (fileSize <= 0)
            {
                return GenerateAndSaveNewKey();
            }

            _encryptionKeyBytes = new byte[fileSize];
            if (SteamRemoteStorage.FileRead(KEY_FILE_NAME, _encryptionKeyBytes, fileSize) == fileSize)
            {
                Debug.Log("[SecureSave] 스팀 클라우드에서 고유 암호화 키를 성공적으로 로드했습니다.");
                return true;
            }
            else
            {
                Debug.LogError("[SecureSave] 스팀 클라우드에서 고유 암호화 키를 읽는데 실패했습니다.");
                _encryptionKeyBytes = null;
                return false;
            }
        }
        else
        {
            // 로컬에서 키 로드
            string keyPath = Path.Combine(Def_System.SavePath, KEY_FILE_NAME);

            if (!File.Exists(keyPath))
            {
                return GenerateAndSaveNewKey();
            }

            try
            {
                _encryptionKeyBytes = File.ReadAllBytes(keyPath);
                Debug.Log("[SecureSave] 로컬에서 암호화 키를 로드했습니다.");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SecureSave] 로컬 키 로드 실패: {e.Message}");
                return GenerateAndSaveNewKey();
            }
        }
    }

    
    /// XOR 암호화/복호화
    
    private static byte[] EncryptDecrypt(byte[] data)
    {
        if (_encryptionKeyBytes == null)
        {
            if (!LoadKey())
            {
                Debug.LogError("[SecureSave] 암호화 키가 없습니다. 암호화/복호화 실패.");
                return data;
            }
        }

        byte[] result = new byte[data.Length];
        for (int i = 0; i < data.Length; i++)
        {
            result[i] = (byte)(data[i] ^ _encryptionKeyBytes[i % _encryptionKeyBytes.Length]);
        }
        return result;
    }

    
    /// SHA256 해시 생성
    
    private static string CalculateHash(string json)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
            StringBuilder builder = new StringBuilder();
            foreach (byte b in hashBytes)
            {
                builder.Append(b.ToString("x2"));
            }
            return builder.ToString();
        }
    }

    
    /// 파일 저장: 암호화 및 해시 포함
    /// Steam 사용 여부와 관계없이 로컬 저장은 항상 수행
    
    public static void SaveData(string filePath, GlobalSaveData dataToSave)
    {
        // 1. 데이터에 해시 생성 및 삽입
        dataToSave.integrityHash = string.Empty;
        string dataJson = JsonUtility.ToJson(dataToSave);
        dataToSave.integrityHash = CalculateHash(dataJson);

        // 2. 해시가 포함된 최종 JSON
        string finalJson = JsonUtility.ToJson(dataToSave);
        byte[] jsonBytes = Encoding.UTF8.GetBytes(finalJson);

        // 3. 암호화 (선택사항 - 암호화 키가 있으면 암호화)
        byte[] finalBytes = jsonBytes;
        if (_encryptionKeyBytes == null)
        {
            LoadKey(); // 키가 없으면 로드 시도
        }

        if (_encryptionKeyBytes != null)
        {
            finalBytes = EncryptDecrypt(jsonBytes);
            Debug.Log("[SecureSave] 데이터 암호화 완료");
        }
        else
        {
            Debug.LogWarning("[SecureSave] 암호화 키가 없어 암호화 없이 저장합니다.");
        }

        // 4. 로컬 파일 저장 (항상 수행!)
        try
        {
            // 디렉토리가 없으면 생성
            string directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(filePath, finalBytes);
            Debug.Log($"[SecureSave] 로컬 저장 완료: {filePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SecureSave] 로컬 저장 실패: {e.Message}");
            return;
        }

        // 5. Steam Cloud 동기화 (Steam 사용 시에만)
        if (AUTO_CLOUD_SYNC && Def_System.USING_STEAM_CLOUD && SteamManager.Initialized)
        {
            string fileName = Path.GetFileName(filePath);
            SteamCloudManager.InitiateCloudSave(fileName, finalBytes);
            Debug.Log($"[SecureSave] Steam Cloud 동기화 시작: {fileName}");
        }
    }

    
    /// 파일 로드: 복호화 및 무결성 검증
    /// 로컬 파일이 없으면 Steam Cloud에서 다운로드 시도 (Steam 사용 시)
    
    public static GlobalSaveData LoadData(string filePath)
    {
        byte[] encryptedBytes = null;

        // 1. 로컬 파일 확인
        if (File.Exists(filePath))
        {
            encryptedBytes = File.ReadAllBytes(filePath);
            Debug.Log($"[SecureSave] 로컬 파일 로드: {filePath}");
        }
        // 2. 로컬에 없으면 Steam Cloud에서 다운로드 시도 (Steam 사용 시)
        else if (Def_System.USING_STEAM_CLOUD && SteamManager.Initialized)
        {
            string fileName = Path.GetFileName(filePath);
            int fileSize = SteamRemoteStorage.GetFileSize(fileName);

            if (fileSize > 0)
            {
                encryptedBytes = new byte[fileSize];
                int bytesRead = SteamRemoteStorage.FileRead(fileName, encryptedBytes, fileSize);

                if (bytesRead == fileSize)
                {
                    Debug.Log($"[SecureSave] Steam Cloud에서 파일 다운로드 성공: {fileName}");

                    // 로컬에도 저장 (캐싱)
                    try
                    {
                        string directory = Path.GetDirectoryName(filePath);
                        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }
                        File.WriteAllBytes(filePath, encryptedBytes);
                        Debug.Log($"[SecureSave] 로컬 캐시 저장 완료: {filePath}");
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"[SecureSave] 로컬 캐시 저장 실패 (로드는 계속): {e.Message}");
                    }
                }
                else
                {
                    Debug.LogError($"[SecureSave] Steam Cloud 다운로드 실패: {fileName}");
                    return null;
                }
            }
            else
            {
                Debug.Log($"[SecureSave] 저장된 파일이 없습니다: {filePath}");
                return null;
            }
        }
        else
        {
            Debug.Log($"[SecureSave] 저장된 파일이 없습니다: {filePath}");
            return null;
        }

        // 3. 복호화 시도
        byte[] decryptedBytes = encryptedBytes;

        // 암호화 키 로드
        if (_encryptionKeyBytes == null)
        {
            LoadKey();
        }

        // 키가 있으면 복호화 시도
        if (_encryptionKeyBytes != null)
        {
            decryptedBytes = EncryptDecrypt(encryptedBytes);
            Debug.Log("[SecureSave] 데이터 복호화 완료");
        }

        string finalJson = Encoding.UTF8.GetString(decryptedBytes);

        // 4. JSON 파싱
        GlobalSaveData loadedData;
        try
        {
            loadedData = JsonUtility.FromJson<GlobalSaveData>(finalJson);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SecureSave] JSON 파싱 실패: {e.Message}");
            return null;
        }

        // 5. 무결성 검증
        string originalHash = loadedData.integrityHash;
        loadedData.integrityHash = string.Empty;
        string dataJsonForCheck = JsonUtility.ToJson(loadedData);
        string newHash = CalculateHash(dataJsonForCheck);

        if (newHash != originalHash)
        {
            Debug.LogError("[SecureSave] 파일 무결성 검증 실패! 파일이 변조되었거나 교체되었습니다.");
            return null;
        }

        // 6. 검증 성공 시 원본 해시 값 복원 및 데이터 반환
        loadedData.integrityHash = originalHash;
        Debug.Log("[SecureSave] 파일 로드 및 무결성 검증 성공!");
        return loadedData;
    }

    
    /// 저장 파일 삭제 (로컬 + Steam Cloud)
    
    public static void DeleteSaveData(string filePath)
    {
        // 로컬 파일 삭제
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            Debug.Log($"[SecureSave] 로컬 파일 삭제: {filePath}");
        }

        // Steam Cloud 파일 삭제 (Steam 사용 시)
        if (Def_System.USING_STEAM_CLOUD && SteamManager.Initialized)
        {
            string fileName = Path.GetFileName(filePath);
            if (SteamRemoteStorage.FileExists(fileName))
            {
                SteamRemoteStorage.FileDelete(fileName);
                Debug.Log($"[SecureSave] Steam Cloud 파일 삭제: {fileName}");
            }
        }
    }
}