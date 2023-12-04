using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using UnityEngine;
using System.Linq;
using System.Text;

namespace PlayUR
{
    public class PlayURLoginWebServer
    {
        private TcpListener myListener;
        private static int port = 5050;
        private static IPAddress localAddr = IPAddress.Parse("127.0.0.1");

        public delegate void OnLoginSuccess(string json);

        OnLoginSuccess callback;

        public PlayURLoginWebServer(OnLoginSuccess callback)
        {
            this.callback = callback;

            myListener = new TcpListener(localAddr, port);
            myListener.Start();
            PlayURPlugin.Log($"Login Web Server Running on {localAddr.ToString()} on port {port}... ");
            Thread th = new Thread(new ThreadStart(StartListen));
            th.Start();

            //now we can launch playur login page
            Application.OpenURL("https://playur.io/api/Login/standalone.php");
        }

        private void StartListen()
        {
            //while (true) //just need one good request then done
            {

                TcpClient client = myListener.AcceptTcpClient();
                NetworkStream stream = client.GetStream();

                //read request 
                byte[] requestBytes = new byte[1024];
                int bytesRead = stream.Read(requestBytes, 0, requestBytes.Length);

                string request = Encoding.UTF8.GetString(requestBytes, 0, bytesRead);
                var requestHeaders = ParseHeaders(request);

                string[] requestFirstLine = requestHeaders.requestType.Split(" ");
                string httpVersion = requestFirstLine.LastOrDefault();
                string contentType = requestHeaders.headers.GetValueOrDefault("Accept");
                string contentEncoding = requestHeaders.headers.GetValueOrDefault("Acept-Encoding");

                if (!request.StartsWith("GET"))
                {
                    SendHeaders(httpVersion, 405, "Method Not Allowed", contentType, contentEncoding, 0, ref stream);
                }
                else
                {
                    var requestedPath = requestFirstLine[1];
                    if (requestedPath.StartsWith("/?auth"))
                    {
                        var fileContent = GetContent(requestedPath);
                        if (fileContent != null)
                        {
                            SendHeaders(httpVersion, 200, "OK", contentType, contentEncoding, 0, ref stream);
                            stream.Write(fileContent, 0, fileContent.Length);
                        }
                    }
                    else
                    {
                        SendHeaders(httpVersion, 404, "Page Not Found", contentType, contentEncoding, 0, ref stream);
                    }
                }


                client.Close();
                //keep this thread alive just a little while before spinning down server
                //Thread.Sleep(5000);
            }
            myListener.Stop();
        }
        private byte[] GetContent(string requestedPath)
        {
            if (requestedPath == "/") requestedPath = "default.html";

            var text = requestedPath.Replace("/?auth=", "");
            callback(text);

            //convert text to byte array
            var redirect = "<html><head><meta http-equiv=\"refresh\" content=\"0; url=https://playur.io/standaloneLoggedIn.php\" /></head><body>Redirecting...</body></html>";
            byte[] file = Encoding.UTF8.GetBytes(redirect);
            return file;

        }

        private void SendHeaders(string? httpVersion, int statusCode, string statusMsg, string? contentType, string? contentEncoding,
                int byteLength, ref NetworkStream networkStream)
        {
            string responseHeaderBuffer = "";

            responseHeaderBuffer = $"HTTP/1.1 {statusCode} {statusMsg}\r\n" +
                $"Connection: Keep-Alive\r\n" +
                $"Date: {DateTime.UtcNow.ToString()}\r\n" +
                $"Location: https://playur.io/standaloneLoggedIn.php\r\n\r\n";

            byte[] responseBytes = Encoding.UTF8.GetBytes(responseHeaderBuffer);
            networkStream.Write(responseBytes, 0, responseBytes.Length);
        }

        private (Dictionary<string, string> headers, string requestType) ParseHeaders(string headerString)
        {
            var headerLines = headerString.Split('\r', '\n');
            string firstLine = headerLines[0];
            var headerValues = new Dictionary<string, string>();
            foreach (var headerLine in headerLines)
            {
                var headerDetail = headerLine.Trim();
                var delimiterIndex = headerLine.IndexOf(':');
                if (delimiterIndex >= 0)
                {
                    var headerName = headerLine.Substring(0, delimiterIndex).Trim();
                    var headerValue = headerLine.Substring(delimiterIndex + 1).Trim();
                    headerValues.Add(headerName, headerValue);
                }
            }
            return (headerValues, firstLine);
        }
    }
}