using UnityEngine;
using SimpleJSON;
using System.IO;

public class GalleryButton : MonoBehaviour, ITappable, IDetailedLogging
{
    private Environment environment;
    private string sceneID;
    private string sceneVersion = "";
    private Vector3 targetPosition;
    private Draggable draggable;

    public void Setup(string sceneID)
    {
        GameObject stageObject = GameObject.FindWithTag("StageObject");
        environment = stageObject.GetComponent<Environment>();
        draggable = GetComponent<Draggable>();
        this.sceneID = sceneID;
        if (null == sceneID)
        {
            Destroy(draggable);
            draggable = null;
        }
        else
        {
            foreach (Transform child in transform) { Destroy(child.gameObject); }
            SpriteRenderer spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingLayerName = "gallery";
            UpdateImage();
        }
    }

    public object[] GetLogDetails()
    {
        return new object[]{ "sceneID", sceneID };
    }

    public void UpdateImage()
    {
        string userID = environment.GetUser().GetID();
        string currentVersion = environment.GetSceneVersion(sceneID);
        if (currentVersion == sceneVersion) return;
        string oldFilePath = Composition.SceneFilePath(userID, sceneID, sceneVersion);
        sceneVersion = currentVersion;
        if (File.Exists(oldFilePath)) { File.Delete(oldFilePath); }
        SpriteRenderer spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        Vector2 colliderSize = GetComponent<BoxCollider2D>().size;
        string sceneFilePath = Composition.SceneFilePath(userID, sceneID, currentVersion);
        if (!File.Exists(sceneFilePath))
        {
            Composition composition = GameObject.FindWithTag("CompositionRoot").GetComponent<Composition>();
            composition.Setup(sceneID, environment.GetScene(sceneID));
            composition.CreateSceneSnapshot(userID, sceneID);
            composition.Reset();
        }
        Debug.Log($"LOADING SCENE IMAGE FROM PATH: {sceneFilePath}");
        spriteRenderer.sprite = SpriteUtil.LoadExternalSpriteOfSize(sceneFilePath, colliderSize.x, colliderSize.y);
    }

    public string GetSceneID()
    {
        return sceneID;
    }

    public void SetTargetPosition(Vector3 targetPosition)
    {
        this.targetPosition = targetPosition;
    }

    public Vector3 GetTargetPosition()
    {
        return this.targetPosition;
    }

    public void Update()
    {
        if (null != draggable && draggable.IsTouched()) return;
        LocalGlide glide = GetComponent<LocalGlide>();
        if (null != glide && glide.GetEnd() != (Vector2)targetPosition)
        {
            Destroy(glide);
            glide = null;
        }
        if (targetPosition != transform.localPosition)
        {
            if (null == glide)
            {
                GameObject.FindWithTag("StageObject").GetComponent<AnimationMaster>().StartLocalGlide(gameObject, targetPosition, 0.5f);
            }
        }
    }

    public void OnTap(TouchInfo touchInfo)
    {
        Gallery gallery = GameObject.FindWithTag("Gallery").GetComponent<Gallery>();
        if (gallery.IsGliding()) return;
        Composition composition = GameObject.FindWithTag("CompositionRoot").GetComponent<Composition>();
        if (null == sceneID)
        {
            composition.Reset();
        }
        else
        {
            GameObject.FindWithTag("AssociationsPanel")?.GetComponent<AssociationsPanel>()?.Retract();
            composition.Setup(sceneID, environment.GetScene(sceneID));
        }
        gallery.Retract(retractInstantly: false);
    }
}
