using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using SFCore.Generics;
using SFCore.Utils;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EnemyChanger
{
    class EcGlobalSettings
    {
        public bool DumpSprites = false;
    }

    class EnemyChanger : GlobalSettingsMod<EcGlobalSettings>
    {
        private readonly string _dir;
        private readonly Texture2D _emptyTex = new Texture2D(2, 2);

        public override string GetVersion() => Util.GetVersion(Assembly.GetExecutingAssembly());

        public EnemyChanger() : base("Enemy Changer")
        {
            for (int x = 0; x < _emptyTex.width; x++)
            {
                for (int y = 0; y < _emptyTex.height; y++)
                {
                    _emptyTex.SetPixel(x, y, new Color(0, 0, 0, 0));
                }
            }
            _emptyTex.Apply(true);

            _dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/Sprites/";
            
            if (!Directory.Exists(_dir)) Directory.CreateDirectory(_dir);

            On.HealthManager.Awake += OnHealthManagerAwake;
        }

        private void OnHealthManagerAwake(On.HealthManager.orig_Awake orig, HealthManager self)
        {
            DebugLog("!OnHealthManagerAwake");
            orig(self);

            Transform highestParent = self.transform;

            if (!self.gameObject.name.ToLower().Contains("radiance"))
            {
                while (highestParent.parent != null)
                {
                    highestParent = highestParent.parent;
                }

                foreach (var s in highestParent.gameObject.GetComponentsInChildren<tk2dSprite>())
                {
                    ChangeTk2dSprite(s);
                }
            }
            else if (self.gameObject.name.Equals("Absolute Radiance"))
            {
                foreach (var s in highestParent.parent.Find("Abyss Pit").gameObject.GetComponentsInChildren<tk2dSprite>())
                {
                    ChangeTk2dSprite(s);
                }
            }
            else if (self.gameObject.name.Equals("Radiance"))
            {
                foreach (var s in highestParent.parent.Find("Abyss Pit").gameObject.GetComponentsInChildren<tk2dSprite>())
                {
                    ChangeTk2dSprite(s);
                }
            }

            DebugLog("~OnHealthManagerAwake");
        }

        public override void Initialize()
        {
            DebugLog("!Initialize");

            DebugLog("~Initialize");
        }

        private Dictionary<tk2dSpriteDefinition, byte[]> spriteDefinitionCache = new();

        private string GetTextureHash(byte[] pngBytes)
        {
            //MD5 hash = new MD5CryptoServiceProvider();
            SHA512 hash = new SHA512Managed();
            return BitConverter.ToString(hash.ComputeHash(pngBytes)).Replace("-", "");
        }

        private void ChangeTk2dSprite(tk2dSprite self)
        {
            DebugLog("!ChangeTk2dSprite");
            //ChangeTk2dSpriteSpriteDef(self.GetCurrentSpriteDef());

            var collection = self.GetCurrentSpriteDef();
            if (spriteDefinitionCache.ContainsKey(collection))
            {
                //Texture2D texture2D = new Texture2D(2, 2);
                //texture2D.LoadImage(spriteDefinitionCache[collection], false);
                //var tmpTex = collection.materialInst.mainTexture;
                //collection.materialInst.mainTexture = texture2D;
                //Texture2D.DestroyImmediate(tmpTex);
            }
            else
            {
                Texture2D origTex = (Texture2D) collection.materialInst.mainTexture;
                Texture2D readTex = EnemyChanger.MakeTextureReadable(origTex);
                byte[] hashBytes = readTex.GetRawTextureData();
                string spriteCollectionName = GetTextureHash(hashBytes);
                if (File.Exists($"{this._dir}/{spriteCollectionName}.png"))
                {
                    using (FileStream fileStream = new FileStream($"{this._dir}/{spriteCollectionName}.png", FileMode.Open))
                    {
                        if (fileStream != null)
                        {
                            byte[] array = new byte[fileStream.Length];
                            fileStream.Read(array, 0, array.Length);
                            Texture2D texture2D = new Texture2D(2, 2);
                            texture2D.LoadImage(array, false);
                            //var tmpTex = self.GetCurrentSpriteDef().material.mainTexture;
                            collection.materialInst.mainTexture = texture2D;
                            //Texture2D.DestroyImmediate(tmpTex);
                            spriteDefinitionCache.Add(collection, array);
                        }
                    }
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
                        DebugLog("---ChangeTk2dSpriteSpriteDef");
                    }
                }
                Object.DestroyImmediate(readTex);
            }
            DebugLog("~ChangeTk2dSprite");
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
}
