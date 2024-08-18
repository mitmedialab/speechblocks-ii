using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using UnityEngine;
using SimpleJSON;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using UnityEngine.Networking;

// This is just a wrapper over Firebase Unity plugin to fix its issue with persistent storage
// (when persistence is activated, the system doesn't ingest remote updates and relies on local cache for some reason).
// This problem may be fixed in future versions of the Firebase plugin.
// If that happens, consider replacing this class with native persistence.
// Although this class also has a secondary function: to serve as a local database when Firebase is not used at all.
// Note: this system operates under the assumption that entries never get deleted from the database.
// Note: some optimization can be later arranged for writing flags
public class DatabaseKeeper : MonoBehaviour
{
    public void Init()
    {
        cacheRoot = Path.Combine(Application.persistentDataPath, "DB-Cache");
        overwritesRoot = Path.Combine(Application.persistentDataPath, "DB-Overwrites");
    }

    void Update()
    {
        synchronizeRunner.Update();
    }

    public IEnumerator ConnectToFirebase()
    {
        Debug.Log("DEVICE NAME: " + SystemInfo.deviceName);
        Debug.Log($"CACHE ROOT: {cacheRoot}");
        Debug.Log("START FIREBASE INITIALIZATION");
        Task<DependencyStatus> dependenciesTask = FirebaseApp.CheckAndFixDependenciesAsync();
        yield return new WaitUntil(() => dependenciesTask.IsCompleted);
        if (dependenciesTask.Result != DependencyStatus.Available) {
            Debug.Log("FIREBASE DEPENDENCIES FAILED");
            yield break;
        }
        Debug.Log("FIREBASE DEPENDENCIES SUCCESSFUL");
        FirebaseDatabase.DefaultInstance.SetPersistenceEnabled(false); // This class will handle persistence
        FirebaseAuth firebaseAuth = FirebaseAuth.DefaultInstance;
        JSONNode keyConfig = Config.GetConfig("KeyConfig");
        string firebaseEmail = keyConfig["FirebaseID"];
        string firebasePassword = keyConfig["FirebaseKey"];
        Task<Firebase.Auth.AuthResult> signUpTask = firebaseAuth.CreateUserWithEmailAndPasswordAsync(firebaseEmail, firebasePassword);
        yield return new WaitUntil(() => signUpTask.IsCompleted);
        if (signUpTask.IsFaulted) {
            Debug.Log("FIREBASE COULDN'T CREATE USER");
        } else {
             Debug.Log("FIREBASE USER CREATION SUCCESSFUL");
        }
        Task<Firebase.Auth.AuthResult> signInTask = firebaseAuth.SignInWithEmailAndPasswordAsync(firebaseEmail, firebasePassword);
        yield return new WaitUntil(() => signInTask.IsCompleted);
        if (signInTask.IsFaulted)
        {   Debug.Log("FIREBASE LOGIN FAILED"); }
        else
        {
            Debug.Log("FIREBASE LOGIN SUCCESSFUL");
            connectedToFirebase = true;
            synchronizeRunner.SetCoroutine(SyncWrites());
        }
    }

    public bool IsConnected()
    {
        return connectedToFirebase;
    }

    public IEnumerator Load(string path, CoroutineResult<string> result, bool isImmutable)
    {
        result.Reset();
        var cachePath = $"{cacheRoot}/{path}.json";
        if (isImmutable && File.Exists(cachePath))
        {
            string record = ReadRecord(cachePath);
            result.Set(record);
        }
        else
        {
            var overwritePath = $"{overwritesRoot}/{path}.json";
            var snapshotResult = new CoroutineResult<DataSnapshot>();
            yield return LoadShapshotFromFirebase(path, snapshotResult, 1.0f);
            if (!snapshotResult.WasSuccessful())
            {
                if (File.Exists(overwritePath))
                {
                    string record = ReadRecord(overwritePath);
                    if (null != record) { result.Set(record); }
                }
                else
                {
                    string record = ReadRecord(cachePath);
                    if (null != record) { result.Set(record); }
                }
            }
            else if (null != snapshotResult.GetResult() && snapshotResult.GetResult().Exists)
            {
                string databaseRecord = snapshotResult.GetResult()?.GetRawJsonValue();
                string cacheRecord = ReadRecord(cachePath);
                string overwriteRecord = ReadRecord(overwritePath);
                string resolvedRecord = ResolveRecord(databaseRecord, cacheRecord, overwriteRecord);
                if (resolvedRecord == databaseRecord && resolvedRecord != cacheRecord) // update cache record if needed
                {
                    WriteRecord(cachePath, databaseRecord);
                }
                result.Set(resolvedRecord);
            }
            else
            {
                string overwriteRecord = ReadRecord(overwritePath);
                result.Set(overwriteRecord);
                if (null == overwriteRecord && File.Exists(cachePath)) { File.Delete(cachePath); }
            }    
        }
    }

