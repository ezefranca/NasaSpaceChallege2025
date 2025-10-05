#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using System.Linq;

public static class AstronautAnimatorBuilder
{
    const string AnimFolder     = "Assets/Animations/";
    const string ControllerPath = AnimFolder + "Astronaut_Auto.controller";

    // ===== MENU: Delete all controllers in Animations folder =====
    [MenuItem("Tools/Astronaut/Delete All Controllers")]
    public static void DeleteAllControllers()
    {
        var guids = AssetDatabase.FindAssets("t:AnimatorController", new[] { AnimFolder });
        int deleted = 0;
        foreach (var g in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(g);
            if (AssetDatabase.DeleteAsset(path)) deleted++;
        }
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Controllers Deleted", $"Deleted {deleted} AnimatorController(s) in {AnimFolder}", "OK");
    }

    // ===== MENU: Force Loop Time ON for walk/run clips =====
    [MenuItem("Tools/Astronaut/Fix Import (Loop Walk/Run)")]
    public static void FixImportLoopFlags()
    {
        // Force-loop Walk_F and Run_F
        ForceLoopForClip("A@Walk_F.fbx", "Walk_F", true);
        ForceLoopForClip("A@Run_F.fbx",  "Run_F",  true);
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Import Fixed", "Loop Time enabled for Walk_F and Run_F.", "OK");
    }

    // ===== MENU: Rebuild clean Animator and assign to selection =====
    [MenuItem("Tools/Astronaut/Rebuild Clean Animator + Assign")]
    public static void RebuildAndAssign()
    {
        // Load exact clips by internal names you listed
        var idle = LoadClip("A@Pose_Idle.fbx",     "Pose_Idle");
        var walk = LoadClip("A@Walk_F.fbx",        "Walk_F");
        var run  = LoadClip("A@Run_F.fbx",         "Run_F");
        var jump = LoadClip("A@Run_F_to_Idle.fbx", "Run_F_to_Idle"); // placeholder (asset has no true jump)

        if (!idle || !walk || !run)
        {
            EditorUtility.DisplayDialog("Missing Clips",
                "Could not load Pose_Idle, Walk_F, or Run_F from Assets/Animations/.", "OK");
            return;
        }

        // Delete existing controller
        var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (existing) AssetDatabase.DeleteAsset(ControllerPath);

        // Create controller
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        var sm   = ctrl.layers[0].stateMachine;

        // Parameters
        ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);   // 0..1 (normalized by run speed)
        ctrl.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("Jump", AnimatorControllerParameterType.Trigger);

        // States
        var idleState = sm.AddState("Idle", new Vector3(0,100)); idleState.motion = idle; sm.defaultState = idleState;

        // Locomotion blend (walk at 0.5, run at 1.0)
        var bt = new BlendTree { name = "Locomotion", blendType = BlendTreeType.Simple1D, useAutomaticThresholds = false, blendParameter = "Speed" };
        AssetDatabase.AddObjectToAsset(bt, ctrl);
        bt.AddChild(walk, 0.5f);
        bt.AddChild(run,  1.0f);
        var loco = sm.AddState("Locomotion", new Vector3(300,100)); loco.motion = bt;

        // Transitions Idle <-> Locomotion
        var toLoco = idleState.AddTransition(loco); toLoco.hasExitTime = false; toLoco.duration = 0.08f; toLoco.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        var toIdle = loco.AddTransition(idleState); toIdle.hasExitTime = false; toIdle.duration = 0.08f; toIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

        // Jump (placeholder using Run_F_to_Idle if present)
        if (jump)
        {
            var jumpState = sm.AddState("Jump", new Vector3(300,-50)); jumpState.motion = jump;
            var anyToJump = sm.AddAnyStateTransition(jumpState); anyToJump.hasExitTime = false; anyToJump.duration = 0.05f; anyToJump.AddCondition(AnimatorConditionMode.If, 0, "Jump");
            var jumpToLoco = jumpState.AddTransition(loco); jumpToLoco.hasExitTime = true; jumpToLoco.exitTime = 0.9f; jumpToLoco.duration = 0.08f;
        }

        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();

        // Assign to selected Astronaut
        var go = Selection.activeGameObject;
        if (!go) { EditorUtility.DisplayDialog("Select Astronaut", "Select your Astronaut root in Hierarchy and run again.", "OK"); return; }
        var anim = go.GetComponentInChildren<Animator>();
        if (!anim) { EditorUtility.DisplayDialog("No Animator", "Animator not found under selection.", "OK"); return; }

        anim.runtimeAnimatorController = ctrl;
        anim.applyRootMotion = false;

        EditorUtility.DisplayDialog("Done", $"Animator rebuilt and assigned:\n{ControllerPath}", "OK");
    }

    // ===== MENU: Print which controller is assigned =====
    [MenuItem("Tools/Astronaut/Print Assigned Controller")]
    public static void PrintAssigned()
    {
        var go = Selection.activeGameObject;
        if (!go) { Debug.Log("Select your Astronaut"); return; }
        var anim = go.GetComponentInChildren<Animator>();
        if (!anim) { Debug.Log("No Animator under selection."); return; }
        var ctrl = anim.runtimeAnimatorController;
        Debug.Log($"Animator on '{anim.gameObject.name}': {(ctrl ? ctrl.name : "<none>")} @ {AssetDatabase.GetAssetPath(ctrl)}");
    }

    // ---------- helpers ----------
    static AnimationClip LoadClip(string fbx, string clipName)
    {
        var reps = AssetDatabase.LoadAllAssetRepresentationsAtPath(AnimFolder + fbx);
        return reps.OfType<AnimationClip>().FirstOrDefault(c => c.name == clipName);
    }

    static void ForceLoopForClip(string fbx, string clipName, bool loop)
    {
        var path = AnimFolder + fbx;
        var importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null) { Debug.LogWarning($"No importer for {path}"); return; }

        // pull all default clips, flip the one we want, and write back as explicit clips
        var defaults = importer.defaultClipAnimations;
        if (defaults == null || defaults.Length == 0)
        {
            // if none, try clipping from take infos
            var takes = importer.clipAnimations;
            if (takes == null || takes.Length == 0) { Debug.LogWarning($"No clips found inside {fbx}"); return; }
            defaults = takes;
        }

        for (int i = 0; i < defaults.Length; i++)
        {
            if (defaults[i].name == clipName)
            {
                var c = defaults[i];
                c.loopTime = loop;
                defaults[i] = c;
            }
        }

        importer.clipAnimations = defaults; // write back as explicit clips
        importer.SaveAndReimport();
    }
}
#endif