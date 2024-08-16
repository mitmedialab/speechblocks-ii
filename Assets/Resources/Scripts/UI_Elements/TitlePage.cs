using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class TitlePage : Panel
{
    private string titleMappingCode = "s>s|p>p|i1>ee|tS>ch|b>b|l>l|A0>o|k>ck|s>s";
    private GameObject blockBasePrefab;
    private List<BlockBase> titleBlocks = new List<BlockBase>();
    private CoroutineRunner hopRunner = new CoroutineRunner();
    private Collider2D[] videoButtonColliders;
    private AnimationMaster anim_master;

    // Start is called before the first frame update
    public void Start()
    {
        GameObject stageObject = GameObject.FindWithTag("StageObject");
        anim_master = stageObject.GetComponent<AnimationMaster>();
        SetupPanel();
        blockBasePrefab = Resources.Load<GameObject>("Prefabs/BlockBase");
        transform.position = Vector3.zero;
        SetupTitle();
        Deploy(deployInstantly : true);
        videoButtonColliders = (new string[] { "video-ok", "video-not-ok" }).Select(elem_name => transform.Find(elem_name).GetComponent<Collider2D>()).ToArray();
        DeactivateVideoButtons(instant: true);
    }

    public override string GetSortingLayer()
    {
        return "title_page";
    }

    public override void Update()
    {
        base.Update();
        hopRunner.Update();
    }

    public void ActivateVideoButtons()
    {
        foreach (Collider2D videoButtonCollider in videoButtonColliders)
        {
            videoButtonCollider.enabled  = true;
            videoButtonCollider.gameObject.SetActive(true);
            videoButtonCollider.gameObject.transform.localScale = Vector3.forward;
            anim_master.StartScale(videoButtonCollider.gameObject, targetScale: 1f, duration: 0.5f);
        }
    }

    public void DeactivateVideoButtons(bool instant)
    {
        foreach (Collider2D videoButtonCollider in videoButtonColliders)
        {
            videoButtonCollider.enabled = false;
            if (instant)
            {
                videoButtonCollider.gameObject.SetActive(false);
            }
            else
            {
                anim_master.StartScale(videoButtonCollider.gameObject, targetScale: 0f, duration: 0.5f);
            }
        }
    }

    private void SetupTitle()
    {
        List<PGPair> titleMapping = PGPair.ParseMapping(titleMappingCode);
        foreach (PGPair titlePG in titleMapping)
        {
            BlockBase titleBlock = Instantiate(blockBasePrefab).GetComponent<BlockBase>();
            titleBlock.Setup(titlePG, "title");
            ZSorting.SetSortingLayer(titleBlock.gameObject, "overlay");
            titleBlocks.Add(titleBlock);
        }
        float width = titleBlocks.Select(block => block.GetWidth()).Sum();
        float scale = 14 / width;
        float offset = -7;
        foreach (BlockBase titleBlock in titleBlocks)
        {
            Transform blockTransform = titleBlock.transform;
            blockTransform.SetParent(transform, false);
            blockTransform.localScale = new Vector3(scale, scale, 1);
            blockTransform.localPosition = new Vector3(offset + 0.5f * scale * titleBlock.GetWidth(), 0, -1);
            offset += scale * titleBlock.GetWidth();
        }
    }

    private IEnumerator HopBlocks()
    {
        while (true)
        {
            foreach (BlockBase block in titleBlocks)
            {
                yield return HopBlock(block.gameObject, 0.5f, 0.35f);
            }
        }
    }

    private IEnumerator HopBlock(GameObject block, float height, float duration)
    {
        double t0 = TimeKeeper.time;
        float x0 = block.transform.localPosition.x;
        float z0 = block.transform.localPosition.z;
        while (true)
        {
            float t = (float)(TimeKeeper.time - t0) / duration;
            if (t > 1) { t = 1; }
            block.transform.localPosition = new Vector3(x0, height * (1 - Mathf.Pow(2 * t - 1, 2)), z0);
            yield return null;
            if (1 == t) yield break;
        }
    }

    protected override void OnDeploy()
    {
        hopRunner.SetCoroutine(HopBlocks());
    }

    protected override void OnRetract()
    {
        hopRunner.SetCoroutine(null);
        foreach (BlockBase block in titleBlocks)
        {
            Vector3 blockPosition = block.transform.localPosition;
            block.transform.localPosition = new Vector3(blockPosition.x, 0, blockPosition.z);
        }
    }
}
