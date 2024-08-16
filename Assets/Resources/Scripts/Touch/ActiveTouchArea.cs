using System;
using System.Collections.Generic;
using UnityEngine;

public class ActiveTouchArea : MonoBehaviour
{
    private List<GameObject> activeZoneHolders = new List<GameObject>();
    
    public void Setup(GameObject activeZoneHolder) {
        activeZoneHolders.Clear();
        activeZoneHolders.Add(activeZoneHolder);
    }

    public void Setup(IEnumerable<GameObject> activeZoneHolders)
    {
        this.activeZoneHolders.Clear();
        this.activeZoneHolders.AddRange(activeZoneHolders);
    }

    public bool PointIsWithinActiveZone(Vector2 pointWorld)
    {
        foreach (GameObject activeZoneHolder in activeZoneHolders)
        {
            try
            {
                SpriteRenderer activeZoneRenderer = activeZoneHolder.GetComponent<SpriteRenderer>();
                if (null == activeZoneRenderer) continue;
                Sprite activeZoneSprite = activeZoneRenderer.sprite;
                if (null == activeZoneSprite) continue;
                Texture2D activeZoneMatrix = activeZoneSprite.texture;
                if (null == activeZoneMatrix) continue;
                Vector3 pointLocal = activeZoneHolder.transform.worldToLocalMatrix.MultiplyPoint((Vector3)pointWorld);
                float xPos = activeZoneSprite.pixelsPerUnit * pointLocal.x + activeZoneSprite.pivot.x;
                float yPos = activeZoneSprite.pixelsPerUnit * pointLocal.y + activeZoneSprite.pivot.y;
                if (xPos < 0 || xPos >= activeZoneMatrix.width) continue;
                if (yPos < 0 || yPos >= activeZoneMatrix.height) continue;
                Color colorAtPixel = activeZoneMatrix.GetPixel((int)xPos, (int)yPos);
                if (colorAtPixel.a > 0.01f) return true;
            }
            catch (Exception e)
            {
                ExceptionUtil.OnException(e);
            }
        }
        return false;
    }
}
