using System.Linq;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using System.IO;

public class SpriteUtil {
    public static Color32[] PALETTE =
    {
        new Color32(51, 153, 255, 255),
        new Color32(239, 253, 95, 255),
        new Color32(57, 255, 20, 95),
        new Color32(0xb6, 0x5f, 0xcf, 255),
        new Color32(0x67, 0x03, 0x2f, 255)
    };

    public static Sprite LoadInternalSprite(string spriteAssetPath, float pixelsPerUnit)
    {
        Texture2D spriteTexture = Resources.Load<Texture2D>(spriteAssetPath);
        return CreateSprite(spriteAssetPath, spriteTexture, pixelsPerUnit);
    }

    public static Sprite LoadInternalSpriteOfSize(string spriteAssetPath, float maxWidth, float maxHeight) {
        Texture2D spriteTexture = Resources.Load<Texture2D>(spriteAssetPath);
        return CreateSprite(spriteAssetPath, spriteTexture, ComputePPU(spriteTexture, maxWidth, maxHeight));
    }

    public static Sprite LoadExternalSpriteOfSize(string spritePath, float maxWidth, float maxHeight)
    {
        Texture2D texture = LoadTexture(spritePath);
        return CreateSprite(spritePath, texture, ComputePPU(texture, maxWidth, maxHeight));
    }

    public static Sprite LoadExternalSprite(string spritePath, float pixelsPerUnit)
    {
        return CreateSprite(spritePath, LoadTexture(spritePath), pixelsPerUnit);
    }

    public static Color32 GetTextureColor(Texture2D spriteTexture)
    {
        Color32[] pixels = spriteTexture.GetPixels32();
        foreach(Color32 pixel in pixels)
        {
            if(pixel.a != 0)
            {
                return pixel;
            }
        }
        return pixels[0];
    }

    public static Color32 PickColor(string word)
    {
        if (0 == word.Length) return PALETTE[0];
        return PALETTE[((int)char.ToLower(word[0]) - (int)'a') % PALETTE.Length];
    }

    private static float ComputePPU(Texture2D texture, float maxWidth, float maxHeight)
    {
        return Mathf.Max(texture.width / maxWidth, texture.height / maxHeight);
    }

    private static Sprite CreateSprite(string path, Texture2D spriteTexture, float ppu)
    {
        Sprite sprite = Sprite.Create(spriteTexture,
                             new Rect(0, 0, spriteTexture.width, spriteTexture.height),
                             new Vector2(0.5f, 0.5f),
                             ppu);
        sprite.name = path;
        return sprite;
    }

    private static Texture2D LoadTexture(string path)
    {
        Texture2D tex2D;
        byte[] data;

        if (File.Exists(path))
        {
            data = File.ReadAllBytes(path);
            tex2D = new Texture2D(2, 2);           // Create new "empty" texture
            if (tex2D.LoadImage(data))           // Load the imagedata into the texture (size is set automatically)
                return tex2D;                 // If data = readable -> return texture
        }
        return null;                     // Return null if load failed
    }
}