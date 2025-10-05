#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Linq;

public static class ListFbxClips
{
    [MenuItem("Tools/Astronaut/List FBX Clips")]
    public static void Run()
    {
        foreach (var fbx in new[]
                 {
                     "A@Pose_Idle.fbx",
                     "A@Walk_F.fbx",
                     "A@Run_F.fbx",
                     "A@Run_F_to_Idle.fbx"
                 })
        {
            string path = "Assets/Animations/" + fbx;
            var clips = AssetDatabase.LoadAllAssetRepresentationsAtPath(path)
                .OfType<AnimationClip>()
                .Select(c => c.name)
                .ToArray();

            Debug.Log(fbx + ": " + string.Join(", ", clips));
        }
    }
}
#endif