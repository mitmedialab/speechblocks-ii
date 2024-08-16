using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using SimpleJSON;
using System.Linq;

public class Composition : MonoBehaviour
{
    private GameObject pictureBlockPrefab = null;
    private Environment environment;
    private string sceneID = null;
    private string[] allowedForScreenshot = new string[] { "CompositionRoot", "MainCamera", "StageObject" };
    private JSONArray emptyJSONArray = new JSONArray();

    private void Start()
    {
        pictureBlockPrefab = Resources.Load<GameObject>("Prefabs/PictureBlock");
        environment = GameObject.FindWithTag("StageObject").GetComponent<Environment>();
    }

    public string Push(bool makeSnapshot)
    {
        if (!HasSomethingToPush()) return null;
        Debug.Log("COMPOSITION PUSH");
        JSONArray content = SerializePictureBlocksHierarchy(transform);
        if (content.Count > 0)
        {
            if (null == sceneID)
            {
                sceneID = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
            }
            JSONObject sceneRecord = new JSONObject();
            sceneRecord["active"] = true;
            sceneRecord["content"] = content;
            environment.RecordScene(sceneID, sceneRecord.ToString());
            if (makeSnapshot) { CreateSceneSnapshot(environment.GetUser().GetID(), sceneID); }
            return sceneID;
        }
        return null;
    }

    public void Reset()
    {
        this.sceneID = null;
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
    }

    public static string SceneFilePath(string userID, string sceneID, string sceneVersion)
    {
        if ("" == sceneVersion) return $"{Application.persistentDataPath}/SceneThumbnails/{userID}/{sceneID}.png"; ;
        return $"{Application.persistentDataPath}/SceneThumbnails/{userID}/{sceneID}-{sceneVersion}.png";
    }

    public void CreateSceneSnapshot(string userID, string sceneID)
    {
        Camera camera = GameObject.FindWithTag("MainCamera").GetComponent<Camera>();
        List<GameObject> hidden = HideUIElements();
        int height = 400;
        int width = (int)(height * camera.aspect);
        RenderTexture renderTexture = new RenderTexture(width, height, 24);
        camera.targetTexture = renderTexture;
        Texture2D screenShot = new Texture2D(width, height, TextureFormat.RGB24, false);
        try { camera.Render(); } catch { }
        RenderTexture.active = renderTexture;
        screenShot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        camera.targetTexture = null;
        RenderTexture.active = null; // JC: added to avoid errors
        Destroy(renderTexture);
        byte[] bytes = screenShot.EncodeToPNG();
        string scenePath = SceneFilePath(userID, sceneID, environment.GetSceneVersion(sceneID));
        Directory.CreateDirectory(Path.GetDirectoryName(scenePath));
        Debug.Log($"SCENE PATH: {scenePath}");
        System.IO.File.WriteAllBytes(scenePath, bytes);
        RestoreUIElements(hidden);
    }

    public void Setup(string sceneID, JSONNode description)
    {
        Reset();
        this.sceneID = sceneID;
        DeserializePictureBlockHierarchy(transform, (JSONArray)description["content"]);
        AdjustInscriptionOrientation();
    }

    public static bool IsOnCanvas(Transform tform)
    {
        if ("CompositionRoot" == tform.tag) { return true; }
        if (null == tform.parent) { return false; }
        return IsOnCanvas(tform.parent);
    }

    public List<PictureBlock> GetTopPictureBlocks()
    {
        List<PictureBlock> pictureBlocks = new List<PictureBlock>();
        foreach (Transform canvasChild in transform)
        {
            PictureBlock pictureBlock = canvasChild.GetComponent<PictureBlock>();
            if (null != pictureBlock) { pictureBlocks.Add(pictureBlock); }
        }
        return pictureBlocks;
    }

    public List<PictureBlock> GetAllPictureBlocks()
    {
        List<PictureBlock> allPictureBlocks = new List<PictureBlock>();
        foreach (PictureBlock topPictureBlock in GetTopPictureBlocks())
        {
            FillPictureBlocksList(topPictureBlock, allPictureBlocks);
        }
        return allPictureBlocks;
    }

    private bool IsThereAnythingInTheScene()
    {
        foreach (Transform child in transform)
        {
            if (child.gameObject.activeSelf) return true;
        }
        return false;
    }

    private bool HasSomethingToPush()
    {
        if (null == sceneID) return IsThereAnythingInTheScene();
        return !SceneMatchesDescription(transform, (JSONArray)(environment.GetScene(sceneID)["content"]));
    }

