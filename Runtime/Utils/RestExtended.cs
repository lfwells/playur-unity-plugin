using PlayUR.Exceptions;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using System.Linq;

namespace PlayUR.Core
{
    public partial class Rest
    {
        private static RestQueue _queue = new RestQueue();
        internal static RestQueue Queue => _queue;

        /// <summary>
        /// Enqueues a GET command
        /// </summary>/// <param name="page">The endpoint we are requesting (relative to <see cref="PlayURPlugin.SERVER_URL"/>/api/</param>
        /// <param name="form">Dictionary of key value pairs of information we want to send to the server.</param>
        /// <param name="callback">Callback for handling response from the server.</param>
        /// <param name="debugOutput">Optionally debug to the Unity console a bunch of information about how the request occurred. 
        /// Use only when things are failing and we need to know what the server is directly saying.</param>
        /// <exception cref="ServerCommunicationException">thrown when the server is unreachable.</exception>
        public static IEnumerator EnqueueGet(string page, Dictionary<string, string> form, ServerCallback callback, bool debugOutput = false)
        {
            // yield return Get(page, form, callback, debugOutput);
            // yield break;

            // Prepare the deets
            string query = CreateQueryParams(form);
            string url = PlayURPlugin.SERVER_URL + page + "/" + query;
            if (page.IndexOf(".php") > 0)
                url = PlayURPlugin.SERVER_URL + page + query;

            if (debugOutput)
                PlayURPlugin.Log("GET " + url);

            // Push the request
            RestRequest request = new RestRequest()
            {
                Method = RestMethod.GET,
                Endpoint = page,
                Url = url,
                Data = null,
            };

            Queue.Enqueue(request);
            yield return new WaitUntil(() => request.IsCompleted);

            if (debugOutput)
                PlayURPlugin.Log($"GET REQUEST FINISHED\nStatus: {request.Response.StatusCode}\nContent:\n{request.Response.Content}");

            // EnqueueRequest will execute it eventually and will do fallback.
            // If it's still bad it is appropriate to throw errors
            if (request.Response.IsNetworkError)
            {
                PlayURPlugin.Log($"Failed to GET (Network): {request.Response.NetworkError}");
                throw new ServerCommunicationException(request.Response.NetworkError);
            }

            // Process the JSON
            JSONNode json;
            try
            {
                json = JSON.Parse(request.Response.Content);
                if (json == null) { throw new System.Exception();  }
            }
            catch (System.Exception ex)
            {
                PlayURPlugin.Log($"Failed to GET (JSON): {ex.Message} (status: {request.Response.StatusCode})");
                throw new ServerCommunicationException("JSON Parser Error: " + ex.Message + "\nRaw: '" + request.Response.Content + "'\n" +
                    "Page: " + page + "\n" +
                    "Form: " + string.Join(",", form.Select((kvp) => kvp.Key + "=" + kvp.Value))
                );
            }

            // Return if HTTP error
            if (request.Response.IsHttpError)
            {
                PlayURPlugin.Log($"Failed to GET (HTTP): {request.Response.StatusCode}");
                callback?.Invoke(false, json);
                yield break;
            }

            if (json["success"] && json["success"].AsBool != true)
            {
                callback?.Invoke(false, json);
                yield break;
            }

            callback?.Invoke(true, json);
        }

        /// <summary>
        /// Enqueues a POST command
        /// </summary>
        /// <param name="page">The endpoint we are requesting (relative to <see cref="PlayURPlugin.SERVER_URL"/>/api/</param>
        /// <param name="form">Dictionary of key value pairs of information we want to send to the server.</param>
        /// <param name="callback">Callback for handling response from the server.</param>
        /// <param name="debugOutput">Optionally debug to the Unity console a bunch of information about how the request occurred. 
        /// Use only when things are failing and we need to know what the server is directly saying.</param>
        /// <param name="storeFormInHistory">Stores the <paramref name="form"></paramref> in the history after the request has been made.</param>
        /// <returns></returns>
        public static IEnumerator EnqueuePost(string page, Dictionary<string, string> form, ServerCallback callback = null, bool HTMLencode = false, bool debugOutput = false, bool storeFormInHistory = true)
        {
            // yield return Get(page, form, callback, debugOutput);
            // yield break;

            // Prepare the deets
            JSONObject jsonOut = CreateJSONParams(form, HTMLencode);
            string url = PlayURPlugin.SERVER_URL + page + "/";

            if (debugOutput)
                PlayURPlugin.Log("POST " + url);

            // Push the request
            RestRequest request = new RestRequest()
            {
                Method = RestMethod.POST,
                Endpoint = page,
                Url = url,
                Data = System.Text.Encoding.UTF8.GetBytes(jsonOut.ToString()),
                ClearPostRequest = !storeFormInHistory
            };

            Queue.Enqueue(request);
            yield return new WaitUntil(() => request.IsCompleted);

            if (debugOutput)
                PlayURPlugin.Log($"POST REQUEST FINISHED\nStatus: {request.Response.StatusCode}\nContent:\n{request.Response.Content}");

            // EnqueueRequest will execute it eventually and will do fallback.
            // If it's still bad it is appropriate to throw errors
            if (request.Response.IsNetworkError)
            {
                PlayURPlugin.Log($"Failed to POST (Network): {request.Response.NetworkError}");
                throw new ServerCommunicationException(request.Response.NetworkError);
            }

            // Process the JSON
            JSONNode json;
            try
            {
                json = JSON.Parse(request.Response.Content);
                if (json == null) { throw new System.Exception();  }
            }
            catch (System.Exception ex)
            {
                PlayURPlugin.Log($"Failed to POST (JSON): {ex.Message} (status: {request.Response.StatusCode})");
                throw new ServerCommunicationException("JSON Parser Error: " + ex.Message + "\nRaw: '" + request.Response.Content + "'\n" +
                    "Page: " + page + "\n" +
                    "Form: " + string.Join(",", form.Select((kvp) => kvp.Key + "=" + kvp.Value))
                );
            }

            // Return if HTTP error
            if (request.Response.IsHttpError)
            {
                PlayURPlugin.Log($"Failed to POST (HTTP): {request.Response.StatusCode}");
                callback?.Invoke(false, json);
                yield break;
            }

            if (json != null && json.HasKey("success") && json["success"].AsBool != true)
            {
                callback?.Invoke(false, json);
                yield break;
            }

            callback?.Invoke(true, json);
        }

