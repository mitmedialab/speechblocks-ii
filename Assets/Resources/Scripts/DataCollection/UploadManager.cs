using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using SimpleJSON;
using RestSharp;

public class UploadManager : MonoBehaviour
{
    void Init()
    {
        if (null != synologyPath) return;
        JSONNode keyConfig = Config.GetConfig("KeyConfig");
        synologyPath = keyConfig["SynologyPath"];
        synologyUser = keyConfig["SynologyUser"];
        synologyPassword = keyConfig["SynologyPassword"];
    }

    private void Update()
    {
        uploadRunner.Update();
    }

    public void ScheduleDirectoryUpload(string sourceDirectory, string targetDirectory, string targetFileType, bool deleteWhenDone)
    {
        Init();
        IEnumerator<string> fileEnumerator = FileUtil.EnumerateFilesRecursively(sourceDirectory);
        while (fileEnumerator.MoveNext())
        {
            string sourcePath = fileEnumerator.Current;
            if (!sourcePath.EndsWith(targetFileType)) continue;
            string targetPath = sourcePath.Substring(sourceDirectory.Length);
            if (targetPath.StartsWith("/")) { targetPath = targetPath.Substring(1); }
            targetPath = $"{targetDirectory}/{targetPath}";
            ScheduleUpload(sourcePath, targetPath, deleteWhenDone);
        }
    }

    public void ScheduleUpload(string sourcePath, string targetPath, bool deleteWhenDone)
    {
        Init();
        uploads.Add(new ScheduledUpload(sourcePath, targetPath, deleteWhenDone));
        if (!uploadRunner.IsRunning())
        {
            uploadRunner.SetCoroutine(Uploader());
        }
    }

    private IEnumerator Uploader()
    {
        Init();
        while (uploads.Count > 0)
        {
            while (null == session_id)
            {
                Debug.Log("Synology: attempting login...");
                yield return Login();
                if (null == session_id) yield return null;
            }
            Debug.Log("Synology: login successful!");
            while (uploads.Count > 0)
            {
                for (int i = 0; i < uploads.Count; ++i)
                {
                    ScheduledUpload upload = uploads[i];
                    Debug.Log("Synology: attempting upload of " + upload.sourcePath);
                    yield return Upload(upload);
                    if (uploadSuccess) {
                        Debug.Log("Synology: upload successful!");
                        if (upload.deleteWhenDone) { File.Delete(upload.sourcePath); }
                        uploads.RemoveAt(i);
                        --i;
                    }
                    else
                    {
                        Debug.Log("Synology: upload failed!");
                        yield return null;
                    }
                }
            }
            Debug.Log("Synology: logging out...");
            yield return Logout();
            Debug.Log("Synology: done!");
        }
    }

    private IEnumerator Login()
    {
        session_id = null;
        var client = new RestClient(synologyPath + "/auth.cgi");
        var request = new RestRequest();
        request.OnBeforeDeserialization = resp => { resp.ContentType = "application/json"; };
        request.AddParameter("api", "SYNO.API.Auth");
        request.AddParameter("version", "3");
        request.AddParameter("method", "login");
        request.AddParameter("account", synologyUser);
        request.AddParameter("passwd", synologyPassword);
        request.AddParameter("session", "FileStation");
        request.AddParameter("format", "cookie");
        yield return ExecuteRequest(synologyPath + "/auth.cgi", request, contentNode => session_id = contentNode["data"]["sid"]);
    }

    private IEnumerator Upload(ScheduledUpload upload)
    {
        RestRequest request = new RestRequest(Method.POST);
        uploadSuccess = false;
        request.AddParameter("_sid", session_id, ParameterType.QueryString);
        request.AddParameter("api", "SYNO.FileStation.Upload", ParameterType.QueryString);
        request.AddParameter("version", "2", ParameterType.QueryString);
        request.AddParameter("method", "upload", ParameterType.QueryString);
        request.AddParameter("path", Path.GetDirectoryName(upload.targetPath));
        request.AddParameter("create_parents", "true");
        request.AddParameter("overwrite", "true");
        request.AddFile(Path.GetFileName(upload.targetPath), upload.sourcePath);
        yield return ExecuteRequest(synologyPath + "/entry.cgi", request, contentNode => uploadSuccess = contentNode["success"]);
    }

    private IEnumerator Logout()
    {
        var request = new RestRequest();
        request.AddParameter("api", "SYNO.API.Auth");
        request.AddParameter("version", "1");
        request.AddParameter("method", "logout");
        request.AddParameter("session", "FileStation");
        yield return ExecuteRequest(synologyPath + "/auth.cgi", request, callback: null);
    }

    private IEnumerator ExecuteRequest(string url, RestRequest request, Action<JSONNode> callback)
    {
        var client = new RestClient(url);
        ResponseStatus responseStatus = ResponseStatus.None;
        client.ExecuteAsync(request, response =>
        {
            responseStatus = response.ResponseStatus != ResponseStatus.None ? response.ResponseStatus : ResponseStatus.Aborted;
            if (null != callback) {
                try
                {
                    callback(JSONNode.Parse(response.Content));
                }
                catch (Exception e)
                {
                    ExceptionUtil.OnException(e);
                }
            }
        });
        while (ResponseStatus.None == responseStatus) yield return null;
    }

    private class ScheduledUpload
    {
        public string sourcePath { get; }
        public string targetPath { get; }
        public bool deleteWhenDone { get; }

        public ScheduledUpload(string sourcePath, string targetPath, bool deleteWhenDone)
        {
            this.sourcePath = sourcePath;
            this.targetPath = $"/DataSpeechBlocks/{targetPath}";
            this.deleteWhenDone = deleteWhenDone;
        }
    }

    private string synologyPath = null;
    private string synologyUser = null;
    private string synologyPassword = null;
    private string session_id = null;
    private bool uploadSuccess = false;
    private List<ScheduledUpload> uploads = new List<ScheduledUpload>();
    private CoroutineRunner uploadRunner = new CoroutineRunner();
}
