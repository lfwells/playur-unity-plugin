using UnityEngine;
using UnityEditor;
using System.Linq;

namespace PlayUR.Editor
{
    public class PlayUREditorUtils
    {
        //from chatgpt:
        public static int CompareSemanticVersions(string version1, string version2)
        {
            string[] parts1 = version1.Split('.');
            string[] parts2 = version2.Split('.');

            for (int i = 0; i < Mathf.Max(parts1.Length, parts2.Length); i++)
            {
                int part1 = i < parts1.Length ? int.Parse(parts1[i]) : 0;
                int part2 = i < parts2.Length ? int.Parse(parts2[i]) : 0;

                if (part1 < part2)
                {
                    return -1;
                }
                else if (part1 > part2)
                {
                    return 1;
                }
            }

            return 0;

        }

        public static void OpenPackageManager()
        {
            UnityEditor.PackageManager.UI.Window.Open("io.playur.unity");
        }


        static UnityEditor.Build.NamedBuildTarget[] platforms = new UnityEditor.Build.NamedBuildTarget[] {
            UnityEditor.Build.NamedBuildTarget.Android,
            UnityEditor.Build.NamedBuildTarget.iOS,
            UnityEditor.Build.NamedBuildTarget.Standalone,
            UnityEditor.Build.NamedBuildTarget.WindowsStoreApps,
            UnityEditor.Build.NamedBuildTarget.WebGL
        };
        public static void AddScriptingDefinesForAllPlatforms(string define)
        {
            foreach (var platform in platforms)
            {
                AddScriptingDefineForPlatform(platform, define);
            }
        }
        static void AddScriptingDefineForPlatform(UnityEditor.Build.NamedBuildTarget buildTarget, string define)
        {
            var defines = PlayerSettings.GetScriptingDefineSymbols(buildTarget).Split(";").ToList();
            if (defines.Contains(define)) return;
            defines.Add(define);
            PlayerSettings.SetScriptingDefineSymbols(buildTarget, string.Join(";", defines));
        }
    }
}