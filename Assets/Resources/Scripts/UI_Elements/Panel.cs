using UnityEngine;

public abstract class Panel : MonoBehaviour
{
    public const int IS_RETRACTED = 0;
    public const int IS_IN_BETWEEN = 1;
    public const int IS_DEPLOYED = 2;

    private Vector2 hidingPlace;
    private AnimationMaster animaster;
    private int state = IS_RETRACTED;
    private int target_state = IS_IN_BETWEEN;


    // Start is called before the first frame update
    public void SetupPanel()
    {
        GameObject stageObject = GameObject.FindWithTag("StageObject");
        animaster = stageObject.GetComponent<AnimationMaster>();
        hidingPlace = transform.position;
    }

    public virtual void Update()
    {
        if (state == IS_IN_BETWEEN && null == GetComponent<Glide>())
        {
            if (target_state == IS_RETRACTED)
            {
                OnRetract();
            }
            else
            {
                OnDeploy();
            }
            state = target_state;
        }
    }

    public abstract string GetSortingLayer();

    public void Deploy(bool deployInstantly)
    {
        Debug.Log($"DEPLOY {gameObject.name} INSTANTLY: {deployInstantly}");
        if (deployInstantly)
        {
            Glide glide = gameObject.GetComponent<Glide>();
            if (null != glide) { Destroy(glide); }
            transform.position = Vector3.zero;
            OnDeploy();
            state = IS_DEPLOYED;
        }
        else
        {
            state = IS_IN_BETWEEN;
            target_state = IS_DEPLOYED;
            animaster.StartGlide(gameObject, Vector2.zero, 0.25f);
        }
    }

    public void Retract(bool retractInstantly)
    {
        Debug.Log($"RETRACT {gameObject.name} INSTANTLY: {retractInstantly}");
        if (retractInstantly)
        {
            Glide glide = gameObject.GetComponent<Glide>();
            if (null != glide) { Destroy(glide); }
            transform.position = hidingPlace;
            OnRetract();
            state = IS_RETRACTED;
        }
        else
        {
            state = IS_IN_BETWEEN;
            target_state = IS_RETRACTED;
            animaster.StartGlide(gameObject, hidingPlace, 0.25f);
        }
    }

    public virtual bool IsDeployed()
    {
        return state == IS_DEPLOYED;
    }

    public virtual bool IsRetracted()
    {
        return state == IS_RETRACTED;
    }

    public bool IsGliding()
    {
        return null != GetComponent<Glide>();
    }

    protected virtual void OnDeploy() {}

    protected virtual void OnRetract() {}
}
