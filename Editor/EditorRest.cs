using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine.Networking;
using UnityEngine;
using PlayUR.Exceptions;
using UnityEditor;

namespace PlayUR.Editor
{ 
    /// <summary>
    /// Interface class used for communicating with the REST API on the server.
    /// </summary>
    public partial class EditorRest
    {
        /// <summary>
        /// Generic callback delegate which is called when we get a request is completed (failed or otherwise).
        /// </summary>
        /// <param name="succ">Boolean result of whether our request was successful (including logical errors, not just server connectivity issues)</param>
        /// <param name="result">The JSON decoded data from the server for our request.</param>
        public delegate void ServerCallback(bool succ, JSONNode result = null);

        /// <summary>
        /// Generic callback delegate which is called when we get a request fpr a file is completed (failed or otherwise).
        /// </summary>
        /// <param name="succ">Boolean result of whether our request was successful (including logical errors, not just server connectivity issues)</param>
        /// <param name="result">The bytes representing the data returned.</param>
        public delegate void ServerFileCallback(bool succ, byte[] result);

        static Dictionary<int, UnityWebRequest> www = new Dictionary<int, UnityWebRequest>();
        static Dictionary<int, UnityWebRequestAsyncOperation> operation = new Dictionary<int, UnityWebRequestAsyncOperation>();
        static int nextRequestKey = 0;
        static Dictionary<int, GetOperationFinished> getRequestFinishedCallback = new Dictionary<int, GetOperationFinished>();
        delegate void GetOperationFinished();
        static void Await(int key)
        {
            if (operation[key].isDone && operation[key].webRequest.result != UnityWebRequest.Result.InProgress)
            {
                getRequestFinishedCallback[key]();
                getRequestFinishedCallback.Remove(key);
                operation.Remove(key);
                www.Remove(key);
            }
        }

        /// <summary>
        /// Standard HTTP GET request. 
        /// Used for requesting information FROM the server.
        /// Has a callback for reading the response.
        /// </summary>
        /// <param name="page">The endpoint we are requesting (relative to <see cref="PlayURPlugin.SERVER_URL"/>/api/</param>
        /// <param name="form">Dictionary of key value pairs of information we want to send to the server.</param>
        /// <param name="callback">Callback for handling response from the server.</param>
        /// <param name="debugOutput">Optionally debug to the Unity console a bunch of information about how the request occurred. 
        /// Use only when things are failing and we need to know what the server is directly saying.</param>
        /// <exception cref="ServerCommunicationException">thrown when the server is unreachable.</exception>
        public static IEnumerator Get(string page, Dictionary<string, string> form, ServerCallback callback, bool debugOutput = false)
        {
            //var www = new WWW(SERVER_URL + page+".php", form.data);
            var kvp = "?";
            if (form != null)
            {
                foreach (var k in form)
                {
                    kvp += k.Key + "=" + k.Value + "&";
                }
            }

            var url = PlayURPlugin.SERVER_URL + page + "/" + kvp;
            if (page.IndexOf(".php") > 0)
                url = PlayURPlugin.SERVER_URL + page + kvp;
            if (debugOutput) PlayURPlugin.Log("GET " + url);

            int key = nextRequestKey++;
            www[key] = UnityWebRequest.Get(url);
            operation[key] = www[key].SendWebRequest();

            www[key] = UnityWebRequest.Get(url);
            //operation[key].completed += () => Debug.Log(www[key].)
            EditorApplication.CallbackFunction AwaitKey = () => Await(key);
            getRequestFinishedCallback[key] = () =>
            {
                EditorApplication.update -= AwaitKey;
                JSONNode json;

                if (operation[key].webRequest.result == UnityWebRequest.Result.ConnectionError)
                {
                    throw new ServerCommunicationException(operation[key].webRequest.error);
                }
                else if (operation[key].webRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    json = JSON.Parse(operation[key].webRequest.downloadHandler.text);
                    PlayURPlugin.LogError("Response Code: " + operation[key].webRequest.responseCode);
                    PlayURPlugin.LogError(json);

                    //if (callback != null) callback(false, json["message"]);
                    return;
                }
                else if (operation[key].webRequest.result != UnityWebRequest.Result.Success)
                {
                    PlayURPlugin.LogError(operation[key].webRequest.result.ToString());
                }

                try
                {
                    json = JSON.Parse(operation[key].webRequest.downloadHandler.text);
                }
                catch (System.Exception e)
                {
                    throw new ServerCommunicationException("JSON Parser Error: " + e.Message + "\nRaw: '" + operation[key].webRequest.downloadHandler.text + "'");
                }

                if (debugOutput) PlayURPlugin.Log(operation[key].webRequest.downloadHandler.text);

                if (json != null && json["success"])
                {
                    if (json["success"].AsBool != true)
                    {
                        callback(false, json);
                        return;
                    }
                }

                callback(json != null, json);

            };
            EditorApplication.update += AwaitKey;

            yield return null;
        }
    }
}
