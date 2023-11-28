using UnityEngine;
using UnityEditor;
using System.Linq;
using NUnit.Framework;
using System;

namespace PlayUR.Editor
{
    public class PlayUREditorUtils
    {
        #region Versioning
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
        #endregion

        #region Scripting Defines
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
        #endregion

        #region Enum Generation
        public static string PlatformNameToValidEnumValue(string input)
        {
            var rgx = new System.Text.RegularExpressions.Regex("[^a-zA-Z0-9_]");
            input = rgx.Replace(input, "");
            input = input.Replace(" ", "");
            if (!char.IsLetter(input[0])) input = "_" + input;
            return input;
        }
        /// <summary>test 
        /// hjh
        /// </summary>
        public static string DescriptionToCommentSafe(string input, int indent = 0, int id = -1, string table = null, string prePath = null)
        {
            string url = PlayURPlugin.DASHBOARD_URL + prePath + "/" + table + "/" + ((id == 0) ? "" : id);
            string URL = id >= 0 ? "\n<para><see href=\"" + url + "\">" + url + "</see></para>\n" : "";

            if (string.IsNullOrEmpty(input))
            {
                input = "<summary>" + URL + "</summary>";
            }
            else
            {
                input = "<summary>" + input + URL + "</summary>";
            }
            string[] indents = new string[indent];
            Array.Fill(indents, "\t");

            return string.Join("\n", input.Split("\n").Select(s => string.Join("", indents)+"/// " + s)) + "\n";
        }
        #endregion
    }
}