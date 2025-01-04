using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using UnityEditor.Build.Reporting;
using Ionic.Zip; // this uses the Unity port of DotNetZip https://github.com/r2d2rigo/dotnetzip-for-unity
using Unity.EditorCoroutines.Editor;
using System.IO;
using UnityEditor.SceneManagement;
using System.Text;
using PlayUR.Core;
using PlayUR.Exceptions;
using System.Diagnostics;
using UnityEditor.PackageManager;
using System.Linq;

namespace PlayUR.Editor
{
    public class PlayURPluginEditor : MonoBehaviour
    {
        #region Initial Set Up
        public static string PluginLocation = "Packages/io.playur.unity/";
        static string generatedFilesPath => Path.Combine("Assets","PlayURPlugin");
        static string GeneratedFilesPath(string subPath)
        {
            return Path.Combine(generatedFilesPath, subPath);
        }

        [MenuItem("PlayUR/Plugin Configuration...",priority=0)]
        public static void ReSetUpPlugin()
        {
            SettingsService.OpenProjectSettings("Project/PlayUR");
        }

        //Set PlayURPlugin to run first
        static void SetExecutionOrder()
        {
            //TODO:set order
            /*
            MonoScript playURScript = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
            MonoImporter.SetExecutionOrder(playURScript, -10000);

            playURScript = AssetDatabase.LoadAssetAtPath<MonoScript>(path.Replace("PlayURPlugin.cs", "Core/PlayURPluginHelper.cs"));
            MonoImporter.SetExecutionOrder(playURScript, -9999);
            */
            PlayURPlugin.Log("Set execution order of PlayUR Plugin to -10000");
        }


