using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Wildbond.EditorTools
{
    /// <summary>One-click project setup + content asset generation.</summary>
    public static class WildbondSetup
    {
        const string MatDir = "Assets/Resources/Materials";
        const string ContentDir = "Assets/Resources/Content";
        const string ScenePath = "Assets/Scenes/Main.unity";
        const string MenuScenePath = "Assets/Scenes/MainMenu.unity";

        [MenuItem("Wildbond/Setup Project (materials, content, scene)")]
        public static void SetupAll()
        {
            CreateMaterials();
            GenerateContent(false);
            CreateScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Wildbond] Setup complete. Open Assets/Scenes/Main.unity and press Play.");
        }

        [MenuItem("Wildbond/Generate Content (add missing)")]
        public static void GenerateMissing() => GenerateContent(false);

        [MenuItem("Wildbond/Generate Content (overwrite all)")]
        public static void GenerateOverwrite()
        {
            if (EditorUtility.DisplayDialog("Overwrite content?", "This replaces every generated content asset with ContentSeed values (your inspector tweaks will be lost).", "Overwrite", "Cancel"))
                GenerateContent(true);
        }

        static void Ensure(string dir)
        {
            if (AssetDatabase.IsValidFolder(dir)) return;
            string parent = Path.GetDirectoryName(dir).Replace('\\', '/');
            Ensure(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(dir));
        }

        // ---------------------------------------------------------------- MATERIALS
        public static void CreateMaterials()
        {
            Ensure(MatDir);
            Mat("WB_Terrain", "Wildbond/VertexLit", m => { m.SetFloat("_Rim", 0.05f); m.SetFloat("_Wrap", 0.5f); });
            Mat("WB_Props", "Wildbond/VertexLit", m => { m.enableInstancing = true; m.SetFloat("_Rim", 0.15f); });
            Mat("WB_Creature", "Wildbond/VertexLit", m => { m.SetFloat("_Rim", 0.4f); });
            Mat("WB_Building", "Wildbond/VertexLit", m => { m.SetFloat("_Rim", 0.15f); });
            Mat("WB_Ghost", "Wildbond/VertexLit", m => { m.SetFloat("_Emission", 0.6f); });
            Mat("WB_Water", "Wildbond/Water", null);
            Mat("WB_Sky", "Wildbond/Sky", null);
            Mat("WB_Particle", "Wildbond/Particle", m => RenderAssets.SetupParticle(m, false, null));
            Mat("WB_ParticleAdd", "Wildbond/Particle", m => RenderAssets.SetupParticle(m, true, null));
        }

        static void Mat(string name, string shader, System.Action<Material> cfg)
        {
            string path = $"{MatDir}/{name}.mat";
            var s = Shader.Find(shader);
            if (s == null) { Debug.LogError($"[Wildbond] Shader not found: {shader}"); return; }
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(s);
                AssetDatabase.CreateAsset(m, path);
            }
            m.shader = s;
            cfg?.Invoke(m);
            EditorUtility.SetDirty(m);
        }

        // ---------------------------------------------------------------- CONTENT
        public static void GenerateContent(bool overwrite)
        {
            var b = ContentSeed.Build();
            int created = 0;
            created += Save(b.items, "Items", overwrite);
            created += Save(b.recipes, "Recipes", overwrite);
            created += Save(b.abilities, "Abilities", overwrite);
            created += Save(b.traits, "Traits", overwrite);
            created += Save(b.species, "Species", overwrite);
            created += Save(b.buildings, "Buildings", overwrite);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Wildbond] Content generation: {created} assets written to {ContentDir}.");
        }

        static int Save<T>(System.Collections.Generic.List<T> list, string sub, bool overwrite) where T : ScriptableObject
        {
            string dir = $"{ContentDir}/{sub}";
            Ensure(dir);
            int n = 0;
            foreach (var so in list)
            {
                string path = $"{dir}/{so.name}.asset";
                var existing = AssetDatabase.LoadAssetAtPath<T>(path);
                if (existing != null)
                {
                    if (!overwrite) continue;
                    EditorUtility.CopySerialized(so, existing);
                    existing.name = so.name;
                    EditorUtility.SetDirty(existing);
                }
                else AssetDatabase.CreateAsset(so, path);
                n++;
            }
            return n;
        }

        // ---------------------------------------------------------------- SCENE
        public static void CreateScene()
        {
            Ensure("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = new GameObject("Game");
            go.AddComponent<GameManager>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            CreateMenuScene();
        }

        [MenuItem("Wildbond/Create Main Menu Scene")]
        public static void CreateMenuScene()
        {
            Ensure("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("MainMenu").AddComponent<MainMenu>();
            EditorSceneManager.SaveScene(scene, MenuScenePath);
            // menu first so the player build boots into it
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(MenuScenePath, true), new EditorBuildSettingsScene(ScenePath, true) };
        }
    }
}
