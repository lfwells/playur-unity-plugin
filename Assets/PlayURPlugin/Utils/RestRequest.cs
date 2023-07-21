using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.Networking;

namespace PlayUR.Core
{
    /// <summary>
    /// Used by the <see cref="RestRequest"/> class to categorise REST requests.
    /// </summary>
    public enum RestMethod { GET, POST, PUT };

    /// <summary>
    /// Represents a single queued request to be sent to the PlayUR server.
    /// </summary>
    public class RestRequest
    {
        [XmlAttribute("id")]
        public ulong Order { get; set; }

        [XmlAttribute("method")]
        public RestMethod Method;

        [XmlAttribute("href")]
        public string Url
        {
            get => _url == null ? Endpoint : _url;
            set
            {
                _url = value;
            }
        }

        /// <summary>Was the data cleared an no longer available?</summary>
        [XmlAttribute("culled")]
        public bool IsCleared { get; set; } = false;

        private string _url = null;
        public string Endpoint;

        public DateTime SubmittedAt { get; set; }
        public DateTime RequestedAt { get; set; }

        /// <summary>Data sent with the request</summary>
        public byte[] Data { get; set; }

        /// <summary>Empty the data after the quest has been made.</summary>
        [XmlIgnore] public bool ClearPostRequest { get; set; } = false;
        [XmlIgnore] public bool IsCompleted => Response != null;
        [XmlIgnore] internal RestResponse Response { get; set; }

        internal UnityWebRequest CreateWebRequest()
        {
            switch (this.Method)
            {
                default:
                    throw new InvalidOperationException("Unkown method " + Method);

                case RestMethod.GET:
                    return UnityWebRequest.Get(Url);

                case RestMethod.PUT:
                    return UnityWebRequest.Put(Url, Data);

                case RestMethod.POST:
                    var wwwPost = UnityWebRequest.Put(Url, Data);
                    wwwPost.method = "post";
                    return wwwPost;
            }
        }
    }
    internal class RestResponse
    {
        public long StatusCode { get; }
        public bool IsHttpError => StatusCode >= 400;

        public bool IsNetworkError { get; }
        public string NetworkError { get; }

        public string Content { get; }

        public RestResponse(UnityWebRequest request)
        {
            if (!request.isDone)
                throw new ArgumentException("request must be completed", "request");

            StatusCode = request.responseCode;
            IsNetworkError = request.isNetworkError;
            NetworkError = request.error;
            Content = request.downloadHandler.text;
        }
    }

    [XmlRoot("Session")]
    public class RestHistoryRecord
    {
        public int Session { get; set; }
        public int GameID { get; set; }
        public User User { get; set; }
        public DateTime CreatedAt { get; set; }
        public RestRequest[] PastRequests { get; set; }
        public RestRequest[] PendingRequests { get; set; }
    }

    internal class RestQueue
    {
        /// <summary>What will we first back off to (in seconds)</summary>
        public float InitialBackoff { get; set; } = 0.5f;
        /// <summary>Is it currently processing?</summary>
        public bool IsProcessing { get; set; }
        /// <summary>Maximum time to allow for a back off before we give up (in seconds)</summary>
        public float MaxBackoffTime { get; set; } = 60 * 10;
        /// <summary>Name of the file that will be saved when the history is written</summary>
        public string HistoryFilename { get; set; } = "rest-queue.xml";

        private ulong _nextOrder = 0;

        private float _backoffSeconds = 0;
        private Queue<RestRequest> _pending;
        private List<RestRequest> _history;

        /// <summary>Made when a request is finished excecuting</summary>
        public event Action<RestRequest> RequestFinished;

        public RestQueue(int pendingCapacity = 25, int historyCapacity = 500)
        {
            _pending = new Queue<RestRequest>(pendingCapacity);
            _history = new List<RestRequest>(historyCapacity);
        }

        /// <summary>Pops the history and marks it as finished</summary>
        private void Pop()
        {
            var request = _pending.Dequeue();
            if (request.ClearPostRequest)
            {
                request.Data = null;
                request.IsCleared = true;
            }

            RequestFinished?.Invoke(request);
            _history.Add(request);
        }

        /// <summary>Stops processing the loop and processes the files IMMEDIATELY. This will block the thread until it's done.</summary>
        public void ProcessImmediate()
        {
            // Suggestion by https://stackoverflow.com/a/60447131/5010271
            Debug.Log("<color=#FF0000>WARNING: Request has been set to immedaite! This will break unity for a short while!!!</color>");
            IsProcessing = false;

            while (_pending.Count > 0)
            {
                _backoffSeconds = InitialBackoff;
                var restRequest = _pending.Peek();

                // While we are within the backoff time, lets keep requesting until success
                // (note we do backoff and not just true to ensure we dont hit a infinte loop)
                while (_backoffSeconds < MaxBackoffTime)
                {
                    restRequest.RequestedAt = DateTime.UtcNow;
                    using (var wwwRequest = restRequest.CreateWebRequest())
                    {
                        var operation = wwwRequest.SendWebRequest();
                        while (!operation.isDone)
                            System.Threading.Thread.Sleep(100);

                        // If there is a DNS/Network Error, OR a HTTP error that matches the code >= 500 (server error) or 408 (timeout)
                        if (wwwRequest.isNetworkError || (wwwRequest.isHttpError && (wwwRequest.responseCode >= 500 || wwwRequest.responseCode == 408)))
                        {
                            if (_backoffSeconds >= 1f)
                            {
                                PlayURPlugin.LogError($"Rest Queue: #{restRequest.Order} exceeded backoff time of {MaxBackoffTime}s. Giving up!!!!");
                                restRequest.Response = new RestResponse(wwwRequest);
                                break;
                            }
                            else
                            {
                                // We need to wait before looping again. Increment the backoff exponentially 
                                PlayURPlugin.LogWarning($"Rest Queue: Required to backoff and attempt {restRequest.Order} again. Waiting {_backoffSeconds}s");
                                System.Threading.Thread.Sleep(Mathf.FloorToInt(_backoffSeconds * 1000f));
                                _backoffSeconds *= 2;
                            }
                        }
                        else
                        {
                            PlayURPlugin.Log($"Rest Queue: #{restRequest.Order} finished (took backoff of {_backoffSeconds}s).");
                            restRequest.Response = new RestResponse(wwwRequest);
                            break;
                        }
                    }
                }

                Pop();
            }

            SaveToFile();
        }

