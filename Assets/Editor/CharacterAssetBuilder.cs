using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Hockey.Editor
{
    /// <summary>
    /// One-click pipeline that turns the Meshy-generated FBX under Assets/Art/Generated into
    /// game-ready prefabs in Assets/Resources/Characters (loaded by <c>SkaterVisual</c> at runtime).
    /// Per character it: configures the FBX rig import, loops the walk/run clips, builds a 1D
    /// locomotion AnimatorController (Speed: walk → run) and saves a prefab with that Animator.
    /// Props (no animation) get a plain prefab. Re-runnable and safe to run after re-generating art.
    /// Menu: Shoresy ▸ Build Character Prefabs &amp; Anims.
    /// </summary>
    public static class CharacterAssetBuilder
    {
        const string GenRoot = "Assets/Art/Generated";
        const string ResDir = "Assets/Resources/Characters";
        const string CtrlDir = "Assets/Art/Generated/_controllers";

        [MenuItem("Shoresy/Build Character Prefabs & Anims")]
        public static void Build()
        {
            if (!Directory.Exists(GenRoot))
            {
                Debug.LogWarning($"[Shoresy] No generated art at {GenRoot}.");
                return;
            }
            Directory.CreateDirectory(ResDir);
            Directory.CreateDirectory(CtrlDir);

            int built = 0;
            foreach (var dir in Directory.GetDirectories(GenRoot))
            {
                string id = Path.GetFileName(dir);
                if (id.StartsWith("_") || id.StartsWith("exp_")) continue;

                string riggedFbx = Norm(Path.Combine(dir, "rigged/character_rigged.fbx"));
                string plainFbx = Norm(Path.Combine(dir, "model/model.fbx"));
                string fbx = File.Exists(riggedFbx) ? riggedFbx : (File.Exists(plainFbx) ? plainFbx : null);
                if (fbx == null) continue;

                ConfigureModelImport(fbx);
                var src = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
                if (src == null) continue;

                RuntimeAnimatorController ctrl = BuildLocomotion(id, dir);

                var instance = Object.Instantiate(src);
                instance.name = id;
                if (ctrl != null)
                {
                    var anim = instance.GetComponentInChildren<Animator>();
                    if (anim == null) anim = instance.AddComponent<Animator>();
                    anim.runtimeAnimatorController = ctrl;
                    anim.applyRootMotion = false;
                }

                PrefabUtility.SaveAsPrefabAsset(instance, $"{ResDir}/{id}.prefab");
                Object.DestroyImmediate(instance);
                built++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Shoresy] Built {built} prefab(s) in {ResDir}.");
        }

        static void ConfigureModelImport(string fbx)
        {
            var imp = AssetImporter.GetAtPath(fbx) as ModelImporter;
            if (imp == null) return;
            imp.animationType = ModelImporterAnimationType.Generic; // Meshy ships a generic skeleton
            var clips = imp.defaultClipAnimations;
            if (clips != null && clips.Length > 0)
            {
                for (int i = 0; i < clips.Length; i++) clips[i].loopTime = true;
                imp.clipAnimations = clips;
            }
            imp.SaveAndReimport();
        }

        static RuntimeAnimatorController BuildLocomotion(string id, string dir)
        {
            string walkFbx = Norm(Path.Combine(dir, "rigged/anim_walking.fbx"));
            string runFbx = Norm(Path.Combine(dir, "rigged/anim_running.fbx"));
            if (File.Exists(walkFbx)) ConfigureModelImport(walkFbx);
            if (File.Exists(runFbx)) ConfigureModelImport(runFbx);

            AnimationClip walk = LoadClip(walkFbx);
            AnimationClip run = LoadClip(runFbx);
            if (walk == null && run == null) return null;
            walk = walk ?? run;
            run = run ?? walk;

            string path = $"{CtrlDir}/{id}_loco.controller";
            AssetDatabase.DeleteAsset(path);
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);
            ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);

            var bt = new BlendTree
            {
                name = "Locomotion",
                blendType = BlendTreeType.Simple1D,
                blendParameter = "Speed",
            };
            AssetDatabase.AddObjectToAsset(bt, ctrl);
            bt.AddChild(walk, 0f);
            bt.AddChild(run, 6f);

            var sm = ctrl.layers[0].stateMachine;
            var state = sm.AddState("Locomotion");
            state.motion = bt;
            sm.defaultState = state;
            EditorUtility.SetDirty(ctrl);
            return ctrl;
        }

        static AnimationClip LoadClip(string fbx)
        {
            if (!File.Exists(fbx)) return null;
            foreach (var a in AssetDatabase.LoadAllAssetsAtPath(fbx))
                if (a is AnimationClip c && !c.name.StartsWith("__preview")) return c;
            return null;
        }

        static string Norm(string p) => p.Replace('\\', '/');
    }
}
