using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using UnityEditor.Build.Reporting;
using Ionic.Zip; // this uses the Unity port of DotNetZip https://github.com/r2d2rigo/dotnetzip-for-unity
using EditorCoroutinesPlugin;
using System.IO;
using UnityEditor.SceneManagement;
using System.Text;
using PlayUR.Core;
using PlayUR.Exceptions;

namespace PlayUR.Editor
{
    public class PlayURPluginEditor : MonoBehaviour
    {
        #region Initial Set Up
        static string path;

        [MenuItem("PlayUR/Set Up Plugin")]
        public static void ReSetUpPlugin()
        {
            SetUpPlugin(reset:true);
        }
        public static void SetUpPlugin(bool reset = false)
        {
            Debug.ClearDeveloperConsole();

            if (reset)
            {
                //if chosing this option, we should clear the saved options
                var p = Path.Combine(Application.dataPath, "gameID.txt");
                File.Delete(p);
                p = Path.Combine(Application.dataPath, "clientSecret.txt");
                File.Delete(p);
            }

            SetSceneBuildSettings();
            if (SetGameIDInPlayURLoginScene())
            {
                GenerateEnum();

                EditorUtility.DisplayDialog("PlayUR Plugin Setup", "Plugin Set Up Complete.", "OK");
                SetExecutionOrder();
                PlayURPlugin.Log("Set up complete."); 
            }
        }
        static void SetPathIfNecessary()
        {
            if (path == null)
            {
                path = "Assets/PlayURPlugin/PlayURPlugin.cs";
            }
            MonoScript playURScript;
            do
            {
                playURScript = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (playURScript == null)
                {
                    path = EditorUtility.OpenFilePanelWithFilters("PlayUR Plugin Setup - Locate PlayURPlugin.cs", path, new string[] { "PlayURPlugin", "cs" });
                    path = path.Substring(path.LastIndexOf("Assets/"));

                    if (string.IsNullOrEmpty(path))
                    {
                        return;
                    }
                }
            }
            while (playURScript == null);
        }

        //Set PlayURPlugin to run first
        static void SetExecutionOrder()
        {
            SetPathIfNecessary();
            MonoScript playURScript = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
            MonoImporter.SetExecutionOrder(playURScript, -10000);

            playURScript = AssetDatabase.LoadAssetAtPath<MonoScript>(path.Replace("PlayURPlugin.cs", "Core/PlayURPluginHelper.cs"));
            MonoImporter.SetExecutionOrder(playURScript, -9999);

            PlayURPlugin.Log("Set execution order of PlayUR Plugin to -10000");
        }