        //add PlayURLogin to Build Settings
        static string LoginScenePath => Path.Combine("Assets", "PlayURLogin.unity");
        public static void SetSceneBuildSettings()
        {
            var scenePath = PluginLocation + "Runtime/" + LoginScenePath;
            var scenes = new List<EditorBuildSettingsScene>();
            scenes.AddRange(EditorBuildSettings.scenes);

            //check it doesn't already exist
            for (var i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].path.Contains("PlayURLogin"))
                {
                    return;
                }
            }
            scenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            PlayURPlugin.Log("Added 'PlayURLogin' to Build Settings");

        }

        [MenuItem("PlayUR/Re-generate Enums and Schema Definitions", priority = 11)]
        public static void GenerateEnum()
        {
            var runner = new CoroutineRunner();
            var GET = "?gameID=" + PlayURPlugin.GameID + "&clientSecret=" + PlayURPlugin.ClientSecret;

            var GENERATED_FILE_HEADER = "// GENERATED CODE, DO NOT MODIFY\n\n\n#if PLAYUR_GENERATED //this comment is a safeguard to only use this file if enums have been properly generated for the project.\nnamespace PlayUR\n{\n";
            var GENERATED_FILE_FOOTER = "\n}\n#endif";

            completeCount = 0;

            //get actions from the server and populate an enum
            EditorCoroutineUtility.StartCoroutine(EditorRest.Get("Action/listForGame.php" + GET, null, (succ, json) =>
            {
                if (succ)
                {
                    var actions = json["records"].AsArray;
                    string text = GENERATED_FILE_HEADER + "\t///<summary>Enum generated from server representing possible user actions. To update use PlayUR\\Re-generate Enums.</summary>\n\tpublic enum Action\n\t{\n";
                    foreach (var action in actions.Values)
                    {
                        text += PlayUREditorUtils.DescriptionToCommentSafe(action["description"], indent: 2, id: action["id"], "Action", "Game/"+PlayURPlugin.GameID);
                        text += "\t\t" + PlayUREditorUtils.PlatformNameToValidEnumValue(action["name"].Value) + " = " + action["id"] + ",\n";
                    }
                    text += "\t}" + GENERATED_FILE_FOOTER;

                    //write it out!
                    File.WriteAllBytes(GeneratedFilesPath("Action.cs"), Encoding.UTF8.GetBytes(text));

                    PlayURPlugin.Log("Generated Actions Enum (" + actions.Count + " actions)");
                    completeCount++;
                }
            }), runner);

            //get elements from the server and populate an enum
            EditorCoroutineUtility.StartCoroutine(EditorRest.Get("Element/listForGame.php" + GET, null, (succ, json) =>
            {
                if (succ)
                {
                    var elements = json["records"].AsArray;
                    string text = GENERATED_FILE_HEADER + "\t///<summary>Enum generated from server representing top-level game elements. To update use PlayUR\\Re-generate Enums.</summary>\n\tpublic enum Element\n\t{\n";
                    foreach (var element in elements.Values)
                    {
                        text += PlayUREditorUtils.DescriptionToCommentSafe(element["description"], indent: 2, id: element["id"], "GameElement", "Game/" + PlayURPlugin.GameID);
                        text += "\t\t" + PlayUREditorUtils.PlatformNameToValidEnumValue(element["name"].Value) + " = " + element["id"] + ",\n";
                    }
                    text += "\t}" + GENERATED_FILE_FOOTER;

                    //write it out!
                    File.WriteAllBytes(GeneratedFilesPath("Element.cs"), Encoding.UTF8.GetBytes(text));

                    PlayURPlugin.Log("Generated Elements Enum (" + elements.Count + " actions)");
                    completeCount++;
                }
            }), runner);

            //get experiments from the server and populate an enum
            EditorCoroutineUtility.StartCoroutine(EditorRest.Get("Experiment/listForGame.php" + GET, null, (succ, json) =>
            {
                if (succ)
                {
                    var experiments = json["records"].AsArray;
                    string text = GENERATED_FILE_HEADER + "\t///<summary>Enum generated from server representing experiments for this game. To update use PlayUR\\Re-generate Enums.</summary>\n\tpublic enum Experiment\n\t{\n";
                    foreach (var experiment in experiments.Values)
                    {
                        text += PlayUREditorUtils.DescriptionToCommentSafe(experiment["description"], indent: 2, id: experiment["id"], "Experiment", "Game/" + PlayURPlugin.GameID);
                        text += "\t\t" + PlayUREditorUtils.PlatformNameToValidEnumValue(experiment["name"].Value) + " = " + experiment["id"] + ",\n";
                    }
                    text += "\t}" + GENERATED_FILE_FOOTER;

                    //write it out!
                    File.WriteAllBytes(GeneratedFilesPath("Experiment.cs"), Encoding.UTF8.GetBytes(text));

                    PlayURPlugin.Log("Generated Experiments Enum (" + experiments.Count + " experiments)");
                    completeCount++;
                }
            }), runner);

            //get experiment groups from the server and populate an enum
            EditorCoroutineUtility.StartCoroutine(EditorRest.Get("ExperimentGroup/listForGame.php" + GET, null, (succ, json) =>
            {
                if (succ)
                {
                    var groups = json["records"].AsArray;
                    string text = GENERATED_FILE_HEADER + "\t///<summary>Enum generated from server representing experiment groups for this game. To update use PlayUR\\Re-generate Enums.</summary>\n\tpublic enum ExperimentGroup\n\t{\n";
                    foreach (var group in groups.Values)
                    {
                        text += PlayUREditorUtils.DescriptionToCommentSafe(group["description"], indent: 2, id: group["id"], "ExperimentGroup", "Game/" + PlayURPlugin.GameID +"/"+ group["experimentID"].Value);
                        text += "\t\t" + PlayUREditorUtils.PlatformNameToValidEnumValue(group["experiment"].Value) + "_" + group["name"].Value.Replace(" ", "") + " = " + group["id"] + ",\n";
                    }
                    text += "\t}" + GENERATED_FILE_FOOTER;

                    //write it out!
                    File.WriteAllBytes(GeneratedFilesPath("ExperimentGroup.cs"), Encoding.UTF8.GetBytes(text));

                    PlayURPlugin.Log("Generated Experiment Groups Enum (" + groups.Count + " groups)");
                    completeCount++;
                }
            }), runner);

            //get analytics columns from the server and populate an enum
            EditorCoroutineUtility.StartCoroutine(EditorRest.Get("AnalyticsColumn/listForGame.php" + GET, null, (succ, json) =>
            {
                if (succ)
                {
                    var columns = json["records"].AsArray;
                    string text = GENERATED_FILE_HEADER + "\t///<summary>Enum generated from server representing the extra analytics columns used for this game. To update use PlayUR\\Re-generate Enums.</summary>\n\tpublic enum AnalyticsColumn\n\t{\n";
                    foreach (var column in columns.Values)
                    {
                        text += PlayUREditorUtils.DescriptionToCommentSafe(column["description"], indent: 2, id: column["id"], "AnalyticsColumn", "Game/" + PlayURPlugin.GameID);
                        text += "\t\t" + PlayUREditorUtils.PlatformNameToValidEnumValue(column["name"].Value) + " = " + column["id"] + ",\n";
                    }
                    text += "\t}" + GENERATED_FILE_FOOTER;

                    //write it out!
                    File.WriteAllBytes(GeneratedFilesPath("AnalyticsColumns.cs"), Encoding.UTF8.GetBytes(text));

                    PlayURPlugin.Log("Generated Analytics Columns Enum (" + columns.Count + " columns)");
                    completeCount++;
                }
            }), runner);

            //get all parameter keys from the server and populate an enum
            EditorCoroutineUtility.StartCoroutine(EditorRest.Get("GameParameter/listParameterKeys.php" + GET, null, (succ, json) =>
            {
                if (succ)
                {
                    var parameters = json["records"].AsArray;
                    string text = GENERATED_FILE_HEADER + "\t///<summary>Constant Strings generated from server representing the parameter keys for this game. To update use PlayUR\\Re-generate Enums.</summary>\n\tpublic static class Parameter\n\t{\n";
                    foreach (var parameter in parameters.Values)
                    {

                        string c = null;
                        if (!string.IsNullOrEmpty(parameter["type"]))
                        {
                            c = $"{parameter["description"].Value}. Expected type: {parameter["typeString"].Value}";
                        }
                        text += PlayUREditorUtils.DescriptionToCommentSafe(c, indent: 2, id: -1, "Parameter", "Game/" + PlayURPlugin.GameID);
                        text += "\t\tpublic static string " + PlayUREditorUtils.PlatformNameToValidEnumValue(parameter["parameter"].Value.Replace("[]", "")) + " = \"" + PlayUREditorUtils.PlatformNameToValidEnumValue(parameter["parameter"].Value.Replace("[]", "")) + "\";\n";
                    }
                    text += "\t}" + GENERATED_FILE_FOOTER;

                    //write it out!
                    File.WriteAllBytes(GeneratedFilesPath("Parameter.cs"), Encoding.UTF8.GetBytes(text));

                    PlayURPlugin.Log("Generated Parameters Constants (" + parameters.Count + " parameters)");
                    completeCount++;

                    //also generate a structure
                    text = GENERATED_FILE_HEADER + "\t///<summary>A generated datastructure containing all known parameters. Values are populated at runtime when plugin is Ready. To update use PlayUR\\Re-generate Enums.</summary>\n\tpublic static partial class Parameters\n\t{\n";
                    foreach (var parameter in parameters.Values)
                    {

                        string type = "string";
                        if (!string.IsNullOrEmpty(parameter["type"]))
                        {
                            type = parameter["typeString"].Value.ToLower();
                            if (type == "boolean") type = "bool";

                            if (parameter["parameter"].Value.EndsWith("[]"))
                            {
                                type += "[]";
                            }
                        }
                        text += PlayUREditorUtils.DescriptionToCommentSafe(parameter["description"].Value, indent: 2, id: 0, "GameParameter", "Game/" + PlayURPlugin.GameID);
                        text += $"\t\tpublic static {type} {PlayUREditorUtils.PlatformNameToValidEnumValue(parameter["parameter"].Value.Replace("[]", ""))};\n";
                    }
                    text += "\t}" + GENERATED_FILE_FOOTER;

                    //write it out!
                    File.WriteAllBytes(GeneratedFilesPath("Parameters.cs"), Encoding.UTF8.GetBytes(text));

                    PlayURPlugin.Log("Generated Configuration Structure (" + parameters.Count + " parameters)");
                    completeCount++;
                }
            }), runner);


            //get all schemas from the server and populate c# classes
            EditorCoroutineUtility.StartCoroutine(EditorRest.Get("ParameterSchema/listForGame.php" + GET+"&generate=true", null, (succ, json) =>
            {
                if (succ)
                {
                    var schemas = json["records"].AsArray;
                    foreach (var schema in schemas.Values)
                    {
                        var name = schema["name"];
                        //make the name safe for a c# filename
                        name = PlayUREditorUtils.PlatformNameToValidEnumValue(name.Value);
                        //print(schema["csharpCode"]);

                        //create folder Schemas if doesn't exist
                        if (!Directory.Exists(GeneratedFilesPath("Schemas")))
                        {
                            Directory.CreateDirectory(GeneratedFilesPath("Schemas"));
                        }

                        var code = "using System;\nusing System.Collections.Generic;\n\n" + schema["csharpCode"];
                        code = code.Replace("using Microsoft.Azure.Mobile.Server;", "");

                        File.WriteAllBytes(GeneratedFilesPath("Schemas/"+name+".cs"), Encoding.UTF8.GetBytes(code));
                        PlayURPlugin.Log("Generated Schema Class File "+name+".cs");

                    }
                    completeCount++;
                }
            }), runner);

            EditorApplication.update += AwaitGeneratedEnums;
        }

        static int completeCount;
        static void AwaitGeneratedEnums()
        {
            if (completeCount < 8)
            {
                EditorUtility.DisplayProgressBar("Generating Enums", $"{completeCount}/7", completeCount / 7f);
                return;
            }
            else
            {
                EditorApplication.update -= AwaitGeneratedEnums;
                EditorUtility.ClearProgressBar();

                //tell the code to now use the generated files instead of the ones that are used on plugin set up
                PlayUREditorUtils.AddScriptingDefinesForAllPlatforms("PLAYUR_GENERATED");

                //create an assembly definition
                var path = GeneratedFilesPath("AssemblyReference.asmref");
                AssetDatabase.DeleteAsset(path);
                File.WriteAllBytes(path, Encoding.UTF8.GetBytes("{\n\"reference\": \"PlayUR\"\n}"));


                AssetDatabase.Refresh();

                PlayURPlugin.Log("Finished generating enums");
            }
        }
        #endregion

        #region BuildTools
        class CoroutineRunner { }
        static string GetBuildPath(BuildTarget buildTarget)
        {
            var path = EditorUtility.SaveFolderPanel("Build "+buildTarget+" to...",
                                                        Application.dataPath + "/build/",
                                                        ""); 
            if (string.IsNullOrEmpty(path)) return path;

            if (buildTarget == BuildTarget.StandaloneWindows || buildTarget == BuildTarget.StandaloneWindows64)
            { 
                //append the exe name to the path
                path += "/" + PlayerSettings.productName + ".exe";
            }
            else if (buildTarget == BuildTarget.StandaloneOSX)
            {
                //append the app name to the path
                path += "/index.app";
            }
            else if (buildTarget == BuildTarget.Android)
            {
                //append the app name to the path
                path += "/index.apk";
            }

            return path;
        }
        [MenuItem("PlayUR/Build Web Player", priority = 22)]
        public static void BuildWebPlayer()
        {
            string path = GetBuildPath(BuildTarget.WebGL); if (string.IsNullOrEmpty(path)) return;
            BuildPlayer(BuildTargetGroup.WebGL, BuildTarget.WebGL, path, onlyUpload: false, upload: false, PlayURPlatformID: 0);
        }
        [MenuItem("PlayUR/Build and Upload Web Player", priority = 23)]
        public static void BuildAndUploadWebPlayer()
        {
            string path = GetBuildPath(BuildTarget.WebGL); if (string.IsNullOrEmpty(path)) return;
            BuildPlayer(BuildTargetGroup.WebGL, BuildTarget.WebGL, path, onlyUpload: false, upload: true, PlayURPlatformID: 0);
        }
        [MenuItem("PlayUR/Upload Web Player", priority = 24)]
        public static void UploadWebPlayer()
        {
            string path = GetBuildPath(BuildTarget.WebGL); if (string.IsNullOrEmpty(path)) return;
            BuildPlayer(BuildTargetGroup.WebGL, BuildTarget.WebGL, path, onlyUpload: true, upload: true, PlayURPlatformID: 0);
        }


        [MenuItem("PlayUR/Build Windows Player", priority = 100)]
        public static void BuildWindowsPlayer()
        {
            string path = GetBuildPath(BuildTarget.StandaloneWindows64); if (string.IsNullOrEmpty(path)) return;
            BuildPlayer(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64, path, onlyUpload: false, upload: false, PlayURPlatformID: 2);
        }
        [MenuItem("PlayUR/Build and Upload Windows Player", priority = 101)]
        public static void BuildAndUploadWindowsPlayer()
        {
            string path = GetBuildPath(BuildTarget.StandaloneWindows64); if (string.IsNullOrEmpty(path)) return;
            BuildPlayer(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64, path, onlyUpload: false, upload: true, PlayURPlatformID: 2);
        }
        [MenuItem("PlayUR/Upload Windows Player", priority = 102)]
        public static void UploadWindowsPlayer()
        {
            string path = GetBuildPath(BuildTarget.StandaloneWindows64); if (string.IsNullOrEmpty(path)) return;
            BuildPlayer(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64, path, onlyUpload: true, upload: true, PlayURPlatformID: 2);
        }

        [MenuItem("PlayUR/Build MacOS Player", priority = 200)]
        public static void BuildMacOSPlayer()
        {
            string path = GetBuildPath(BuildTarget.StandaloneOSX); if (string.IsNullOrEmpty(path)) return;
            BuildPlayer(BuildTargetGroup.Standalone, BuildTarget.StandaloneOSX, path, onlyUpload: false, upload: false, PlayURPlatformID: 3);
        }
        [MenuItem("PlayUR/Build and Upload MacOS Player", priority = 201)]
        public static void BuildAndUploadMacOSPlayer()
        {
            string path = GetBuildPath(BuildTarget.StandaloneOSX); if (string.IsNullOrEmpty(path)) return;
            BuildPlayer(BuildTargetGroup.Standalone, BuildTarget.StandaloneOSX, path, onlyUpload: false, upload: true, PlayURPlatformID: 3);
        }
        [MenuItem("PlayUR/Upload MacOS Player", priority = 202)]
        public static void UploadMacOSPlayer()
        {
            string path = GetBuildPath(BuildTarget.StandaloneOSX); if (string.IsNullOrEmpty(path)) return;
            BuildPlayer(BuildTargetGroup.Standalone, BuildTarget.StandaloneOSX, path, onlyUpload: true, upload: true, PlayURPlatformID: 3);
        }


        [MenuItem("PlayUR/Build Android Player", priority = 300)]
        public static void BuildAndroidPlayer()
        {
            string path = GetBuildPath(BuildTarget.Android); if (string.IsNullOrEmpty(path)) return;
            BuildPlayer(BuildTargetGroup.Android, BuildTarget.Android, path, onlyUpload: false, upload: false, PlayURPlatformID: 4);
        }
        [MenuItem("PlayUR/Build and Upload Android Player", priority = 301)]
        public static void BuildAndUploadAndroidPlayer()
        {
            string path = GetBuildPath(BuildTarget.Android); if (string.IsNullOrEmpty(path)) return;
            BuildPlayer(BuildTargetGroup.Android, BuildTarget.Android, path, onlyUpload: false, upload: true, PlayURPlatformID: 4);
        }
        [MenuItem("PlayUR/Upload Android Player", priority = 302)]
        public static void UploadAndroidPlayer()
        {
            string path = GetBuildPath(BuildTarget.Android); if (string.IsNullOrEmpty(path)) return;
            BuildPlayer(BuildTargetGroup.Android, BuildTarget.Android, path, onlyUpload: true, upload: true, PlayURPlatformID: 4);
        }


        // this is the main player builder function
        static void BuildPlayer(BuildTargetGroup buildTargetGroup, BuildTarget buildTarget, string buildPath, bool onlyUpload = false, bool upload = true, int PlayURPlatformID = 0)
        {
            Debug.ClearDeveloperConsole();
            if (onlyUpload == false)
            {
                PlayURPlugin.Log("====== BuildPlayer: " + buildTarget.ToString() + " at " + buildPath);

                EditorUserBuildSettings.SwitchActiveBuildTarget(buildTargetGroup, buildTarget);

                //turning off build just this second
                BuildReport report = BuildPipeline.BuildPlayer(GetScenePaths(), buildPath, buildTarget, BuildOptions.None);
                BuildResult result = report.summary.result;
                if (result != BuildResult.Succeeded)
                {
                    EditorUtility.DisplayDialog("PlayUR Plugin", "Build Failed! Check the log.", "OK");
                    return;
                }
            }
            if (IsDetachedMode)
            {
                EditorUtility.DisplayDialog("PlayUR Plugin", "Build Complete. Note, in detached mode, you can just do normal builds--you don't need to use the PlayUR menu.", "OK");
                return;
            }

            // ZIP everything
            var uploadfilename = "";
            if (buildTarget == BuildTarget.StandaloneWindows || buildTarget == BuildTarget.StandaloneWindows64 || buildTarget == BuildTarget.StandaloneOSX)
            {
                //get a build path minus the exe filename IF there is a .exe in the path
                if (buildPath.EndsWith(".exe"))
                    buildPath = buildPath.Substring(0, buildPath.LastIndexOf('/'));
                if (buildPath.EndsWith(".app"))
                    buildPath = buildPath.Substring(0, buildPath.LastIndexOf('/'));
                uploadfilename = buildPath + "/index.zip";
                if (buildTarget == BuildTarget.StandaloneOSX)
                    CompressDirectoryWithCommand(buildPath + "/", uploadfilename);
                else
                    CompressDirectory(buildPath + "/", uploadfilename);
            }
            else if (buildTarget == BuildTarget.Android)
            {
                //don't zip the apk
                uploadfilename = buildPath;
            }
            else
            {
                uploadfilename = buildPath + "/index.zip";
                CompressDirectory(buildPath + "/", uploadfilename);
            }
            PlayURPlugin.Log("uploadfilename = " + uploadfilename);

            if (upload || onlyUpload)
            {
                //ask the user for the branch name
                PopupInput.Open("Build Branch Name", "Enter the branch to upload this build to.", (branch, cancelled) =>
                {
                    if (cancelled == false)
                    {
                        branch = string.IsNullOrEmpty(branch) ? "main" : branch;
                        PlayURPlugin.Log("Selected branch: " + branch);
                        PlayURPlugin.Log("uploadfilename: " + uploadfilename);

                        //upload to server
                        EditorCoroutineUtility.StartCoroutine(UploadBuild(uploadfilename, branch, (succ2, json) =>
                        {
                            if (succ2)
                            {
                                PlayURPlugin.Log("Build Uploaded!");

                                EditorUtility.DisplayDialog("PlayUR Plugin", $"Build Uploaded to Branch `{branch}`.", "OK");

                                /*if (EditorUtility.DisplayDialog("PlayUR Plugin", $"Build Uploaded to Branch `{branch}`. Do you want to open it in your browser?", "Yes", "No"))
                                {
                                    OpenGameInBrowser();
                                }*/
                            }
                            else
                            {
                                PlayURPlugin.LogError("Build Failed...");
                                PlayURPlugin.LogError(json.ToString());
                                EditorUtility.DisplayDialog("PlayUR Plugin", "Build Failed. " + json.ToString(), "OK");
                            }
                        }, PlayURPlatformID), new CoroutineRunner());
                    }

                }, defaultText: "main");//TODO: remember last time a branch was used?

            }
        }
        /*
        [MenuItem("PlayUR/Run Game In Browser", priority = 45)]
        public static void OpenGameInBrowser()
        {
            //get the latest build id, so that we can open it up in unity
            var form = GetGameIDForm();
            EditorCoroutineUtility.StartCoroutine(Rest.Get("Build/latestBuildID.php", form, (succ, result) =>
            {
                int buildID = result["latestBuildID"];
                Application.OpenURL(PlayURPlugin.SERVER_URL.Replace("/api/", "/games.php?/game/" + form["clientSecret"] + "/buildID/" + buildID));
            }, debugOutput: true), new CoroutineRunner());
        }*/

        [MenuItem("PlayUR/Re-generate Enums and Schema Definitions", isValidateFunction: true)]
        [MenuItem("PlayUR/Build Web Player", isValidateFunction: true)]
        [MenuItem("PlayUR/Build and Upload Web Player", isValidateFunction: true)]
        [MenuItem("PlayUR/Upload Web Player", isValidateFunction: true)]
        [MenuItem("PlayUR/Run Game In Browser", isValidateFunction: true)]
        public static bool ValidateGameMenuFunctions()
        {
            return PlayURPlugin.GameID > 0 || string.IsNullOrEmpty(PlayURPlugin.ClientSecret);
        }

        [MenuItem("PlayUR/Build Windows Player", isValidateFunction:true)]
        [MenuItem("PlayUR/Build and Upload Windows Player", isValidateFunction: true)]
        [MenuItem("PlayUR/Upload Windows Player", isValidateFunction: true)]
        public static bool ValidateWindowsMenuFunctions()
        {
            return Application.platform == RuntimePlatform.WindowsEditor;
        }

        [MenuItem("PlayUR/Build MacOS Player", isValidateFunction: true)]
        [MenuItem("PlayUR/Build and Upload MacOS Player", isValidateFunction: true)]
        [MenuItem("PlayUR/Upload MacOS Player", isValidateFunction: true)]
        public static bool ValidateMacOSMenuFunctions()
        {
            return Application.platform == RuntimePlatform.OSXEditor;
        }

        [MenuItem("PlayUR/Build and Upload Web Player", isValidateFunction: true)]
        [MenuItem("PlayUR/Upload Web Player", isValidateFunction: true)]
        [MenuItem("PlayUR/Build and Upload Windows Player", isValidateFunction: true)]
        [MenuItem("PlayUR/Upload Windows Player", isValidateFunction: true)]
        [MenuItem("PlayUR/Build and Upload MacOS Player", isValidateFunction: true)]
        [MenuItem("PlayUR/Upload MacOS Player", isValidateFunction: true)]
        [MenuItem("PlayUR/Build and Upload Android Player", isValidateFunction: true)]
        [MenuItem("PlayUR/Upload Android Player", isValidateFunction: true)]
        [MenuItem("PlayUR/Run Game In Browser", isValidateFunction: true)]
        [MenuItem("PlayUR/Check for Updates...", isValidateFunction: true)]
        [MenuItem("PlayUR/Re-generate Enums and Schema Definitions", isValidateFunction: true)]
        public static bool ValidateMenuFunctionsWhichDontWorkInDetachedMode()
        {
            return !IsDetachedMode;
        }

        static string[] GetScenePaths()
        {
            string[] scenes = new string[EditorBuildSettings.scenes.Length];
            for (int i = 0; i < scenes.Length; i++)
            {
                scenes[i] = EditorBuildSettings.scenes[i].path;
            }
            return scenes;
        }
        // compress the folder into a ZIP file, uses https://github.com/r2d2rigo/dotnetzip-for-unity
        static void CompressDirectory(string directory, string zipFileOutputPath)
        {
            PlayURPlugin.Log("attempting to compress " + directory + " into " + zipFileOutputPath);

            if (File.Exists(zipFileOutputPath))
                File.Delete(zipFileOutputPath);

            // display fake percentage, I can't get zip.SaveProgress event handler to work for some reason, whatever
            EditorUtility.DisplayProgressBar("COMPRESSING... please wait", zipFileOutputPath, 0.38f);
            using (ZipFile zip = new ZipFile())
            {
                zip.ParallelDeflateThreshold = -1; // DotNetZip bugfix that corrupts DLLs / binaries http://stackoverflow.com/questions/15337186/dotnetzip-badreadexception-on-extract
                zip.AddDirectory(directory);
                zip.Save(zipFileOutputPath);
            }
            EditorUtility.ClearProgressBar();
        }
        static void CompressDirectoryWithCommand(string directory, string zipFileOutputPath)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = "zip",
                UseShellExecute = false,
                //CreateNoWindow = false,
                Arguments = "-r index.zip index.app",
                RedirectStandardOutput = true,
                WorkingDirectory = directory,
            };
            Process myProcess = new Process
            {
                StartInfo = startInfo,
            };
            myProcess.OutputDataReceived += (sender, e) =>
            {
                print(e.Data);
            };
            myProcess.Start();
            myProcess.BeginOutputReadLine();
            myProcess.WaitForExit();
        }
        static IEnumerator UploadBuild(string zipPath, string branch, EditorRest.ServerCallback callback, int PlayURPlatformID = 0)
        {
            var form = GetGameIDForm();
            form.Add("branch", branch);
            form.Add("platform", PlayURPlatformID.ToString());

            yield return EditorCoroutineUtility.StartCoroutine(EditorRest.Get("Build/latestBuildID.php", form, (succ, result) =>
            {
                var newBuildID = int.Parse(result["latestBuildID"]);
                var id = result.HasKey("id") ? result["id"].AsInt : -1;
                EditorCoroutineUtility.StartCoroutine(UploadBuildPart2(zipPath, branch, callback, form["gameID"], form["clientSecret"], newBuildID, PlayURPlatformID), new CoroutineRunner());
            }, debugOutput:true), new CoroutineRunner());
        }
        static IEnumerator UploadBuildPart2(string zipPath, string branch, EditorRest.ServerCallback callback, string gameID, string clientSecret, int newBuildID, int PlayURPlatformID = 0)
        {
            PlayURPlugin.Log("New Build ID: " + newBuildID);
            PlayURPlugin.Log("Branch: " + branch);
            PlayURPlugin.Log(PlayURPlugin.SERVER_URL + "Build/");

            JSONObject jsonSend = new JSONObject();
            jsonSend["gameID"] = gameID;
            jsonSend["clientSecret"] = clientSecret;
            jsonSend["buildID"] = newBuildID;
            jsonSend["branch"] = branch;
            jsonSend["platform"] = PlayURPlatformID;
            jsonSend["current"] = true;
            PlayURPlugin.Log("JSON: " + jsonSend.ToString());

            //get the filename from the zipPath
            string fileName = Path.GetFileName(zipPath);
            //determine the mimetype from the filename
            string mimeType = "application/zip";
            if (fileName.EndsWith(".app")) mimeType = "application/octet-stream";
            else if (fileName.EndsWith(".apk")) mimeType = "application/vnd.android.package-archive";

            yield return EditorCoroutineUtility.StartCoroutine(UploadFile("Build/", zipPath, fileName, mimeType, jsonSend, callback), new CoroutineRunner());
        }
        #endregion

        #region Utils
        static bool IsDetachedMode { get { return PlayURPlugin.Settings.detachedMode; } }
        static float progress = 0;
        static UnityWebRequest www;
        static UnityWebRequestAsyncOperation operation;
        static string uploadFilename = "";
        static UploadFinishedCallback uploadFinishedCallback;
        delegate void UploadFinishedCallback();
        static void ProgressUpdate() 
        {
            progress = operation.progress;
            EditorUtility.DisplayProgressBar("UPLOADING... please wait", uploadFilename, operation.progress);
            if (operation.isDone && operation.webRequest.result != UnityWebRequest.Result.InProgress)
            {
                uploadFinishedCallback();
            }
        }
        static IEnumerator UploadFile(string endPoint, string filePath, string fileName, string mimeType, JSONObject additionalRequest = null, EditorRest.ServerCallback callback = null)
        {
            EditorUtility.ClearProgressBar();

            WWWForm form = new WWWForm();
            form.AddField("gameID", PlayURPlugin.GameID);
            form.AddField("clientSecret", PlayURPlugin.ClientSecret);
            form.AddBinaryData("file", File.ReadAllBytes(filePath), fileName, mimeType);

            //display a progress bar
            EditorUtility.DisplayProgressBar("UPLOADING... please wait", fileName, 0f);

            if (additionalRequest != null) form.AddField("request", additionalRequest.ToString());

            www = UnityWebRequest.Post(PlayURPlugin.SERVER_URL + endPoint, form);
            
            PlayURPlugin.Log("Upload Begin");

            progress = 0;
            operation = www.SendWebRequest();
            uploadFilename = filePath;
            uploadFinishedCallback = () =>
            {
                EditorApplication.update -= ProgressUpdate;

                //kill progress bar
                EditorUtility.ClearProgressBar();

                JSONNode json;
                if (www.result == UnityWebRequest.Result.ConnectionError)
                {
                    throw new ServerCommunicationException(www.error);
                }
                else if (www.result == UnityWebRequest.Result.ProtocolError)
                {
                    json = JSON.Parse(www.downloadHandler.text);
                    PlayURPlugin.LogError("Response Code: " + www.responseCode);
                    PlayURPlugin.LogError(json);

                    //if (callback != null) callback(false, json["message"]);
                    return;
                }
                else if (www.result != UnityWebRequest.Result.Success)
                {
                    PlayURPlugin.LogError(www.result.ToString());
                }
                PlayURPlugin.Log(www.downloadHandler.text);

                try
                {
                    json = JSON.Parse(www.downloadHandler.text);
                }
                catch (System.Exception e)
                {
                    throw new ServerCommunicationException("JSON Parser Error: " + e.Message);
                }


                www.Dispose();


                if (json == null)
                {
                    PlayURPlugin.Log("json == null, Response Code: " + www.responseCode);
                    if (callback != null) callback(false, "Unknown error: " + www.downloadHandler.text);

                    return;
                }
                if (json["success"] != null)
                {
                    if (json["success"].AsBool != true)
                    {
                        if (callback != null) callback(false, json["message"]);

                        return;
                    }
                }

                if (callback != null) callback(true, json);
                    
            };
            EditorApplication.update += ProgressUpdate;


            yield return null;

        }

        static Dictionary<string, string> GetGameIDForm()
        {

            //first, grab the latest build number for this game
            int gameID = PlayURPlugin.GameID;
            if (gameID == -1)
                return null;

            string clientSecret = PlayURPlugin.ClientSecret;
            if (string.IsNullOrEmpty(clientSecret))
                return null;

            PlayURPlugin.Log("Game ID: " + gameID + ", Client Secret: " + clientSecret);

            //then get the latest build id, so we can upload the next one in sequence
            Dictionary<string, string> gameIDForm = new Dictionary<string, string>();
            gameIDForm.Add("gameID", gameID.ToString());
            gameIDForm.Add("clientSecret", clientSecret);
            return gameIDForm;
        }

        [MenuItem("GameObject/PlayUR/PlayUR Plugin Helper Prefab")]
        public static void CreatePlayurHelper(UnityEditor.MenuCommand menuCommand)
        {
            //find the helper prefab using asset database
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PluginLocation + "Runtime/Assets/PlayURPluginHelper.prefab");
            //instantiate the prefab
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = "PlayURPluginHelper";
            // Ensure it gets reparented if this was a context click (otherwise does nothing)
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            Selection.activeObject = go;
        }
        #endregion

        #region Update Checking
        [MenuItem("PlayUR/Check for Updates...", priority = 500)]
        public static void CheckForUpdates()
        {
            //this is a dumb solution now
            PlayUREditorUtils.OpenPackageManager();
            //var runner = new CoroutineRunner();
            //EditorCoroutineUtility.StartCoroutine(CheckUpdateRoutine(), runner);
        }

        static IEnumerator CheckUpdateRoutine()
        {
            checkingForUpdate = true;

            currentVersion = null;
            latestVersion = null;
            //print(UpdateAvailable);

            var listResult = Client.List();
            while (listResult.IsCompleted == false) yield return 0;
            var package = listResult.Result.FirstOrDefault(c => c.name == "io.playur.unity");

            currentVersion = package.version;
            latestVersion = package.versions.latestCompatible;
            if (string.IsNullOrEmpty(latestVersion)) latestVersion = "0.2.1";//just for testing when I have a local one

            //print(currentVersion);
            //print(latestVersion);
            //print(UpdateAvailable);

            checkingForUpdate = false;

        }
        public static void GetCurrentVersion()
        {
            var runner = new CoroutineRunner();
            EditorCoroutineUtility.StartCoroutine(GetCurrentVersionRoutine(), runner);
        }
        static IEnumerator GetCurrentVersionRoutine()
        {
            currentVersion = null;

            var listResult = Client.List();
            while (listResult.IsCompleted == false) yield return 0;
            var package = listResult.Result.FirstOrDefault(c => c.name == "io.playur.unity");

            currentVersion = package.version;

        }

        public delegate void WarningsDelegate(string[] warnings);
        public static void CheckForWarnings(int gameID, WarningsDelegate callback)
        {
            if (IsDetachedMode) return; 

            var runner = new CoroutineRunner();
            var form = GetGameIDForm();
            EditorCoroutineUtility.StartCoroutine(EditorRest.Get("game/warnings.php", form, (succ, message) => {
                if (succ)
                {
                    var json = message;
                    if (json["success"].AsBool)
                    {
                        var warnings = json["warnings"].AsArray;
                        string[] warningStrings = new string[Mathf.Min(6, warnings.Count)];
                        for (int i = 0; i < Mathf.Min(5,warnings.Count); i++)
                        {
                            warningStrings[i] = warnings[i];
                        }
                        if (warnings.Count > 5)
                        {
                            warningStrings[5] = "Plus " + (warnings.Count - 5) + " More Warnings";
                        }
                        callback(warningStrings);
                    }
                    else
                    {
                        callback(null);
                    }
                }
                else
                {
                    callback(null);
                }   
            }), runner);
        }

        public static string currentVersion, latestVersion;
        public static bool checkingForUpdate = false;
        public static bool? UpdateAvailable => (string.IsNullOrEmpty(currentVersion) || string.IsNullOrEmpty(latestVersion)) ? null : PlayUREditorUtils.CompareSemanticVersions(currentVersion, latestVersion) < 0;
        #endregion

        [MenuItem("PlayUR/Clear PlayerPrefs (Local Only)", priority = 400)] 
        public static void ClearPlayerPrefs()
        {
            PlayerPrefs.Clear();
        }
    }

}
