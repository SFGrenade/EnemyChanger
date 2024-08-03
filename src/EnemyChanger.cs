using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using HutongGames.PlayMaker.Actions;
using JetBrains.Annotations;
using Modding;
using SFCore.Generics;
using SFCore.Utils;
using UnityEngine;
using UObject = UnityEngine.Object;
using UScene = UnityEngine.SceneManagement.Scene;

namespace EnemyChanger;

class EcGlobalSettings
{
    public bool DumpSprites = false;
}

[UsedImplicitly]
class EnemyChanger : GlobalSettingsMod<EcGlobalSettings>
{
    private readonly string _dir;

    public override string GetVersion() => Util.GetVersion(Assembly.GetExecutingAssembly());

    public EnemyChanger() : base("Enemy Changer")
    {
        _dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/Sprites/";

        if (!Directory.Exists(_dir)) Directory.CreateDirectory(_dir);
    }

    public override void Initialize()
    {
        DebugLog("!Initialize");

        ModHooks.OnEnableEnemyHook += ModHooksOnOnEnableEnemyHook;

        DebugLog("~Initialize");
    }

    private bool ModHooksOnOnEnableEnemyHook(GameObject enemy, bool isAlreadyDead)
    {
        foreach (var tk in enemy.GetComponentsInChildren<tk2dSprite>(true))
        {
            ChangeTk2dSprite(tk);
        }

        if (enemy.name == "Ghost Warrior Markoth")
        {
            // the fucking spears
            PlayMakerFSM attackFsm = enemy.LocateMyFSM("Attacking");
            GameObject go = attackFsm.GetAction<SpawnObjectFromGlobalPool>("Nail", 0).gameObject.Value;
            foreach (var tk in go.GetComponentsInChildren<SpriteRenderer>(true))
            {
                ChangeSprite(tk);
            }
        }
        return isAlreadyDead;
    }

    private Dictionary<tk2dSpriteDefinition, byte[]> spriteDefinitionCache = new();
    private Dictionary<Texture2D, byte[]> spriteCache = new();

    private string GetTextureHash(byte[] pngBytes)
    {
        SHA512 hash = new SHA512Managed();
        return BitConverter.ToString(hash.ComputeHash(pngBytes)).Replace("-", "");
    }

    private void ChangeTk2dSprite(tk2dSprite self)
    {
        DebugLog("!ChangeTk2dSprite");

        var collection = self.GetCurrentSpriteDef();

        if (spriteDefinitionCache.ContainsKey(collection)) return; // no need to change twice

        Texture2D origTex = (Texture2D) collection.materialInst.mainTexture;
        Texture2D readTex = EnemyChanger.MakeTextureReadable(origTex);
        byte[] hashBytes = readTex.GetRawTextureData();
        string spriteCollectionName = GetTextureHash(hashBytes);
        if (File.Exists($"{this._dir}/{spriteCollectionName}.png"))
        {
            using FileStream fileStream = new FileStream($"{this._dir}/{spriteCollectionName}.png", FileMode.Open);
            byte[] array = new byte[fileStream.Length];
            fileStream.Read(array, 0, array.Length);
            Texture2D texture2D = new Texture2D(2, 2);
            texture2D.LoadImage(array, false);
            collection.materialInst.mainTexture = texture2D;
            spriteDefinitionCache.Add(collection, array);
        }
        else if (GlobalSettings.DumpSprites)
        {
            try
            {
                byte[] pngBytes = readTex.EncodeToPNG();
                SaveTex(pngBytes, $"{this._dir}/{spriteCollectionName}.png");
            }
            catch (Exception)
            {
                DebugLog("---ChangeTk2dSprite");
            }
        }
        UObject.DestroyImmediate(readTex);
        DebugLog("~ChangeTk2dSprite");
    }
    private void ChangeSprite(SpriteRenderer self)
    {
        DebugLog("!ChangeSprite");

        //var collection = self.sprite.texture;
        //
        //if (spriteCache.ContainsKey(collection)) return; // no need to change twice

        Texture2D origTex = self.sprite.texture;
        Texture2D readTex = EnemyChanger.MakeTextureReadable(origTex);
        byte[] hashBytes = readTex.GetRawTextureData();
        string spriteCollectionName = GetTextureHash(hashBytes);
        if (File.Exists($"{this._dir}/{spriteCollectionName}.png"))
        {
            using FileStream fileStream = new FileStream($"{this._dir}/{spriteCollectionName}.png", FileMode.Open);
            byte[] array = new byte[fileStream.Length];
            fileStream.Read(array, 0, array.Length);
            Texture2D texture2D = new Texture2D(2, 2);
            texture2D.LoadImage(array, false);
            self.sprite = Sprite.Create(texture2D, new Rect(0, 0, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f), 64);
            spriteCache.Add(texture2D, array);
        }
        else if (GlobalSettings.DumpSprites)
        {
            try
            {
                byte[] pngBytes = readTex.EncodeToPNG();
                SaveTex(pngBytes, $"{this._dir}/{spriteCollectionName}.png");
            }
            catch (Exception)
            {
                DebugLog("---ChangeSprite");
            }
        }
        UObject.DestroyImmediate(readTex);
        DebugLog("~ChangeSprite");
    }

    private static Texture2D MakeTextureReadable(Texture2D orig)
    {
        DebugLog("!makeTextureReadable");
        Texture2D ret = new Texture2D(orig.width, orig.height);
        RenderTexture tempRt = RenderTexture.GetTemporary(orig.width, orig.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
        Graphics.Blit(orig, tempRt);
        RenderTexture tmpActiveRt = RenderTexture.active;
        RenderTexture.active = tempRt;
        ret.ReadPixels(new Rect(0f, 0f, tempRt.width, tempRt.height), 0, 0);
        ret.Apply();
        RenderTexture.active = tmpActiveRt;
        RenderTexture.ReleaseTemporary(tempRt);
        DebugLog("~makeTextureReadable");
        return ret;
    }

    private static void SaveTex(byte[] pngBytes, string filename)
    {
        DebugLog("!saveTex");
        using (FileStream fileStream2 = new FileStream(filename, FileMode.Create))
        {
            if (fileStream2 != null)
            {
                fileStream2.Write(pngBytes, 0, pngBytes.Length);
            }
        }
        DebugLog("~saveTex");
    }

    private static void DebugLog(string msg)
    {
        Modding.Logger.LogDebug($"[{typeof(EnemyChanger).FullName.Replace(".", "][")}] - {msg}");
        Debug.Log($"[{typeof(EnemyChanger).FullName.Replace(".", "][")}] - {msg}");
    }
    private static void DebugLog(object msg)
    {
        DebugLog($"{msg}");
    }
}