    public void Write(string path, string record)
    {
        if (!path.StartsWith(LOCAL_ONLY_PATH))
        {
            string overwritePath = $"{overwritesRoot}/{path}.json";
            WriteRecord(overwritePath, record);
            QueueUpload(path);
        }
        else
        {
            string cachePath = $"{cacheRoot}/{path}.json";
            WriteRecord(cachePath, record);
        }
    }

    public void WriteIn(string path, string pathInFile, JSONNode recordJSON)
    {
        if ("" == pathInFile)
        {
            Write(path, recordJSON.ToString());
        }
        else
        {
            string targetRecord;
            string overwritePath = $"{overwritesRoot}/{path}.json";
            string cachePath = $"{cacheRoot}/{path}.json";
            if (File.Exists(overwritePath)) { targetRecord = ReadRecord(overwritePath); }
            else if (File.Exists(cachePath)) { targetRecord = ReadRecord(cachePath); }
            else { targetRecord = "{}"; }
            if (null == targetRecord) { targetRecord = "{}"; }
            JSONNode targetRecordJSON = JSONNode.Parse(targetRecord);
            Integrate(targetRecordJSON, pathInFile.Split('/'), 0, recordJSON);
            string record = targetRecordJSON.ToString();
            Write(path, record);
        }
    }

    private void Integrate(JSONNode targetRecordJSON, string[] path, int iInPath, JSONNode recordJSON)
    {
        string key = path[iInPath];
        if (iInPath == path.Length - 1)
        {
            targetRecordJSON[key] = recordJSON;
        }
        else
        {
            if (null == targetRecordJSON[key]) { targetRecordJSON[key] = new JSONObject(); }
            Integrate(targetRecordJSON[key], path, iInPath + 1, recordJSON);
        }
    }

    private IEnumerator LoadShapshotFromFirebase(string path, CoroutineResult<DataSnapshot> result, float timeout)
    {
        if (path.StartsWith(LOCAL_ONLY_PATH)) yield break;
        Debug.Log($"Loading from Firebase: {path}");
        result.Reset();
        if (!connectedToFirebase)
        {
            Debug.Log($"Firebase not connected (path: {path})");
            yield break;
        }
        FirebaseDatabase database;
        Task<DataSnapshot> databaseLoadingTask;
        try
        {
            database = FirebaseDatabase.DefaultInstance;
            databaseLoadingTask = database.GetReference(path).GetValueAsync();
        }
        catch
        {
            Debug.Log($"Failed to load from Firebase (path: {path})");
            yield break;
        }
        double t_max = TimeKeeper.time + timeout;
        while (!databaseLoadingTask.IsCompleted && TimeKeeper.time < t_max) { yield return null; }
        if (databaseLoadingTask.IsCompleted && !databaseLoadingTask.IsFaulted)
        {
            Debug.Log($"LOADED from Firebase (path: {path})");
            result.Set(databaseLoadingTask.Result);
        }
        else
        {
            Debug.Log($"Failed to load from Firebase (path: {path})");
        }
    }

    private string ResolveRecord(string databaseRecord, string cacheRecord, string overwriteRecord)
    {
        if (null == overwriteRecord)
        {
            if (null != databaseRecord) return databaseRecord;
            return cacheRecord;
        }
        else // TODO: if we expect the child to log in from several stations, we can introduce a procedure to merge records
        {
            if (null == databaseRecord || databaseRecord == cacheRecord) return overwriteRecord;
            if (databaseRecord == overwriteRecord) return databaseRecord;
            JSONNode mergedRecord = MergeRecords(   SerializationUtil.ParseOrNull(databaseRecord),
                                                    SerializationUtil.ParseOrNull(cacheRecord),
                                                    SerializationUtil.ParseOrNull(overwriteRecord));
            return mergedRecord.ToString();
        }
    }

    private JSONNode MergeRecords(JSONNode databaseRecord, JSONNode cacheRecord, JSONNode overwriteRecord)
    {
        if (databaseRecord == cacheRecord) return overwriteRecord;
        if (overwriteRecord == cacheRecord) return databaseRecord;
        if (typeof(JSONObject).IsInstanceOfType(databaseRecord)
            && typeof(JSONObject).IsInstanceOfType(overwriteRecord))
        {
            JSONNode merged = new JSONObject();
            HashSet<string> keysUnion = new HashSet<string>();
            AddJSONKeys(databaseRecord, keysUnion);
            AddJSONKeys(overwriteRecord, keysUnion);
            foreach (string key in keysUnion)
            {
                JSONNode mergedChild = MergeRecords(databaseRecord?[key],
                                                    cacheRecord?[key],
                                                    overwriteRecord?[key]);
                if (null != mergedChild) { merged[key] = mergedChild; }
            }
            return merged;
        }
        else
        {
            if (null != databaseRecord) return databaseRecord;
            if (null != overwriteRecord) return overwriteRecord;
            return cacheRecord;
        }
    }

