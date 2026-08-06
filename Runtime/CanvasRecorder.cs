using System;
using System.IO;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine;

namespace PlayUR
{
    public static class CanvasRecorder
    {
        private static bool isRecording = false;
        private static Action<byte[]> activeOnCompleteCallback;

        public static bool IsRecording => isRecording;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void JS_StartCanvasRecorder(int fps);

        [DllImport("__Internal")]
        private static extern void JS_StopCanvasRecorder(Action<IntPtr, int> callback);

        // Delegate matching dynCall('vpi', ...) -> void(int ptr, int length)
        private delegate void WebGLDataCallback(IntPtr bufferPtr, int bufferSize);

        [MonoPInvokeCallback(typeof(WebGLDataCallback))]
        private static void OnWebGLDataReceived(IntPtr bufferPtr, int bufferSize)
        {
            if (bufferPtr == IntPtr.Zero || bufferSize <= 0)
            {
                PlayUR.LogWarning("WebGL returned empty recording buffer.");
                activeOnCompleteCallback?.Invoke(null);
                activeOnCompleteCallback = null;
                return;
            }

            byte[] recordingBytes = new byte[bufferSize];
            Marshal.Copy(bufferPtr, recordingBytes, 0, bufferSize);

            PlayURPlugin.Log($"WebGL recording complete. Received {recordingBytes.Length} bytes.");

            Action<byte[]> callback = activeOnCompleteCallback;
            activeOnCompleteCallback = null;
            callback?.Invoke(recordingBytes);
        }
#endif

        /// <summary>
        /// Starts canvas recording. (Currently supported natively on WebGL only).
        /// </summary>
        /// <param name="targetCamera">Ignored on WebGL (captures full active Canvas stream).</param>
        /// <param name="targetRenderTexture">Ignored on WebGL.</param>
        /// <param name="width">Ignored on WebGL (defaults to native canvas render size).</param>
        /// <param name="height">Ignored on WebGL.</param>
        /// <param name="frameRate">Target frame capture rate (default 30 FPS).</param>
        public static void StartRecording(Camera targetCamera = null, RenderTexture targetRenderTexture = null, int width = 0, int height = 0, int frameRate = 30)
        {
            if (isRecording)
            {
                PlayURPlugin.LogWarning("Recording is already in progress.");
                return;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            isRecording = true;
            JS_StartCanvasRecorder(frameRate);
            PlayURPlugin.Log($"Started WebGL canvas recording @ {frameRate}fps.");
#else
            PlayURPlugin.LogWarning(
                "Screen/Canvas recording is currently only supported in WebGL builds. " +
                "If you need native recording support for Standalone (Windows/macOS), Mobile, or the Unity Editor, " +
                "please open a feature request on our project repository!"
            );
#endif
        }

        /// <summary>
        /// Stops recording and returns the raw WebM byte array via callback.
        /// </summary>
        /// <param name="onComplete">Callback returning raw file bytes.</param>
        public static void StopRecording(Action<byte[]> onComplete)
        {
            if (!isRecording)
            {
#if !UNITY_WEBGL || UNITY_EDITOR
                PlayURPlugin.LogWarning(
                    "Recording is currently only supported in WebGL builds. " +
                    "Please open a feature request on our project repository if you require non-WebGL support!"
                );
#else
                PlayURPlugin.LogWarning("No active recording to stop.");
#endif
                onComplete?.Invoke(null);
                return;
            }

            isRecording = false;

#if UNITY_WEBGL && !UNITY_EDITOR
            activeOnCompleteCallback = onComplete;
            JS_StopCanvasRecorder(OnWebGLDataReceived);
#else
            onComplete?.Invoke(null);
#endif
        }
    }
}