using System.Collections;
using System.Diagnostics;
using PlayUR.Core;
using System.IO;

namespace PlayUR
{
    public partial class PlayURPlugin : UnitySingletonPersistent<PlayURPlugin>
    {
        /// <summary>
        /// Returns true if a screen recording is currently active. Note that this may be inconsistent or incorrect, based upon browser compatability--use with caution.
        /// </summary>
        public bool IsScreenRecordingActive => CanvasRecorder.IsRecording;

        /// <summary>
        /// Begins recording the current screen or canvas. The recording will continue until <see cref="EndScreenRecording"/> or <see cref="EndScreenRecordingAndUploadFile"/> is called.
        /// </summary>
        public void BeginScreenRecording()
        {
            CanvasRecorder.StartRecording();
        }

        /// <summary>
        /// Ends the current screen recording and returns the final file bytes via callback. If you want to upload the recording to the server, use <see cref="EndScreenRecordingAndUploadFile"/> instead.
        /// <param name="callback">Callback invoked with the final recording bytes. If recording failed
        /// </summary>
        /// <param name="callback"></param>
        public void EndScreenRecording(System.Action<byte[]> callback = null)
        {
            CanvasRecorder.StopRecording(callback);
        }

        /// <summary>
        /// Ends the current screen recording and uploads the final file to the server. The file will be stored in the database and can be downloaded later via the <see cref="DownloadUserFile"/> method.
        /// If you need to retrieve the file later, you will need to store the ID returned in the callback. 
        /// <param name="filename">The name of the file, including the extension. If null, a default name will be generated.</param>
        /// <param name="callback">The callback to invoke when the upload is complete, including the ID of the uploaded file
        /// </summary>
        public void EndScreenRecordingAndUploadFile(string filename = null, Rest.ServerCallback callback = null)
        {
            if (string.IsNullOrEmpty(filename))
            {
                filename = $"screen_recording_{System.DateTime.Now.ToString("yyyyMMdd_HHmmss")}.webm";
            }

            CanvasRecorder.StopRecording((recordingBytes) =>
            {
                if (recordingBytes == null || recordingBytes.Length == 0)
                {
                    UnityEngine.Debug.LogError("EndScreenRecordingAndUploadFile: Recording failed or returned empty data.");
                    callback?.Invoke(false, null);
                    return;
                }

                UploadUserFile(recordingBytes, filename, "application/webm",callback);
            });
        }
    }


}