        //add PlayURLogin to Build Settings
        static void SetSceneBuildSettings()
        {
            SetPathIfNecessary();
            var scenePath = path.Replace("PlayURPlugin.cs", "Assets/PlayURLogin.unity");
            var scenes = new List<EditorBuildSettingsScene>();
            scenes.AddRange(EditorBuildSettings.scenes);

            //check it doesn't already exist
            for (var i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].path.Equals(scenePath))
                {
                    return;
                }
            }
            scenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            PlayURPlugin.Log("Added 'PlayURLogin' to Build Settings");

        }
        static bool SetGameIDInPlayURLoginScene()
        {
            var previousScenePath = EditorSceneManager.GetActiveScene().path;
            var scenePath = path.Replace("PlayURPlugin.cs", "Assets/PlayURLogin.unity");
            EditorSceneManager.OpenScene(scenePath);

            var PlayURPluginObject = FindObjectOfType<PlayURPlugin>();
            if (PlayURPluginObject)
            {
                var gameID = GetGameIDFromFile();
                var clientSecret = GetClientSecretFromFile();
                if (gameID != -1 && !string.IsNullOrEmpty(clientSecret))
                {
                    PlayURPluginObject.gameID = gameID;
                    PlayURPluginObject.clientSecret = clientSecret;
                    EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                    PlayURPlugin.Log("Set GameID in PlayURLogin scene to " + gameID);

                    EditorSceneManager.OpenScene(previousScenePath);
                    return true;
                }
                else
                {
                    //need to set a game id via a little popup
                    PopupInput.Open("PlayUR Platform - Game ID", "Please enter the game id issued by the back-end website", (input, cancelled) =>
                    {
                        int gid = -1;
                        int.TryParse(input, out gid);
                        if (cancelled == false && gid != -1)
                        {
                            SetGameIDInFile(gid);
                            PopupInput.Open("PlayUR Platform - Client Secret", "Please enter the client secret issued by the back-end website", (input2, cancelled2) =>
                            {
                                if (cancelled2 == false && !string.IsNullOrEmpty(input2))
                                {
                                    SetClientSecretInFile(input2);
                                    SetUpPlugin();
                                }
                                else
                                {
                                    PlayURPlugin.LogError("unexpected input2: " + input2);
                                }
                            });
                        }
                        else
                        {
                            PlayURPlugin.LogError("unexpected input: " + input);
                        }
                    });
                }
            }
            else
            {
                PlayURPlugin.LogError("Could not find PlayURPlugin object in PlayURLoginScene. Contact the developer as this error shouldn't occur!");
            }
            return false;
        }

        [MenuItem("PlayUR/Re-generate Enums")]
        public static void GenerateEnum()
        {
            SetPathIfNecessary();

            var GET = "?gameID=" + GetGameIDFromScene()+"&clientSecret=" + GetClientSecretFromScene();
            //get actions from the server and populate an enum
            EditorCoroutines.StartCoroutine(Rest.Get("Action/listForGame.php"+GET, null, (succ, json) =>
              {
                  if (succ)
                  {
                      var actions = json["records"].AsArray;
                      string text = "namespace PlayUR\n{\n\t///<summary>Enum generated from server representing possible user actions. To update use PlayUR\\Re-generate Enums.</summary>\n\tpublic enum Action\n\t{\n";
                      foreach (var action in actions.Values)
                      {
                          text += "\t\t" + PlatformNameToValidEnumValue(action["name"].Value) + " = " + action["id"] + ",\n";
                      }
                      text += "\t}\n}\n";

                      //write it out!
                      File.WriteAllBytes(path.Replace("PlayURPlugin.cs", "Action.cs"), Encoding.UTF8.GetBytes(text));
                      AssetDatabase.Refresh();

                      PlayURPlugin.Log("Generated Actions Enum (" + actions.Count + " actions)");
                  }
              }), new CoroutineRunner());

            //get elements from the server and populate an enum
            EditorCoroutines.StartCoroutine(Rest.Get("Element/listForGame.php"+GET, null, (succ, json) =>
            {
                if (succ)
                {
                    var elements = json["records"].AsArray;
                    Debug.Log(elements);
                    string text = "namespace PlayUR\n{\n\t///<summary>Enum generated from server representing top-level game elements. To update use PlayUR\\Re-generate Enums.</summary>\n\tpublic enum Element\n\t{\n";
                    foreach (var element in elements.Values)
                    {
                        text += "\t\t" + PlatformNameToValidEnumValue(element["name"].Value) + " = " + element["id"] + ",\n";
                    }
                    text += "\t}\n}\n";

                    //write it out!
                    File.WriteAllBytes(path.Replace("PlayURPlugin.cs", "Element.cs"), Encoding.UTF8.GetBytes(text));
                    AssetDatabase.Refresh();

                    PlayURPlugin.Log("Generated Elements Enum (" + elements.Count + " actions)");
                }
            }), new CoroutineRunner());

            //get experiments from the server and populate an enum
            EditorCoroutines.StartCoroutine(Rest.Get("Experiment/listForGame.php"+GET, null, (succ, json) =>
            {
                if (succ)
                {
                    var experiments = json["records"].AsArray;
                    Debug.Log(experiments);
                    string text = "namespace PlayUR\n{\n\t///<summary>Enum generated from server representing experiments for this game. To update use PlayUR\\Re-generate Enums.</summary>\n\tpublic enum Experiment\n\t{\n";
                    foreach (var experiment in experiments.Values)
                    {
                        text += "\t\t" + PlatformNameToValidEnumValue(experiment["name"].Value) + " = " + experiment["id"] + ",\n";
                    }
                    text += "\t}\n}\n";

                    //write it out!
                    File.WriteAllBytes(path.Replace("PlayURPlugin.cs", "Experiment.cs"), Encoding.UTF8.GetBytes(text));
                    AssetDatabase.Refresh();

                    PlayURPlugin.Log("Generated Experiments Enum (" + experiments.Count + " experiments)");
                }
            }), new CoroutineRunner());

            //get experiment groups from the server and populate an enum
            EditorCoroutines.StartCoroutine(Rest.Get("ExperimentGroup/listForGame.php"+GET, null, (succ, json) =>
            {
                if (succ)
                {
                    var experiments = json["records"].AsArray;
                    Debug.Log(experiments);
                    string text = "namespace PlayUR\n{\n\t///<summary>Enum generated from server representing experiment groups for this game. To update use PlayUR\\Re-generate Enums.</summary>\n\tpublic enum ExperimentGroup\n\t{\n";
                    foreach (var experiment in experiments.Values)
                    {
                        text += "\t\t" + PlatformNameToValidEnumValue(experiment["experiment"].Value) +"_"+ experiment["name"].Value.Replace(" ", "") + " = " + experiment["id"] + ",\n";
                    }
                    text += "\t}\n}\n";

                    //write it out!
                    File.WriteAllBytes(path.Replace("PlayURPlugin.cs", "ExperimentGroup.cs"), Encoding.UTF8.GetBytes(text));
                    AssetDatabase.Refresh();

                    PlayURPlugin.Log("Generated Experiment Groups Enum (" + experiments.Count + " groups)");
                }
            }), new CoroutineRunner());

            //get analytics columns from the server and populate an enum
            EditorCoroutines.StartCoroutine(Rest.Get("AnalyticsColumn/listForGame.php"+GET, null, (succ, json) =>
            {
                if (succ)
                {
                    var columns = json["records"].AsArray;
                    Debug.Log(columns);
                    string text = "namespace PlayUR\n{\n\t///<summary>Enum generated from server representing the extra analytics columns used for this game. To update use PlayUR\\Re-generate Enums.</summary>\n\tpublic enum AnalyticsColumn\n\t{\n";
                    foreach (var column in columns.Values)
                    {
                        text += "\t\t" + PlatformNameToValidEnumValue(column["name"].Value) + " = " + column["id"] + ",\n";
                    }
                    text += "\t}\n}\n";

                    //write it out!
                    File.WriteAllBytes(path.Replace("PlayURPlugin.cs", "AnalyticsColumns.cs"), Encoding.UTF8.GetBytes(text));
                    AssetDatabase.Refresh();

                    PlayURPlugin.Log("Generated Analytics Columns Enum (" + columns.Count + " columns)");
                }
            }), new CoroutineRunner());

            //get all parameter keys from the server and populate an enum
            EditorCoroutines.StartCoroutine(Rest.Get("GameParameter/listParameterKeys.php" + GET, null, (succ, json) =>
            {
                if (succ)
                {
                    var parameters = json["records"].AsArray;
                    Debug.Log(parameters);
                    string text = "namespace PlayUR\n{\n\t///<summary>Constant Strings generated from server representing the parameter keys for this game. To update use PlayUR\\Re-generate Enums.</summary>\n\tpublic static class Parameter\n\t{\n";
                    foreach (var parameter in parameters.Values)
                    {
                        text += "\t\tpublic static string " + PlatformNameToValidEnumValue(parameter.ToString().Replace("[]",  "")) + " = \"" + PlatformNameToValidEnumValue(parameter.ToString().Replace("[]", "")) + "\";\n";
                    }
                    text += "\t}\n}\n";

                    //write it out!
                    File.WriteAllBytes(path.Replace("PlayURPlugin.cs", "Parameter.cs"), Encoding.UTF8.GetBytes(text));
                    AssetDatabase.Refresh();

                    PlayURPlugin.Log("Generated Parameters Constants (" + parameters.Count + " parameters)");
                }
            }), new CoroutineRunner());
        }
        static string PlatformNameToValidEnumValue(string input)
        {
            var rgx = new System.Text.RegularExpressions.Regex("[^a-zA-Z0-9_]"); 
            input = rgx.Replace(input, "");
            input = input.Replace(" ", "");
            if (!char.IsLetter(input[0])) input = "_"+input;
            return input;      
        }
        #endregion

        #region BuildTools
        class CoroutineRunner { }
        static string GetBuildPath()
        {
            return EditorUtility.SaveFolderPanel("Build out WebGL to...",
                                                        Application.dataPath + "/build/",
                                                        "");
        }
        [MenuItem("PlayUR/Build Web Player")]
        public static void BuildWebPlayer()
        {
            string path = GetBuildPath(); if (string.IsNullOrEmpty(path)) return;
            BuildPlayer(BuildTargetGroup.WebGL, BuildTarget.WebGL, path, onlyUpload: false, upload: false);
        }
        [MenuItem("PlayUR/Build and Upload Web Player")]
        public static void BuildAndUploadWebPlayer()
        {
            string path = GetBuildPath(); if (string.IsNullOrEmpty(path)) return;
            BuildPlayer(BuildTargetGroup.WebGL, BuildTarget.WebGL, path, onlyUpload: false, upload: true);
        }
        [MenuItem("PlayUR/Upload Web Player")]
        public static void UploadWebPlayer()
        {
            string path = GetBuildPath(); if (string.IsNullOrEmpty(path)) return;
            BuildPlayer(BuildTargetGroup.WebGL, BuildTarget.WebGL, path, onlyUpload: true, upload: true);
        }


        // this is the main player builder function
        static void BuildPlayer(BuildTargetGroup buildTargetGroup, BuildTarget buildTarget, string buildPath, bool onlyUpload = false, bool upload = true)
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
            // ZIP everything
            CompressDirectory(buildPath+"/", buildPath + "/index.zip");

            if (upload || onlyUpload)
            {
                //ask the user for the branch name
                PopupInput.Open("Build Branch Name", "Enter the branch to upload this build to.", (branch, cancelled) =>
                {
                    if (cancelled == false)
                    {
                        branch = string.IsNullOrEmpty(branch) ? "main" : branch;
                        PlayURPlugin.Log("Selected branch: " + branch);

                        //upload to server
                        EditorCoroutines.StartCoroutine(UploadBuild(buildPath + "/index.zip", branch, (succ2, json) =>
                        {
                            if (succ2)
                            {
                                PlayURPlugin.Log("Build Uploaded!");

                                if (EditorUtility.DisplayDialog("PlayUR Plugin", $"Build Uploaded to Branch `{branch}`. Do you want to open it in your browser?", "Yes", "No"))
                                {
                                    OpenGameInBrowser();
                                }
                            }
                            else
                            {
                                PlayURPlugin.LogError("Build Failed...");
                                PlayURPlugin.LogError(json.ToString());
                                EditorUtility.DisplayDialog("PlayUR Plugin", "Build Failed. " + json.ToString(), "OK");
                            }
                        }), new CoroutineRunner());
                    }

                }, defaultText: "main");//TODO: remember last time a branch was used?
                
            }
        }
        [MenuItem("PlayUR/Run Game In Broswer")]
        public static void OpenGameInBrowser()
        {
            //get the latest build id, so that we can open it up in unity
            var form = GetGameIDForm();
            EditorCoroutines.StartCoroutine(Rest.Get("Build/latestBuildID.php", form, (succ, result) =>
            {
                int buildID = result["latestBuildID"];
                                       Application.OpenURL(PlayURPlugin.SERVER_URL.Replace("/api/", "/games.php?/game/" + form["clientSecret"] + "/buildID/" + buildID));
           }, debugOutput: true), new CoroutineRunner());
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
        static IEnumerator UploadBuild(string zipPath, string branch, Rest.ServerCallback callback)
        {
            var form = GetGameIDForm(); 
            form.Add("branch", branch);

            yield return EditorCoroutines.StartCoroutine(Rest.Get("Build/latestBuildID.php", form, (succ, result) =>
            {
                var newBuildID = int.Parse(result["latestBuildID"]) + 1;
                EditorCoroutines.StartCoroutine(UploadBuildPart2(zipPath, branch, callback, form["gameID"], form["clientSecret"], newBuildID), new CoroutineRunner());
            }), new CoroutineRunner());
        }
        static IEnumerator UploadBuildPart2(string zipPath, string branch, Rest.ServerCallback callback, string gameID, string clientSecret, int newBuildID)
        {
            PlayURPlugin.Log("New Build ID: " + newBuildID);
            PlayURPlugin.Log("Branch: " + branch);
            PlayURPlugin.Log(PlayURPlugin.SERVER_URL + "Build/");

            JSONObject jsonSend = new JSONObject();
            jsonSend["gameID"] = gameID;
            jsonSend["clientSecret"] = clientSecret;
            jsonSend["buildID"] = newBuildID;
            jsonSend["branch"] = branch;
            PlayURPlugin.Log("JSON: " + jsonSend.ToString());

            yield return EditorCoroutines.StartCoroutine(UploadFile("Build/", zipPath, "index.zip", "application/zip", jsonSend, callback), new CoroutineRunner());
        }

        #endregion

        #region Auto-Update
        [MenuItem("PlayUR/Check for Update...")]
        public static void CheckForUpdate()
        {
            EditorCoroutines.StartCoroutine(CheckForUpdate(true), new CoroutineRunner());
        }
        static IEnumerator CheckForUpdate(bool showMessageEvenIfNoUpdate)
        {
            var form = new Dictionary<string, string>();
            form.Add("currentVersion", CurrentVersion().ToString());//lol @ toString()
            yield return EditorCoroutines.StartCoroutine(Rest.Get("PluginVersion/check.php", form, (succ, result) =>
            {
                if (succ && result["updateExists"].AsBool == true)
                {
                    if (EditorUtility.DisplayDialog("PlayUR Plugin", "An updated version exists. Do you want to download it?", "Yes", "No"))
                    {
                        EditorCoroutines.StartCoroutine(DownloadAndImportPlugin((succ2, result2) =>
                        {
                            SetGameIDInPlayURLoginScene();
                            EditorUtility.DisplayDialog("PlayUR Plugin", "Downloaded.", "OK");
                        }), new CoroutineRunner());
                    }
                }
                else if (showMessageEvenIfNoUpdate)
                {
                    EditorUtility.DisplayDialog("PlayUR Plugin", "This is the latest version of the plugin.", "OK");
                }
            }, debugOutput: false), new CoroutineRunner());

        }
        static int CurrentVersion()
        {
            SetPathIfNecessary();
            var versionFilePath = path.Replace("PlayURPlugin.cs", "version.txt");
            var versionFileContents = File.ReadAllText(versionFilePath);
            var version = -100;
            int.TryParse(versionFileContents, out version);
            return version;
        }
        static void SetCurrentVersion(int v)
        {
            SetPathIfNecessary();
            var versionFilePath = path.Replace("PlayURPlugin.cs", "version.txt");
            File.WriteAllText(versionFilePath, v.ToString());
        }
        static IEnumerator DownloadAndImportPlugin(Rest.ServerCallback callback)
        {
            int newVersion = -100;
            //get the latest version number first (we need it later)
            yield return EditorCoroutines.StartCoroutine(Rest.Get("PluginVersion/getLatestVersionID.php", null, (succ, result) =>
            {
                if (succ)
                {
                    newVersion = result["latestBuild"].AsInt;
                }
            }), new CoroutineRunner());

            //now download the actual plugin 
            yield return EditorCoroutines.StartCoroutine(Rest.GetFile("PluginVersion/latestVersion.php", new Dictionary<string, string>(), (succ, result) =>
            {
                PlayURPlugin.Log(result.Length + " bytes downloaded...");

                //put the data into a temp file
                var tmpPath = Path.Combine(Application.temporaryCachePath, "plugin.unitypackage");
                File.WriteAllBytes(tmpPath, result);

                //actually do the import
                AssetDatabase.ImportPackage(tmpPath, interactive: true);

                //because the enums will get overwritten, we have to do an ugly thing here, 
                //which is wait until the package is imported with the brand new version number, 
                //and then once the number is up to date, we know the package has been imported
                //so then we can generate the enums
                EditorCoroutines.StartCoroutine(WaitForPackageImportThenUpdateEnums(newVersion, callback), new CoroutineRunner());

            }), new CoroutineRunner());
        }
        static IEnumerator WaitForPackageImportThenUpdateEnums(int versionNumberToLookFor, Rest.ServerCallback callback)
        {
            int version = -100;
            do
            {
                version = CurrentVersion();
                yield return new WaitForSeconds(1);
                //PlayURPlugin.Log("waiting, current version = " + version);
            }
            while (version != versionNumberToLookFor);

            if(callback != null)
                callback(true, null);
            GenerateEnum();
        }

        [MenuItem("PlayUR/Admin/Upload plugin code (requires password)")]
        public static void UploadPluginCodeToServer()
        {
            //require a password first
            PopupInput.Open("PlayUR Plugin", "Enter the secret password to upload plugin code...", (input, cancelled) =>
            {
                if (cancelled || string.IsNullOrEmpty(input)) return;

                SetPathIfNecessary();
                //first get package output path (we could skip this, but this is kinda nice
                var exportPath = EditorUtility.SaveFilePanel("Build out unitypackage to...",
                                                                Application.dataPath + "../../",
                                                                "PlayUR_UnityPlugin", "unitypackage");
                if (string.IsNullOrEmpty(exportPath)) return;

                //first work out what the latest version number is to use
                EditorCoroutines.StartCoroutine(Rest.Get("PluginVersion/getLatestVersionID.php", null, (succ, result) =>
                {
                    if (succ)
                    {
                        var latestBuild = result["latestBuild"].AsInt;

                        //set the version file text
                        SetCurrentVersion(latestBuild + 1);

                        //ask for input for what the changelog is, and password
                        AssetDatabase.ExportPackage(path.Replace("/PlayURPlugin.cs", ""), exportPath, ExportPackageOptions.Recurse);

                        PopupInput.Open("PlayUR Plugin", "Any changelog messages?", (changeLog, cancelled2) =>
                        {
                            if (cancelled2) return;

                            //then upload that file
                            JSONObject jsonSend = new JSONObject();
                            jsonSend["changeLog"] = changeLog;

                            //pop the password in to upload
                            jsonSend["pass"] = input;

                            EditorCoroutines.StartCoroutine(UploadFile("PluginVersion/", exportPath, "PlayUR_UnityPlugin", "application/unitypackage", jsonSend, (succ2, result2) =>
                            {
                                if (succ2)
                                    EditorUtility.DisplayDialog("PlayUR Plugin", "Code Uploaded! Current Version = " + result2["id"], "OK");// (Version number is now:"+result2['currentVersion']+")");
                                else
                                    EditorUtility.DisplayDialog("PlayUR Plugin", result2, "OK");
                            }), new CoroutineRunner());
                        }, "Upload Build", "Change Log");
                    }

                }, debugOutput: false), new CoroutineRunner());
            }, "Upload Build");
        }
        #endregion

        #region Game ID and Client Secret Getters
        static int GetGameIDFromFile()
        {
            var p = Path.Combine(Application.dataPath, "gameID.txt");
            if (File.Exists(p) == false)
                return -1;

            int gameID = -1;
            if (int.TryParse(File.ReadAllText(p), out gameID) == false)
                PlayURPlugin.LogError("Set a GameID in the gameID.txt file!");

            return gameID;
        }
        static void SetGameIDInFile(int id)
        {
            var p = Path.Combine(Application.dataPath, "gameID.txt");
            if (File.Exists(p) == false)
            {
                var f = File.Open(p, FileMode.OpenOrCreate);
                f.Close(); //lol
            }
            File.WriteAllText(p, id.ToString());
        }
        static string GetClientSecretFromFile()
        {
            var p = Path.Combine(Application.dataPath, "clientSecret.txt");
            if (File.Exists(p) == false)
                return null;

            var s = File.ReadAllText(p);
            if (string.IsNullOrEmpty(s))
                PlayURPlugin.LogError("Set a Client Secret in the clientSecret.txt file!");

            return s;
        }
        static void SetClientSecretInFile(string clientSecret)
        {
            var p = Path.Combine(Application.dataPath, "clientSecret.txt");
            if (File.Exists(p) == false)
            {
                var f = File.Open(p, FileMode.OpenOrCreate);
                f.Close(); //lol
            }
            File.WriteAllText(p, clientSecret);
        }
        static int GetGameIDFromScene()
        {
            string previousSceneID = EditorSceneManager.GetActiveScene().path;
            if (FindObjectOfType<PlayURPlugin>() == null && EditorBuildSettings.scenes.Length > 0)
                EditorSceneManager.OpenScene(EditorBuildSettings.scenes[0].path);
            if (FindObjectOfType<PlayURPlugin>() == null)
            {
                PlayURPlugin.LogError("Could not find PlayURPlugin object. Have you set up the plugin?");
                EditorSceneManager.OpenScene(previousSceneID);
                return -1;
            }
            var gameID = FindObjectOfType<PlayURPlugin>().gameID;
            EditorSceneManager.OpenScene(previousSceneID);
            return gameID;
        }
        static string GetClientSecretFromScene()
        {
            string previousSceneID = EditorSceneManager.GetActiveScene().path;
            if (FindObjectOfType<PlayURPlugin>() == null && EditorBuildSettings.scenes.Length > 0)
                EditorSceneManager.OpenScene(EditorBuildSettings.scenes[0].path);
            if (FindObjectOfType<PlayURPlugin>() == null)
            {
                PlayURPlugin.LogError("Could not find PlayURPlugin object. Have you set up the plugin?");
                EditorSceneManager.OpenScene(previousSceneID);
                return null;
            }
            var clientSecret = FindObjectOfType<PlayURPlugin>().clientSecret;
            EditorSceneManager.OpenScene(previousSceneID);
            return clientSecret;
        }
        #endregion

        #region utils
        static IEnumerator UploadFile(string endPoint, string filePath, string fileName, string mimeType, JSONObject additionalRequest = null, Rest.ServerCallback callback = null)
        {
            WWWForm form = new WWWForm();
            form.AddField("gameID", GetGameIDFromFile().ToString());
            form.AddField("clientSecret", GetClientSecretFromFile());
            form.AddBinaryData("file", File.ReadAllBytes(filePath), fileName, mimeType);
            
            if (additionalRequest != null) form.AddField("request", additionalRequest.ToString());

            using (var www = UnityWebRequest.Post(PlayURPlugin.SERVER_URL + endPoint, form))
            {
                yield return www.SendWebRequest();

                JSONNode json;
                if (www.isNetworkError)
                {
                    throw new ServerCommunicationException(www.error);
                }
                else if (www.isHttpError)
                {
                    json = JSON.Parse(www.downloadHandler.text);
                    PlayURPlugin.LogError("Response Code: " + www.responseCode);
                    PlayURPlugin.LogError(json);
                    //if (callback != null) callback(false, json["message"]);
                    //yield break;
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


                if (json == null)
                {
                    PlayURPlugin.Log("json == null, Response Code: " + www.responseCode);
                    if (callback != null) callback(false, "Unknown error: " + www.downloadHandler.text);
                    yield break;
                }
                if (json["success"] != null)
                {
                    if (json["success"].AsBool != true)
                    {
                        if (callback != null) callback(false, json["message"]);
                        yield break;
                    }
                }

                if (callback != null) callback(true, json);

            }
        }

        static Dictionary<string, string> GetGameIDForm()
        {
            
            //first, grab the latest build number for this game
            int gameID = GetGameIDFromScene();
            if (gameID == -1)
                return null;

            string clientSecret = GetClientSecretFromScene();
            if (string.IsNullOrEmpty(clientSecret))
                return null;

            PlayURPlugin.Log("Game ID: " + gameID + ", Client Secret: "+clientSecret);

            //then get the latest build id, so we can upload the next one in sequence
            Dictionary<string, string> gameIDForm = new Dictionary<string, string>();
            gameIDForm.Add("gameID", gameID.ToString());
            gameIDForm.Add("clientSecret", clientSecret);
            return gameIDForm;
        }
        #endregion
        
        [MenuItem("PlayUR/Clear PlayerPrefs (Local Only)")] //TODO: clear from server for this user too?
        public static void ClearPlayerPrefs()
        {
            PlayerPrefs.Clear();
        }
    }

}
