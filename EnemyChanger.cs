#define SPRITEDUMPER

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SFCore.Generics;
using SFCore.Utils;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EnemyChanger
{
    class EcGlobalSettings
    {
        public bool DumpSprites = true;
    }

    class EnemyChanger : GlobalSettingsMod<EcGlobalSettings>
    {
        private readonly string _dir;
        private readonly Texture2D _emptyTex = new Texture2D(2, 2);

        public override string GetVersion() => Util.GetVersion(Assembly.GetExecutingAssembly());

#if SPRITEDUMPER
        private Dictionary<string, Sprite> _sprites = new Dictionary<string, Sprite>();
        public override List<(string, string)> GetPreloadNames()
        {
            List<(string, string)> ret = new List<(string, string)>();
            //for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings; i++)
            for (int i = 0; i < 20; i++)
            {
                if (i < 4) continue;
                if (i == 5) continue;
                if (i == 403) continue;
                if (i >= 410 && i <= 419) continue;
                if (i == 421) continue;
                if (i == 465) continue;
                if (i == 472) continue;
                if (i == 480) continue;
                if (i == 483) continue;
                if (i > 498) continue;
                ret.Add((Path.GetFileNameWithoutExtension(UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i)), "_SceneManager"));
            }
            return ret;
        }
#endif

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

#if !SPRITEDUMPER
            //On.HealthManager.Start += OnHealthManagerStart;
            //On.tk2dSprite.Awake += OnTk2dSpriteAwake;
            On.PlayMakerFSM.Start += OnPlayMakerFsmStart;
#endif
#if SPRITEDUMPER
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += (to, settings) =>
            {
                Log($"Scene '{to.name}' loaded.");
                DumpSprites();
            };
#endif
        }

        private void OnPlayMakerFsmStart(On.PlayMakerFSM.orig_Start orig, PlayMakerFSM self)
        {
            if (self.FsmName.Equals("Geo Pool"))
            {
                Log("Found Geo Pool fsm.");
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

#if SPRITEDUMPER
            GameCameras.instance.StartCoroutine(WaitForButton());
            //GameManager.instance.StartCoroutine(WaitForButton());
#endif

            DebugLog("~Initialize");
        }

#if SPRITEDUMPER
        private IEnumerator WaitForButton()
        {
            while (true)
            {
                yield return new WaitWhile(() => !Input.GetKeyDown(KeyCode.O));

                Log("Dumping sprites now.");

                yield return null;
                yield return null;

                DumpSprites();

                yield return null;
                yield return null;

                Log("Sprites dumped.");
            }
        }

        private void CollectSprites()
        {
            int i = 0;
            foreach (var item in Resources.FindObjectsOfTypeAll<SpriteRenderer>())
            {
                if (item.sprite == null) continue;

                if (_sprites.ContainsKey(item.sprite.name)) continue;

                i++;
                Object.DontDestroyOnLoad(item.sprite);
                _sprites.Add(item.sprite.name, item.sprite);
            }
            Log($"Collected {i} sprites.");
        }

        private void DumpSprites()
        {
            //foreach ((var name, var sprite) in sprites)
            foreach (var item in Resources.FindObjectsOfTypeAll<SpriteRenderer>())
            {
                var sprite = item.sprite;
                if (sprite == null) continue;
                var name = sprite.name;

                //Log($"Sprite: '{item.sprite.name}'");

                //Log($"\tOrig Size: ({tex.GetRawTextureData().Length}) @ {tex.format}");
                if (File.Exists($"{_dir}/{name}.png"))
                {
                    Log($"File '{name}.png' already exists!");
                }
                else
                {
                    var tex = ExtractTextureFromSprite(sprite);
                    SaveTex(tex, $"{_dir}/{name}.png");
                    Object.DestroyImmediate(tex);
                }
            }
        }

        private void SaveTriangle(bool[][] triangle, string spriteName, int num)
        {
            var outTex = new Texture2D(triangle[0].Length, triangle.Length);
            for (int x = 0; x < triangle[0].Length; x++)
                for (int y = 0; y < triangle.Length; y++)
                    outTex.SetPixel(x, y, triangle[y][x] ? Color.white : Color.black);
            outTex.Apply();
            SaveTex(outTex, $"{_dir}/{spriteName}/{num}.png");
            Object.DestroyImmediate(outTex);
        }

        private static float CalcTriangleArea(Vector2Int a, Vector2Int b, Vector2Int c)
        {
            return Mathf.Abs(((a.x * (b.y - c.y)) + (b.x * (c.y - a.y)) + (c.x * (a.y - b.y))) / 2f);
        }

        private Texture2D ExtractTextureFromSprite(Sprite testSprite, bool saveTriangles = false)
        {
            if (saveTriangles && !Directory.Exists($"{_dir}/{testSprite.name}")) Directory.CreateDirectory($"{_dir}/{testSprite.name}");
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
                SaveTriangle(contents, testSprite.name, 1000000);

            #endregion

            origTex = MakeTextureReadable(testSprite.texture);
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

            Object.DestroyImmediate(origTex);

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

        private static Texture2D MakeTextureReadable(Texture2D orig)
        {
            DebugLog("!makeTextureReadable");
            Texture2D ret = new Texture2D(orig.width, orig.height);
            RenderTexture tempRt = RenderTexture.GetTemporary(orig.width, orig.height, 0);
            Graphics.Blit(orig, tempRt);
            RenderTexture tmpActiveRt = RenderTexture.active;
            RenderTexture.active = tempRt;
            ret.ReadPixels(new Rect(0f, 0f, (float) tempRt.width, (float) tempRt.height), 0, 0);
            ret.Apply();
            RenderTexture.active = tmpActiveRt;
            RenderTexture.ReleaseTemporary(tempRt);
            DebugLog("~makeTextureReadable");
            return ret;
        }

        private static void SaveTex(Texture2D tex, string filename)
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
