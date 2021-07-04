#define SPRITEDUMPER

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using IL.InControl;
using Modding;
using SFCore.Generics;
using SFCore.Utils;
using UnityEngine;

namespace EnemyChanger
{
    class ECGlobalSettings
    {
        public bool DumpSprites = true;
    }

    class EnemyChanger : GlobalSettingsMod<ECGlobalSettings>
    {
        private readonly string DIR;
        private readonly string FOLDER = "SpriteChanger";
        private readonly Texture2D emptyTex = new Texture2D(2, 2);

        public override string GetVersion() => SFCore.Utils.Util.GetVersion(Assembly.GetExecutingAssembly());

        public EnemyChanger() : base("Enemy Changer")
        {
            for (int x = 0; x < emptyTex.width; x++)
            {
                for (int y = 0; y < emptyTex.height; y++)
                {
                    emptyTex.SetPixel(x, y, new Color(0, 0, 0, 0));
                }
            }
            emptyTex.Apply(true);
            switch (SystemInfo.operatingSystemFamily)
            {
                case OperatingSystemFamily.MacOSX:
                    DIR = Path.GetFullPath(Application.dataPath + "/Resources/Data/Managed/Mods/" + FOLDER);
                    break;
                default:
                    DIR = Path.GetFullPath(Application.dataPath + "/Managed/Mods/" + FOLDER);
                    break;
            }

            if (!Directory.Exists(DIR)) Directory.CreateDirectory(DIR);

#if !SPRITEDUMPER

            //On.HealthManager.Start += OnHealthManagerStart;
            //On.tk2dSprite.Awake += OnTk2dSpriteAwake;
            On.PlayMakerFSM.Start += OnPlayMakerFsmStart;

#endif
        }

        private void OnPlayMakerFsmStart(On.PlayMakerFSM.orig_Start orig, PlayMakerFSM self)
        {
            if (self.FsmName.Equals("Geo Pool"))
            {
                Log("Found Geo Pool fsm!");
                self.MakeLog();
            }

            orig(self);
        }

#if !SPRITEDUMPER

        //private void OnHealthManagerStart(On.HealthManager.orig_Start orig, HealthManager self)
        //{
        //    DebugLog("!OnHealthManagerStart");
        //    orig(self);

        //    foreach (var ts in self.gameObject.GetComponentsInChildren<tk2dSprite>())
        //    {
        //        ChangeTk2dSprite(ts);
        //    }
        //    DebugLog("~OnHealthManagerStart");
        //}

        private void OnTk2dSpriteAwake(On.tk2dSprite.orig_Awake orig, tk2dSprite self)
        {
            DebugLog("!OnTk2dSpriteAwake");
            orig(self);

            ChangeTk2dSprite(self);

            DebugLog("~OnTk2dSpriteAwake");
        }

#endif

        public override void Initialize()
        {
            DebugLog("!Initialize");
            //foreach (var text in Resources.LoadAll<TextAsset>(""))
            //{
            //    Log($"Text name: '{text.name}'");

            //    StreamWriter sw = new StreamWriter(new FileStream($"D:\\_ModdingTools\\HK_Text\\{text.name}.txt", FileMode.Create));
            //    sw.Write(text.text);
            //    sw.Close();
            //}

#if SPRITEDUMPER

            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += (from, to) =>
            {
                dumpSprites();
            };

#endif

            DebugLog("~Initialize");
        }

#if SPRITEDUMPER

        private void dumpSprites()
        {
            foreach (var item in Resources.FindObjectsOfTypeAll<SpriteRenderer>())
            {
                if (item.sprite == null) continue;

                //Log($"Sprite: '{item.sprite.name}'");

                //Log($"\tOrig Size: ({tex.GetRawTextureData().Length}) @ {tex.format}");
                if (File.Exists($"{this.DIR}/{item.sprite.name}.png"))
                {
                    Log($"File '{item.sprite.name}.png' already exists!");
                }
                else
                {
                    var tex = ExtractTextureFromSprite(item.sprite);
                    saveTex(tex, $"{this.DIR}/{item.sprite.name}.png");
                    Texture2D.DestroyImmediate(tex);
                }
            }
        }

