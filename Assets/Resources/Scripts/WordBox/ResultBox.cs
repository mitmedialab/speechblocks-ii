using System;
using System.Collections.Generic;
using UnityEngine;

public class ResultBox : MonoBehaviour
{
    private GameObject editButton;
    private GameObject spawnedPictureBlock = null;
    private SpriteRenderer spriteRenderer = null;
    private Environment environment = null;
    private string wordSense = null;
    private List<Action> deploymentCallbacks = new List<Action>();

    void Start()
    {
        GameObject stageObject = GameObject.FindWithTag("StageObject");
        environment = stageObject.GetComponent<Environment>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        editButton = transform.Find("edit-button").gameObject;
        UpdateEditButton();
    }

    public string GetWordSense()
    {
        return wordSense;
    }

    public void AddDeploymentCallback(Action callback)
    {
        deploymentCallbacks.Add(callback);
    }

    public void OnDeploy()
    {
        foreach (Action callback in deploymentCallbacks)
        {
            try
            {
                callback();
            }
            catch (Exception e)
            {
                ExceptionUtil.OnException(e);
            }
        }
        spawnedPictureBlock = null;
        wordSense = null;
        UpdateEditButton();
    }

    public GameObject GetSpawnedPictureBlock()
    {
        return spawnedPictureBlock;
    }

    public void Refresh(string wordSense)
    {
        Clear();
        this.wordSense = wordSense;
        SpawnPictureBlock();
        UpdateEditButton();
    }

    public void Clear()
    {
        wordSense = null;
        ClearSpawnPlace();
        UpdateEditButton();
    }

    public void Edit()
    {
        AvatarPicker avatarPicker = GameObject.FindWithTag("AvatarPicker").GetComponent<AvatarPicker>();
        if (avatarPicker.IsRetracted()) { avatarPicker.Deploy(wordSense, deployInstantly: false); }
    }

    private void SpawnPictureBlock()
    {
        spawnedPictureBlock = (GameObject)Instantiate(Resources.Load("Prefabs/PictureBlock"));
        PictureBlock thePB = spawnedPictureBlock.GetComponent<PictureBlock>();
        thePB.Setup(transform.position, wordSense: wordSense, sortingLayer: "word_drawer");
        thePB.transform.SetParent(transform, false);
        float scale = thePB.transform.localScale.x;
        float targetWidth = spriteRenderer.size.x;
        scale = 0.95f * Mathf.Min(spriteRenderer.size.y * scale / thePB.GetHeight(), targetWidth * scale / thePB.GetWidth());
        thePB.transform.localScale = new Vector3(scale, scale, 1);
        thePB.transform.localPosition = new Vector3(0, 0, -0.1f);
        DeploymentMonitor deploymentMonitor = spawnedPictureBlock.AddComponent<DeploymentMonitor>();
        deploymentMonitor.AddCallback(OnDeploy);
        Logging.LogBirth(thePB.gameObject, "res-box");
        environment.GetRoboPartner().SuggestObjectsOfInterest(new List<GameObject>() { spawnedPictureBlock });
    }

    private void ClearSpawnPlace()
    {
        if (spawnedPictureBlock != null)
        {
            Logging.LogDeath(spawnedPictureBlock, "spawn-place-clear");
            Destroy(spawnedPictureBlock);
            spawnedPictureBlock = null;
        }
    }

    private void UpdateEditButton() {
        if (Vocab.IsInNameSense(wordSense))
        {
            editButton.SetActive(true);
        }
        else
        {
            editButton.SetActive(false);
        }
    }
}