    private void AddJSONKeys(JSONNode json, HashSet<string> keysUnion)
    {
        if (null != json) { foreach (string key in json.Keys) { keysUnion.Add(key); } }
    }

    private string ReadRecord(string localPath)
    {
        if (!File.Exists(localPath)) return null;
        try
        {
            return File.ReadAllText(localPath);
        }
        catch
        {
            return null;
        }
    }

    private void WriteRecord(string localPath, string record)
    {
        if (null == record) return;
        try
        {
            string localDirectory = Path.GetDirectoryName(localPath);
            if (!Directory.Exists(localDirectory)) { Directory.CreateDirectory(localDirectory); }
            File.WriteAllText(localPath, record);
        }
        catch
        {
            Debug.Log("FAILED TO STORE RECORD: " + localPath);
        }
    }

    private IEnumerator SyncWrites()
    {
        try { if (Directory.Exists(overwritesRoot)) { PopulateUploadQueue(overwritesRoot); } } catch { Debug.Log("ISSUE POPULATING UPLOAD QUEUE"); }
        uploadQueue.OrderBy(path => File.GetLastWriteTime($"{overwritesRoot}/{path}.json"));
        while (true) {
            while (0 == uploadQueue.Count) yield return null;
            string path = uploadQueue[0];
            uploadQueue.RemoveAt(0);
            var cachePath = $"{cacheRoot}/{path}.json";
            var overwritePath = $"{overwritesRoot}/{path}.json";
            var loadingResult = new CoroutineResult<DataSnapshot>();
            Debug.Log($"Sync {path}: database read");
            while (!loadingResult.WasSuccessful())
            {
                yield return LoadShapshotFromFirebase(path, loadingResult, 10000000f);
            }
            Debug.Log($"Sync {path}: resolving...");
            string databaseRecord = loadingResult.GetResult()?.GetRawJsonValue();
            string cacheRecord = ReadRecord(cachePath);
            string overwriteRecord = ReadRecord(overwritePath);
            string resolvedRecord = ResolveRecord(databaseRecord, cacheRecord, overwriteRecord);
            Debug.Log($"Sync {path}: record resolved");
            if (resolvedRecord != overwriteRecord) { WriteRecord(overwritePath, resolvedRecord); }
            if (resolvedRecord != databaseRecord)
            {
                while (true)
                {
                    Debug.Log($"Sync {path}: database write");
                    Task writeTask = null;
                    try
                    {
                        writeTask = FirebaseDatabase.DefaultInstance.GetReference(path).SetRawJsonValueAsync(resolvedRecord);
                    }
                    catch
                    {
                        break;
                    }
                    while (!writeTask.IsCompleted) yield return null;
                    if (!writeTask.IsFaulted)
                    {
                        Debug.Log($"Sync {path}: success");
                        WriteRecord(cachePath, resolvedRecord);
                        try { File.Delete(overwritePath); } catch { }
                        break;
                    }
                    else
                    {
                        Debug.Log($"Sync {path}: faulted");
                        yield return null;
                    }
                }
            }
            else
            {
                Debug.Log($"Sync {path}: no update needed");
            }
        }
    }

    private void PopulateUploadQueue(string path)
    {
        foreach (string filePath in Directory.GetFiles(path))
        {
            if (filePath.EndsWith(".json"))
            {
                string databasePath = filePath.Substring(overwritesRoot.Length + 1, filePath.Length - overwritesRoot.Length - 6);
                uploadQueue.Add(databasePath);
            }
        }
        foreach (string directoryPath in Directory.GetDirectories(path))
        {
            PopulateUploadQueue(directoryPath);
        }
    }

    private void QueueUpload(string path)
    {
        int qIndex = uploadQueue.IndexOf(path);
        if (qIndex >= 0) { uploadQueue.RemoveAt(qIndex); }
        uploadQueue.Add(path);
    }

    private bool connectedToFirebase = false;
    private const float LOADING_TIMEOUT = 10f;
    private string cacheRoot = null;
    private string overwritesRoot = null;
    private List<string> uploadQueue = new List<string>();
    private CoroutineRunner synchronizeRunner = new CoroutineRunner();

    private string LOCAL_ONLY_PATH = "groups/default/";
}