    private bool SceneMatchesDescription(Transform rootTransform, JSONArray descriptions)
    {
        int children_count = 0;
        foreach (Transform child in rootTransform)
        {
            if (!child.gameObject.activeSelf) continue;
            PictureBlock pictureBlock = child.GetComponent<PictureBlock>();
            if (null == pictureBlock) continue;
            JSONNode matchingDescription = FindMatchingDescription(pictureBlock, descriptions);
            if (null == matchingDescription) return false;
            JSONNode childrenNode = matchingDescription["children"];
            JSONArray childrenArray = typeof(JSONArray).IsInstanceOfType(childrenNode) ? (JSONArray)childrenNode : emptyJSONArray;
            if (!SceneMatchesDescription(child, childrenArray)) return false;
            ++children_count;
        }
        return children_count == descriptions.Count;
    }

    private JSONNode FindMatchingDescription(PictureBlock pictureBlock, JSONArray descriptions)
    {
        for (int i = 0; i < descriptions.Count; ++i)
        {
            JSONNode pblockDescription = descriptions[i];
            if (pictureBlock.Matches(pblockDescription)) return pblockDescription;
        }
        return null;
    }

    private JSONArray SerializePictureBlocksHierarchy(Transform rootTransform)
    {
        JSONArray pictureBlocksArray = new JSONArray();
        foreach (Transform child in rootTransform)
        {
            PictureBlock pictureBlock = child.GetComponent<PictureBlock>();
            if (null != pictureBlock)
            {
                JSONNode pictureBlockDescription = pictureBlock.Serialize();
                JSONArray childrenDescription = SerializePictureBlocksHierarchy(child);
                if (childrenDescription.Count > 0)
                {
                    pictureBlockDescription["children"] = childrenDescription;
                }
                pictureBlocksArray.Add(pictureBlockDescription);
            }
        }
        return pictureBlocksArray;
    }

    private void DeserializePictureBlockHierarchy(Transform rootTransform, JSONArray description)
    {
        for (int i = 0; i < description.Count; ++i)
        {
            JSONNode pictureblockDescription = description[i];
            PictureBlock pictureBlock = Instantiate(pictureBlockPrefab).GetComponent<PictureBlock>();
            pictureBlock.Setup(rootTransform, pictureblockDescription);
            JSONNode childrenNode = pictureblockDescription["children"];
            if (null == childrenNode) continue;
            JSONArray childrenDescription = (JSONArray)childrenNode;
            if (null != childrenDescription)
            DeserializePictureBlockHierarchy(pictureBlock.transform, childrenDescription);
        }
    }

    private void AdjustInscriptionOrientation()
    {
        List<PictureBlock> topPictureBlocks = GetTopPictureBlocks();
        foreach (PictureBlock pictureBlock in topPictureBlocks)
        {
            List<GameObject> inscriptions = new List<GameObject>();
            Inscription.GatherInscriptions(pictureBlock.gameObject, inscriptions);
            foreach (GameObject inscription in inscriptions)
            {
                Vector3 lossyScale = inscription.transform.lossyScale;
                if (lossyScale.x < 0)
                {
                    Vector3 localScale = inscription.transform.localScale;
                    inscription.transform.localScale = new Vector3(-localScale.x, localScale.y, localScale.z);
                }
            }
        }
    }

    private List<GameObject> HideUIElements()
    {
        List<GameObject> rootObjects = new List<GameObject>();
        Scene scene = SceneManager.GetActiveScene();
        scene.GetRootGameObjects(rootObjects);
        rootObjects = rootObjects.Where(obj => obj.activeSelf && !allowedForScreenshot.Contains(obj.tag)).ToList();
        foreach (GameObject objToHide in rootObjects)
        {
            objToHide.SetActive(false);
        }
        return rootObjects;
    }

    private void RestoreUIElements(List<GameObject> hidden)
    {
        foreach (GameObject hiddenObj in hidden)
        {
            hiddenObj.SetActive(true);
        }
    }

    private void FillPictureBlocksList(PictureBlock root, List<PictureBlock> pictureBlockList)
    {
        pictureBlockList.Add(root);
        foreach (Transform childTform in root.transform)
        {
            PictureBlock child = childTform.GetComponent<PictureBlock>();
            if (null != child) { FillPictureBlocksList(child, pictureBlockList); }
        }
    }
}