        /// <summary>
        /// Enqueues a PUT command
        /// </summary>
        /// <param name="page">The endpoint we are requesting (relative to <see cref="PlayURPlugin.SERVER_URL"/>/api/</param>
        /// <param name="id">id of the object we are updating data for.</param>
        /// <param name="form">Dictionary of key value pairs of information we want to send to the server.</param>
        /// <param name="callback">Callback for handling response from the server.</param>
        /// <param name="debugOutput">Optionally debug to the Unity console a bunch of information about how the request occurred. </param>
        /// <param name="storeFormInHistory">Stores the <paramref name="form"></paramref> in the history after the request has been made.</param>
        /// Use only when things are failing and we need to know what the server is directly saying.</param>
        /// <returns></returns>
        public static IEnumerator EnqueuePut(string page, int id, Dictionary<string, string> form, ServerCallback callback = null, bool HTMLencode = false, bool debugOutput = false, bool storeFormInHistory = true)
        {
            form.Add("id", id.ToString());

            // Prepare the deets
            JSONObject jsonOut = CreateJSONParams(form, HTMLencode);
            string url = PlayURPlugin.SERVER_URL + page + "/";

            if (debugOutput)
                PlayURPlugin.Log("PUT " + url);

            // Push the request
            RestRequest request = new RestRequest()
            {
                Method = RestMethod.PUT,
                Endpoint = page,
                Url = url,
                Data = System.Text.Encoding.UTF8.GetBytes(jsonOut.ToString()),
                ClearPostRequest = !storeFormInHistory
            };

            Queue.Enqueue(request);
            yield return new WaitUntil(() => request.IsCompleted);

            if (debugOutput)
                PlayURPlugin.Log($"PUT REQUEST FINISHED\nStatus: {request.Response.StatusCode}\nContent:\n{request.Response.Content}");

            // EnqueueRequest will execute it eventually and will do fallback.
            // If it's still bad it is appropriate to throw errors
            if (request.Response.IsNetworkError)
            {
                PlayURPlugin.Log($"Failed to PUT (Network): {request.Response.NetworkError}");
                throw new ServerCommunicationException(request.Response.NetworkError);
            }

            // Process the JSON
            JSONNode json;
            try
            {
                json = JSON.Parse(request.Response.Content);
                if (json == null) { throw new System.Exception();  }
            }
            catch (System.Exception ex)
            {
                PlayURPlugin.Log($"Failed to PUT (JSON): {ex.Message} (status: {request.Response.StatusCode})");
                throw new ServerCommunicationException("JSON Parser Error: " + ex.Message + "\nRaw: '" + request.Response.Content + "'\n" +
                    "Page: " + page + "\n" +
                    "Form: " + string.Join(",", form.Select((kvp) => kvp.Key + "=" + kvp.Value))
                );
            }

            // Return if HTTP error
            if (request.Response.IsHttpError)
            {
                PlayURPlugin.Log($"Failed to PUT (HTTP): {request.Response.StatusCode}");
                callback?.Invoke(false, json);
                yield break;
            }

            if (json != null && json.HasKey("success") && json["success"].AsBool != true)
            {
                callback?.Invoke(false, json);
                yield break;
            }

            callback?.Invoke(true, json);
        }

        /// <summary>Creates a query param from the given form.</summary>
        internal static string CreateQueryParams(Dictionary<string, string> form)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append('?');
            if (form != null)
            {
                foreach (var pair in form)
                {
                    builder.Append(pair.Key)
                            .Append('=')
                            .Append(pair.Value)
                            .Append('&');
                }
            }
            return builder.ToString();
        }
        internal static JSONObject CreateJSONParams(Dictionary<string, string> form, bool HTMLEncode = false)
        {
            var jsonOut = new JSONObject();
            foreach (var kvp in form)
            {
                if (HTMLEncode)
                    jsonOut[kvp.Key] = System.Net.WebUtility.HtmlEncode(kvp.Value);
                else
                    jsonOut[kvp.Key] = kvp.Value;
            }
            return jsonOut;
        }
    }
}