        private void saveTriangle(bool[][] triangle, string spriteName, int num)
        {
            var outTex = new Texture2D(triangle[0].Length, triangle.Length);
            for (int x = 0; x < triangle[0].Length; x++)
                for (int y = 0; y < triangle.Length; y++)
                    outTex.SetPixel(x, y, triangle[y][x] ? Color.white : Color.black);
            outTex.Apply();
            saveTex(outTex, $"{DIR}/{spriteName}/{num}.png");
            Texture2D.DestroyImmediate(outTex);
        }

        private static float CalcTriangleArea(Vector2Int a, Vector2Int b, Vector2Int c)
        {
            return Mathf.Abs(((a.x * (b.y - c.y)) + (b.x * (c.y - a.y)) + (c.x * (a.y - b.y))) / 2f);
        }

        private Texture2D ExtractTextureFromSprite(Sprite testSprite, bool saveTriangles = false)
        {
            if (saveTriangles && !Directory.Exists($"{DIR}/{testSprite.name}")) Directory.CreateDirectory($"{DIR}/{testSprite.name}");
            var testSpriteRect = (testSprite.texture.width, testSprite.texture.height);
            List<Vector2Int> texUVs = new List<Vector2Int>();
            List<(Vector2Int, Vector2Int, Vector2Int)> triangles = new List<(Vector2Int, Vector2Int, Vector2Int)>();
            int i;
            bool[][] contents;
            bool[][] triangle;
            float triangleArea;
            float pab, pbc, pac;
            Vector2Int p;
            int x, y;
            int minX, maxX, minY, maxY;
            int width, height;
            Texture2D origTex, outTex;

            foreach (var item in testSprite.uv)
            {
                texUVs.Add(new Vector2Int(Mathf.RoundToInt(item.x * (testSpriteRect.width - 1)), Mathf.RoundToInt(item.y * (testSpriteRect.height - 1))));
            }
            for (i = 0; i < testSprite.triangles.Length; i += 3)
            {
                triangles.Add((texUVs[testSprite.triangles[i]], texUVs[testSprite.triangles[i+1]], texUVs[testSprite.triangles[i+2]]));
            }

            minX = texUVs.Select(uv => uv.x).ToList().Min();
            maxX = texUVs.Select(uv => uv.x).ToList().Max();
            minY = texUVs.Select(uv => uv.y).ToList().Min();
            maxY = texUVs.Select(uv => uv.y).ToList().Max();
            width = maxX - minX + 1;
            height = maxY - minY + 1;

            #region Make bool array of important contents

            contents = new bool[height][];
            for (i = 0; i < contents.Length; i++)
                contents[i] = new bool[width];
            int triangleCounter = 0;
            foreach (var item in triangles)
            {
                //triangleCounter++;
                //triangle = new bool[height][];
                //for (i = 0; i < triangle.Length; i++)
                //    triangle[i] = new bool[width];

                triangleArea = CalcTriangleArea(item.Item1, item.Item2, item.Item3);
                for (x = 0; x < width; x++)
                {
                    for (y = 0; y < height; y++)
                    {
                        p = new Vector2Int(minX + x, minY + y);
                        pab = CalcTriangleArea(item.Item1, item.Item2, p);
                        pbc = CalcTriangleArea(p, item.Item2, item.Item3);
                        pac = CalcTriangleArea(item.Item1, p, item.Item3);
                        if ((pab + pbc + pac) == triangleArea)
                        {
                            //triangle[y][x] = true;
                            contents[y][x] = true;
                        }
                    }
                }

                //if (saveTriangles)
                //    saveTriangle(triangle, testSprite.name, triangleCounter);

                //for (x = 0; x < width; x++)
                //    for (y = 0; y < height; y++)
                //        contents[y][x] |= triangle[y][x];
            }
            if (saveTriangles)
                saveTriangle(contents, testSprite.name, 1000000);

            #endregion

            origTex = makeTextureReadable(testSprite.texture);
            outTex = new Texture2D(width, height);

            for (x = 0; x < width; x++)
            {
                for (y = 0; y < height; y++)
                {
                    if (!contents[y][x])
                        outTex.SetPixel(x, y, new Color(0, 0, 0, 0));
                    else
                        outTex.SetPixel(x, y, origTex.GetPixel(minX + x, minY + y));
                }
            }
            outTex.Apply();

            Texture2D.DestroyImmediate(origTex);

            return outTex;
        }

#endif

#if !SPRITEDUMPER

