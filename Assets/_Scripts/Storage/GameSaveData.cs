
using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;


#region Game Save Data Types

/// <summary> Container for all the game's sensitive data. </summary>
[Serializable]
public class GameSaveData
{
    public int Coins = 0;
    public List<BiomeData> BiomeDatas = new();
    public List<string> UnlockedSkins = new();
}

[Serializable]
public class BiomeData
{
    public sulong ReachedHeight = 0;
    public sulong AccumulatedHeight = 0;

    public BiomeData(sulong reachedHeight, sulong accumulatedHeight)
    { ReachedHeight = reachedHeight; AccumulatedHeight = accumulatedHeight; }
}

#endregion


/// <summary>
/// <br/>   Handles saving and loading sensitive data to and from an encrypted JSON file.
/// <br/>   Simple and lightweight XOR encryption is used:
/// <br/>   → Data is safe from casual users but not from more tech savvy ones.
/// </summary>
public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; } = null;


    // TODO: Add second file for data safety
    // Altrentate between two or more save files between each play session



    // .dat is used to store generic data, in this case it's used to store our encrypted game save data
    private string savePath = Path.Combine(Application.persistentDataPath, "save.dat");

    private bool isDirty = false;
    private GameSaveData currentSaveData;


    #region Initialization ================================================================================== Initialization

    public static void Initialize()
    {
        if (Instance == null)
        { DontDestroyOnLoad(new GameObject("[Data Manager]", typeof(DataManager))); }
    }

    private void Awake()
    {
        Instance = this;
        currentSaveData = Load();
        StartCoroutine(AutoSave());
    }

    #endregion Initialization


    #region Data Modifying ================================================================================== Data Modifying

    public int GetCoins() => currentSaveData.Coins;
    public void SetCoins(int newCoins) { isDirty = true; currentSaveData.Coins = newCoins; }
    public void AddCoins(int coinsToAdd) { isDirty = true; currentSaveData.Coins += coinsToAdd; }

    //public BiomeData GetBiomeData(BiomeType biomeType) => currentSaveData.BiomeDatas.Find(b => b.BiomeType == biomeType);
    //public void SetBiomeData(BiomeType biomeType, ulong reachedHeight, ulong accumulatedHeight)
    //{
    //    isDirty = true;
    //    int index = currentSaveData.BiomeDatas.FindIndex(b => b.BiomeType == biomeType);

    //    if (index == -1)
    //    {
    //        currentSaveData.BiomeDatas.Add(new(biomeType, accumulatedHeight, reachedHeight));
    //    }
    //    else
    //    {
    //        currentSaveData.BiomeDatas[index].ReachedHeight = reachedHeight;
    //        currentSaveData.BiomeDatas[index].AccumulatedHeight = accumulatedHeight;
    //    }
    //}

    public List<string> GetUnlockedSkins() => currentSaveData.UnlockedSkins;
    public void AddUnlockedSkin(string skinName)
    {
        isDirty = true;
        if (!currentSaveData.UnlockedSkins.Contains(skinName))
        { currentSaveData.UnlockedSkins.Add(skinName); }
    }

    #endregion Data Modifying


    #region Data Saving ===================================================================================== Data Saving

    public void Save()
    {
        isDirty = false;
        string json = JsonUtility.ToJson(currentSaveData);
        string encrypted = Encrypt(json);
        File.WriteAllText(savePath, encrypted);
    }

    private void OnApplicationQuit() => Save();

    private IEnumerator AutoSave()
    { while (true) { yield return new WaitForSeconds(10); if (isDirty) { Save(); } } }

    #endregion Data Saving


    #region Data Loading ==================================================================================== Data Loading 

    private GameSaveData Load()
    {
        if (!File.Exists(savePath)) { return new(); }

        string encrypted = File.ReadAllText(savePath);
        string json = Decrypt(encrypted);
        return JsonUtility.FromJson<GameSaveData>(json);
    }

    #endregion Data Loading


    #region Encrypt / Decrypt =============================================================================== Encrypt / Decrypt

    private string Encrypt(string plainText)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(plainText);
        for (int i = 0; i < bytes.Length; i++) { bytes[i] ^= 203; }

        return Convert.ToBase64String(bytes);
    }

    private string Decrypt(string encryptedText)
    {
        byte[] bytes = Convert.FromBase64String(encryptedText);
        for (int i = 0; i < bytes.Length; i++) { bytes[i] ^= 203; }

        return Encoding.UTF8.GetString(bytes);
    }

    #endregion Encrypt / Decrypt
}