        /// <summary>Starts a loop trying to process requests</summary>
        public IEnumerator StartProcessing()
        {
            IsProcessing = true;
            while (IsProcessing)
            {
                if (_pending.Count > 0)
                {
                    PlayURPlugin.Log("Premptive saving of history...");
                    SaveToFile();
                }

                while (_pending.Count > 0)
                {
                    _backoffSeconds = InitialBackoff;
                    var restRequest = _pending.Peek();

                    // While we are within the backoff time, lets keep requesting until success
                    // (note we do backoff and not just true to ensure we dont hit a infinte loop)
                    while (_backoffSeconds < MaxBackoffTime)
                    {
                        restRequest.RequestedAt = DateTime.UtcNow;
                        using (var wwwRequest = restRequest.CreateWebRequest())
                        {
                            yield return wwwRequest.SendWebRequest();

                            // If there is a DNS/Network Error, OR a HTTP error that matches the code >= 500 (server error) or 408 (timeout)
                            if (wwwRequest.isNetworkError || (wwwRequest.isHttpError && (wwwRequest.responseCode >= 500 || wwwRequest.responseCode == 408)))
                            {
                                if (_backoffSeconds >= MaxBackoffTime)
                                {
                                    PlayURPlugin.LogError($"Rest Queue: #{restRequest.Order} exceeded backoff time of {MaxBackoffTime}s. Giving up!!!!");
                                    restRequest.Response = new RestResponse(wwwRequest);
                                    break;
                                }
                                else
                                {
                                    // We need to wait before looping again. Increment the backoff exponentially 
                                    PlayURPlugin.LogWarning($"Rest Queue: Required to backoff and attempt {restRequest.Order} again. Waiting {_backoffSeconds}s");
                                    yield return new WaitForSeconds(_backoffSeconds);
                                    _backoffSeconds *= 2;
                                }
                            }
                            else
                            {
                                PlayURPlugin.Log($"Rest Queue: #{restRequest.Order} finished (took backoff of {_backoffSeconds}s).");
                                restRequest.Response = new RestResponse(wwwRequest);
                                break;
                            }
                        }
                    }

                    Pop();
                    SaveToFile();
                }

                yield return null;
            }
        }

        /// <summary>Serializes and compresses the history</summary>
        public byte[] Save(System.IO.Compression.CompressionLevel compression)
        {
            try
            {
                PlayURPlugin.Log("Serializing and Saving Rest history...");
                XmlSerializer serializer = new XmlSerializer(typeof(RestHistoryRecord));


                var dataset = new RestHistoryRecord()
                {
                    Session = PlayURPlugin.instance.CurrentSession,
                    GameID = PlayURPlugin.instance.gameID,
                    User = PlayURPlugin.instance.user,
                    CreatedAt = DateTime.UtcNow,
                    PastRequests = _history.ToArray(),
                    PendingRequests = _pending.ToArray()
                };

                using (var memoryStream = new MemoryStream())
                {
                    using (var compressionStream = new System.IO.Compression.DeflateStream(memoryStream, compression))
                    {
                        Stream targetStrean = compressionStream;
                        if (compression == System.IO.Compression.CompressionLevel.NoCompression)
                            targetStrean = memoryStream;

                        using (var textWriter = new StreamWriter(targetStrean))
                        {
                            serializer.Serialize(textWriter, dataset);

                        }
                    }

                    byte[] data = memoryStream.ToArray();
                    return data;
                }
            }
            catch (System.Exception ex)
            {
                PlayURPlugin.LogError("Exception occured while saving: " + ex.Message);
            }

            return new byte[0];
        }

        /// <summary>Writes the history and pending to file</summary>
        public void SaveToFile()
        {
            var data = Save(System.IO.Compression.CompressionLevel.NoCompression);

            string fullPath = System.IO.Path.Combine(Application.persistentDataPath, HistoryFilename);
            File.WriteAllBytes(fullPath, data);
            PlayURPlugin.Log("Finished Saving history to " + fullPath);
        }

        /// <summary>Enqueues a request to be compelted</summary>
        public ulong Enqueue(RestRequest request)
        {
            request.Order = _nextOrder++;
            request.SubmittedAt = DateTime.UtcNow;
            _pending.Enqueue(request);
            return request.Order;
        }
    }
}
