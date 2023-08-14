using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine.Networking;
using UnityEngine;
using PlayUR.Exceptions;

namespace PlayUR.Core
{ 
    /// <summary>
    /// Interface class used for communicating with the REST API on the server.
    /// </summary>
    public partial class Rest
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
                foreach (var key in form)
                {
                    kvp += key.Key + "=" + key.Value + "&";
                }
            }

            var url = PlayURPlugin.SERVER_URL + page + "/" + kvp;
            if (page.IndexOf(".php") > 0)
                url = PlayURPlugin.SERVER_URL + page + kvp;
            if (debugOutput) PlayURPlugin.Log("GET " + url);

            using (var www = UnityWebRequest.Get(url))
            {
                yield return www.SendWebRequest();

                JSONNode json;

                if (www.isNetworkError)
                {
                    PlayURPlugin.Log("Response Code: " + www.responseCode);
                    throw new ServerCommunicationException(www.error);
                }
                else if (www.isHttpError)
                {
                    json = JSON.Parse(www.downloadHandler.text);
                    PlayURPlugin.Log("Response Code: " + www.responseCode);
                    if (debugOutput) PlayURPlugin.Log(json);
                    if (callback != null) callback(false, json);
                    yield break;
                }

                try
                {
                    json = JSON.Parse(www.downloadHandler.text);
                }
                catch (System.Exception e)
                {
                    throw new ServerCommunicationException("JSON Parser Error: " + e.Message+"\nRaw: '"+www.downloadHandler.text + "'");
                }

                if (debugOutput) PlayURPlugin.Log(www.downloadHandler.text);

                if (json["success"])
                {
                    if (json["success"].AsBool != true)
                    {
                        callback(false, json);
                        yield break;
                    }
                }

                callback(true, json);

            }
        }

        /// <summary>
        /// Standard HTTP POST request. 
        /// Used for sending NEW data TO the server.
        /// Has a callback for reading the response.
        /// </summary>
        /// <param name="page">The endpoint we are requesting (relative to <see cref="PlayURPlugin.SERVER_URL"/>/api/</param>
        /// <param name="form">Dictionary of key value pairs of information representing the object we want to send to the server.</param>
        /// <param name="callback">Callback for handling response from the server.</param>
        /// <param name="HTMLencode">Optionally convert form items special characters using <code>WebUtility.HtmlEncode</code>. </param>
        /// <param name="debugOutput">Optionally debug to the Unity console a bunch of information about how the request occurred. </param>
        /// Use only when things are failing and we need to know what the server is directly saying.</param>
        /// <exception cref="ServerCommunicationException">thrown when the server is unreachable.</exception>
        public static IEnumerator Post(string page, Dictionary<string, string> form, ServerCallback callback = null, bool HTMLencode = false, bool debugOutput = false)
        {
            var jsonOut = new JSONObject();
            foreach (var kvp in form)
            {
                if (HTMLencode)
                    jsonOut[kvp.Key] = WebUtility.HtmlEncode(kvp.Value); 
                else
                    jsonOut[kvp.Key] = kvp.Value;
            }

            var url = PlayURPlugin.SERVER_URL + page + "/";//the slash on the end is actually important....
            if (debugOutput) PlayURPlugin.Log("POST " +url);
            //if (debugOutput) PlayURPlugin.Log("jsonOut " +jsonOut.ToString());
            //var f = new WWWForm();
            //f.AddField("request", jsonOut.ToString());
            using (var www = UnityWebRequest.Put(url, System.Text.Encoding.UTF8.GetBytes(jsonOut.ToString())))
            {
                www.method = "post";
                yield return www.SendWebRequest();

                JSONNode json;

                if (www.isNetworkError)
                {
                    PlayURPlugin.Log("Response Code: " + www.responseCode);
                    throw new ServerCommunicationException(www.error);
                }
                else if (www.isHttpError)
                {
                    PlayURPlugin.Log(www.downloadHandler.text);
                    json = JSON.Parse(www.downloadHandler.text);
                    PlayURPlugin.Log("Response Code: " + www.responseCode);
                    if (debugOutput) PlayURPlugin.Log(json);
                    if (callback != null) callback(false, null);
                    yield break;
                }

                try
                {
                    json = JSON.Parse(www.downloadHandler.text);
                }
                catch (System.Exception e)
                {
                    PlayURPlugin.Log(www.downloadHandler.text);
                    throw new ServerCommunicationException("JSON Parser Error: " + e.Message + "(" + www.downloadHandler.text + ")");
                }
                if (debugOutput) PlayURPlugin.Log(www.downloadHandler.text);

                //Debug.Log("json.HasKey(success)" + json.HasKey("success"));
                if (json != null && json.HasKey("success"))
                {
                    //Debug.Log("json[success].AsBool" + json["success"].AsBool);
                    if (json["success"].AsBool != true)
                    {
                        if (callback != null) callback(false, json);
                        yield break;
                    }
                }

                if (callback != null) callback(true, json);

            }
        }

        /// <summary>
        /// Standard HTTP PUT request. 
        /// Used for UPDATING data on the server.
        /// Has a callback for reading the response.
        /// </summary>
        /// <param name="page">The endpoint we are requesting (relative to <see cref="PlayURPlugin.SERVER_URL"/>/api/</param>
        /// <param name="id">id of the object we are updating data for.</param>
        /// <param name="form">Dictionary of key value pairs of information we want to send to the server.</param>
        /// <param name="callback">Callback for handling response from the server.</param>
        /// <param name="debugOutput">Optionally debug to the Unity console a bunch of information about how the request occurred. 
        /// Use only when things are failing and we need to know what the server is directly saying.</param>
        /// <exception cref="ServerCommunicationException">thrown when the server is unreachable.</exception>
        public static IEnumerator Put(string page, int id, Dictionary<string, string> form, ServerCallback callback = null, bool debugOutput = false)
        {
            form.Add("id", id.ToString());

            var jsonOut = new JSONObject();
            foreach (var kvp in form)
                jsonOut[kvp.Key] = kvp.Value;

            var url = PlayURPlugin.SERVER_URL + page + "/"; //the slash on the end is actually important....
            if (debugOutput) PlayURPlugin.Log("PUT "+url);
            using (var www = UnityWebRequest.Put(url, jsonOut.ToString()))
            {
                yield return www.SendWebRequest();

                JSONNode json;

                if (www.isNetworkError)
                {
                    PlayURPlugin.Log("Response Code: " + www.responseCode);
                    throw new ServerCommunicationException(www.error);
                }
                else if (www.isHttpError)
                {
                    json = JSON.Parse(www.downloadHandler.text);
                    PlayURPlugin.Log("Response Code: " + www.responseCode);
                    if (debugOutput) PlayURPlugin.Log(json);
                    if (callback != null) callback(false, null);
                    yield break;
                }

                try
                {
                    json = JSON.Parse(www.downloadHandler.text);
                }
                catch (System.Exception e)
                {
                    throw new ServerCommunicationException("JSON Parser Error: " + e.Message);
                }
                if (debugOutput)
                    PlayURPlugin.Log(www.downloadHandler.text);

                if (json["success"])
                {
                    if (json["success"].AsBool != true)
                    {
                        if (callback != null) callback(false, null);
                        yield break;
                    }
                }

                if (callback != null) callback(true, json);

            }
        }

        /// <summary>
        /// Helper function for building the <c>form</c> paramaters to the <see cref="Rest"/> class functions.
        /// Use this because it will automatically populate with the userID (from <see cref="PlayURPlugin.instance.user.id" />)
        /// and gameID (from <see cref="PlayURPlugin.instance.gameID"/>).
        /// Uses the terminology "WWWForm" because this class previously used <see cref="WWWForm"/> objects.
        /// </summary>
        /// <returns>A new Dictionary suitable for use as a <c>form parameter</c>.</returns>
        public static Dictionary<string, string> GetWWWForm()
        {
            var form = new Dictionary<string, string>();
            if (PlayURPlugin.instance.user != null) form.Add("userID", PlayURPlugin.instance.user.id.ToString());
            form.Add("gameID", PlayURPlugin.GameID.ToString());
            form.Add("clientSecret", PlayURPlugin.ClientSecret);
            return form;
        }

        /// <summary>
        /// Helper function for building the <c>form</c> paramaters to the <see cref="Rest"/> class functions.
        /// Use this because it will automatically populate with the userID (from <see cref="PlayURPlugin.instance.user.id" />)
        /// and gameID (from <see cref="PlayURPlugin.instance.gameID"/>).
        /// This version will also add in experimentID and experimentGroupID parameters, useful for some endpoints.
        /// Uses the terminology "WWWForm" because this class previously used <see cref="WWWForm"/> objects.
        /// </summary>
        /// <returns>A new Dictionary suitable for use as a <c>form parameter</c>.</returns>
        public static Dictionary<string, string> GetWWWFormWithExperimentInfo()
        {
            var form = GetWWWForm();
            form.Add("experimentID", ((int)PlayURPlugin.instance.CurrentExperiment).ToString());
            form.Add("experimentGroupID", ((int)PlayURPlugin.instance.CurrentExperimentGroup).ToString());
            return form;
        }


        /// <summary>
        /// HTTP GET request for file end-points. 
        /// Used for requesting information FROM the server.
        /// Has a callback for reading the response.
        /// </summary>
        /// <param name="page">The endpoint we are requesting (relative to <see cref="PlayURPlugin.SERVER_URL"/>/api/</param>
        /// <param name="form">Dictionary of key value pairs of information we want to send to the server.</param>
        /// <param name="callback">Callback for handling response from the server.</param>
        /// <param name="debugOutput">Optionally debug to the Unity console a bunch of information about how the request occurred. 
        /// Use only when things are failing and we need to know what the server is directly saying.</param>
        /// <exception cref="ServerCommunicationException">thrown when the server is unreachable.</exception>
        public static IEnumerator GetFile(string page, Dictionary<string, string> form, ServerFileCallback callback, bool debugOutput = false)
        {
            //var www = new WWW(SERVER_URL + page+".php", form.data);
            var kvp = "?";
            if (form != null)
            {
                foreach (var key in form)
                {
                    kvp += key.Key + "=" + key.Value + "&";
                }
            }

            var url = PlayURPlugin.SERVER_URL + page + "/" + kvp;
            if (page.IndexOf(".php") > 0)
                url = PlayURPlugin.SERVER_URL + page + kvp;
            if (debugOutput) PlayURPlugin.Log("GET " + url);

            using (var www = UnityWebRequest.Get(url))
            {
                yield return www.SendWebRequest();

                if (www.isNetworkError)
                {
                    PlayURPlugin.Log("Response Code: " + www.responseCode);
                    throw new ServerCommunicationException(www.error);
                }
                else if (www.isHttpError)
                {
                    PlayURPlugin.Log("Response Code: " + www.responseCode);
                    if (debugOutput) PlayURPlugin.Log(www.downloadHandler.text);
                    if (callback != null) callback(false, null);
                    yield break;
                }

                if (debugOutput) PlayURPlugin.Log(www.downloadHandler.text);
                
                callback(true, www.downloadHandler.data);

            }
        }
    }
}
