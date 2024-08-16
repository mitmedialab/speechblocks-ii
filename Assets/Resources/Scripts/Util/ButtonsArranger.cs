using UnityEngine;

public class ButtonsArranger
{
    private float[] originAlongDim;
    private float[] areaDims;
    private float[] buttonDims;
    private float[] spaceAlongDim = new float[2];
    private int[] nAlongDim = new int[2];
    private int[] dirAlongDim = { 1, -1 };
    private int maxButtons;

    public ButtonsArranger(float x0, float y0, float areaWidth, float areaHeight, float buttonWidth, float buttonHeight)
    {
        originAlongDim = new float[2] { x0, y0 };
        areaDims = new float[2] { areaWidth, areaHeight };
        buttonDims = new float[2] { buttonWidth, buttonHeight };
        for (int i = 0; i < 2; ++i)
        {
            nAlongDim[i] = (int)Mathf.Floor(areaDims[i] / buttonDims[i]);
            spaceAlongDim[i] = (areaDims[i] - nAlongDim[i] * buttonDims[i]) / (nAlongDim[i] + 1);
        }
        maxButtons = nAlongDim[0] * nAlongDim[1];
    }

    public int MaxButtons()
    {
        return maxButtons;
    }

    public Vector2 GetButtonPos(int buttonN)
    {
        return new Vector2(GetButtonX(buttonN), GetButtonY(buttonN));
    }

    public Vector3 GetButtonPos3D(int buttonN)
    {
        return new Vector3(GetButtonX(buttonN), GetButtonY(buttonN), -1f);
    }

    public float GetButtonX(int buttonN)
    {
        int colNum = buttonN % nAlongDim[0];
        return ButtonPosAlongDim(0, colNum);
    }

    public float GetButtonY(int buttonN)
    {
        int rowNum = buttonN / nAlongDim[0];
        return ButtonPosAlongDim(1, rowNum);
    }

    public static float GetButtonWidth(GameObject buttonPrefab)
    {
        return buttonPrefab.transform.localScale.x * buttonPrefab.GetComponent<BoxCollider2D>().size.x;
    }

    public static float GetButtonHeight(GameObject buttonPrefab)
    {
        return buttonPrefab.transform.localScale.y * buttonPrefab.GetComponent<BoxCollider2D>().size.y;
    }

    private float ButtonPosAlongDim(int dim, int buttonNAlongDim)
    {
        return originAlongDim[dim] + dirAlongDim[dim] * (-0.5f * areaDims[dim] + spaceAlongDim[dim] * (buttonNAlongDim + 1) + buttonDims[dim] * (buttonNAlongDim + 0.5f));
    }
}
