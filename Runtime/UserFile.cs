using System.Collections;
using System.Diagnostics;
using PlayUR.Core;
using System.IO;

namespace PlayUR
{
    public partial class PlayURPlugin : UnitySingletonPersistent<PlayURPlugin>
    {
        const string USER_FILE_API_ENDPOINT = "UserFile";

        /// <summary>
        /// Upload a file to the server for the current user. The file will be stored in the database and can be downloaded later via the <see cref="DownloadUserFile"/> method.
        /// If you need to retrieve the file later, you will need to store the ID returned in the callback. 
        /// <param name="fileData">The file data as a byte array.</param>
        /// <param name="fileName">The name of the file, including the extension.</param>
        /// <param name="mimeType">The MIME type of the file.</param>
        /// <param name="callback">The callback to invoke when the upload is complete, including the ID of the uploaded file.</param>
        /// <param name="debugOutput">Whether to output debug information.</param>
        /// </summary>
        public void UploadUserFile(byte[] fileData, string fileName, string mimeType, Rest.ServerCallback callback, bool debugOutput = true)
        {
            if (IsDetachedMode)
            {
                DetachedModeProxy.UploadUserFile( fileData, fileName, mimeType, callback);
                return;
            }

            var form = Rest.GetWWWFormWithExperimentInfo();
            if (CurrentSessionRunning)
            {
                form.Add("sessionId", CurrentSession.ToString());
            }

            StartCoroutine(Rest.EnqueuePostFile(USER_FILE_API_ENDPOINT, fileData, fileName, mimeType, form, callback, debugOutput: debugOutput));
        }

        /// <summary>
        /// Upload a file to the server for the current user. The file will be stored in the database and can be downloaded later via the <see cref="DownloadUserFile"/> method.
        /// If you need to retrieve the file later, you will need to store the ID returned
        /// </summary>
        /// <param name="filePath">The path to the file to upload.</param>
        /// <param name="callback">The callback to invoke when the upload is complete, including the ID of the uploaded file.</param>
        /// <param name="debugOutput">Whether to output debug information.</param>
        public void UploadUserFile(string filePath, Rest.ServerCallback callback, bool debugOutput = true)
        {
            if (System.IO.File.Exists(filePath))
            {
                var fileData = System.IO.File.ReadAllBytes(filePath);
                var fileName = System.IO.Path.GetFileName(filePath);
                var mimeType = GetMimeType(fileName);
                UploadUserFile(fileData, fileName, mimeType, callback, debugOutput);
            }
            else
            {
                UnityEngine.Debug.LogError($"UploadUserFile: File not found at path: {filePath}");
                callback?.Invoke(false, null);
            }
        }

        /// <summary>
        /// Upload a file to the server for the current user. The file will be stored in the database and can be downloaded later via the <see cref="DownloadUserFile"/> method.
        /// If you need to retrieve the file later, you will need to store the ID returned
        /// </summary>
        /// <param name="fileInfo">The file info for the file to upload.</param>
        /// <param name="callback">The callback to invoke when the upload is complete, including the ID of the uploaded file.</param>
        /// <param name="debugOutput">Whether to output debug information.</param>
        public void UploadUserFile(FileInfo fileInfo, Rest.ServerCallback callback, bool debugOutput = true)
        {
            if (fileInfo.Exists)
            {
                var fileData = System.IO.File.ReadAllBytes(fileInfo.FullName);
                var mimeType = GetMimeType(fileInfo.Name);
                UploadUserFile(fileData, fileInfo.Name, mimeType, callback, debugOutput);
            }
            else
            {
                UnityEngine.Debug.LogError($"UploadUserFile: File not found at path: {fileInfo.FullName}");
                callback?.Invoke(false, null);
            }
        }

        /// <summary>
        /// Download a file from the server for the current user. The file must have been previously uploaded via the <see cref="UploadUserFile"/> method.
        /// </summary>
        /// <param name="id">The ID of the file to download. This ID is returned in the callback of the <see cref="UploadUserFile"/> method, or when listing user files.</param>
        /// <param name="callback">The callback to invoke when the download is complete, including the downloaded file data as a byte array.</param>
        /// <param name="debugOutput">Whether to output debug information.</param>
        public void DownloadUserFile(int id, Rest.ServerFileCallback callback, bool debugOutput = true)
        {
            if (IsDetachedMode)
            {
                DetachedModeProxy.DownloadUserFile(id, callback);
                return;
            }
            StartCoroutine(Rest.EnqueueGetFile(USER_FILE_API_ENDPOINT, id, callback, debugOutput: debugOutput));
        }

        /// <summary>
        /// List all files uploaded by the current user. 
        /// </summary>
        /// <param name="callback">The callback to invoke when the list is retrieved, including the list of file ids.</param>
        /// <param name="debugOutput">Whether to output debug information.</param>
        public void ListUserFiles(Rest.ServerCallback callback, bool debugOutput = true)
        {
            if (IsDetachedMode)
            {
                DetachedModeProxy.ListUserFiles(callback);
                return;
            }

            var form = Rest.GetWWWForm();
            StartCoroutine(Rest.EnqueueGet(USER_FILE_API_ENDPOINT, form, callback, debugOutput: debugOutput));
        }

        internal string GetMimeType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            switch (extension)
            {
                case ".txt": return "text/plain";
                case ".jpg":
                case ".jpeg": return "image/jpeg";
                case ".png": return "image/png";
                case ".gif": return "image/gif";
                case ".pdf": return "application/pdf";
                case ".zip": return "application/zip";
                default: return "application/octet-stream"; // Default binary type
            }
        }

    
    
    }


}