        private string GetGameObjectBaseName(GameObject go)
        {
            DebugLog("!GetGameObjectBaseName");
            string ret = go.name;
            ret = ret.ToLower();
            ret.Replace("(clone)", "");
            ret = ret.Trim();
            ret.Replace("Cln", "");
            ret = ret.Trim();
            ret = Regex.Replace(ret, @"\([0-9+]+\)", "");
            ret = ret.Trim();
            ret = Regex.Replace(ret, @"[0-9+]+$", "");
            ret = ret.Trim();
            DebugLog("~GetGameObjectBaseName");
            return ret;
        }

        private void ChangeTk2dSprite(tk2dSprite self)
        {
            DebugLog("!ChangeTk2dSprite");
            string spriteCollectionName = $"{self.Collection.spriteCollectionName} - {GetGameObjectBaseName(self.gameObject)}";
            if (File.Exists($"{this.DIR}/{spriteCollectionName}.png"))
            {
                using (FileStream fileStream = new FileStream($"{this.DIR}/{spriteCollectionName}.png", FileMode.Open))
                {
                    if (fileStream != null)
                    {
                        byte[] array = new byte[fileStream.Length];
                        fileStream.Read(array, 0, array.Length);
                        Texture2D texture2D = new Texture2D(2, 2);
                        texture2D.LoadImage(array, false);
                        var tmpTex = self.GetCurrentSpriteDef().material.mainTexture;
                        self.GetCurrentSpriteDef().material.mainTexture = texture2D;
                        Texture2D.DestroyImmediate(tmpTex);
                    }
                }
            }
            else if (_globalSettings.DumpSprites)
            {
                try
                {
                    Texture2D tex =
                        EnemyChanger.makeTextureReadable(
                            (Texture2D) self.GetCurrentSpriteDef().material.mainTexture);

                    saveTex(tex, $"{this.DIR}/{spriteCollectionName}.png");
                    Texture2D.DestroyImmediate(tex);
                }
                catch (Exception)
                {
                    DebugLog("---makeTextureReadable");
                }
            }
            DebugLog("~ChangeTk2dSprite");
        }

#endif

        private static Texture2D makeTextureReadable(Texture2D orig)
        {
            DebugLog("!makeTextureReadable");
            Texture2D ret = new Texture2D(orig.width, orig.height);
            RenderTexture tempRT = RenderTexture.GetTemporary(orig.width, orig.height, 0);
            Graphics.Blit(orig, tempRT);
            RenderTexture tmpActiveRT = RenderTexture.active;
            RenderTexture.active = tempRT;
            ret.ReadPixels(new Rect(0f, 0f, (float) tempRT.width, (float) tempRT.height), 0, 0);
            ret.Apply();
            RenderTexture.active = tmpActiveRT;
            RenderTexture.ReleaseTemporary(tempRT);
            DebugLog("~makeTextureReadable");
            return ret;
        }

        private static void saveTex(Texture2D tex, string filename)
        {
            DebugLog("!saveTex");
            using (FileStream fileStream2 = new FileStream(filename, FileMode.Create))
            {
                if (fileStream2 != null)
                {
                    byte[] array2 = tex.EncodeToPNG();
                    fileStream2.Write(array2, 0, array2.Length);
                }
            }
            DebugLog("~saveTex");
        }

        private static void DebugLog(string msg)
        {
            Modding.Logger.Log($"[{typeof(EnemyChanger).FullName.Replace(".", "][")}] - {msg}");
            Debug.Log($"[{typeof(EnemyChanger).FullName.Replace(".", "][")}] - {msg}");
        }
        private static void DebugLog(object msg)
        {
            DebugLog($"{msg}");
        }
    }
}
