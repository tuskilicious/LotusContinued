using System;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using Lotus.Extensions;
using Lotus.Logging;
using Lotus.Utilities;
using UnityEngine;

namespace Lotus.GUI;

public class LotusAssets
{
    private static AssetBundle? _assetBundle;
    private static bool _loaded = false;
    private static readonly Dictionary<(string Path, float PixelsPerUnit, bool Linear, int MipMapLevel, Vector4 Border), Sprite> Sprites = new();

    public static AssetBundle Bundle
    {
        get
        {
            if (_loaded) return _assetBundle!;
            _assetBundle = AssetLoader.LoadAssetBundle("Lotus.assets.projectlotus_bundle-" + (OperatingSystem.IsAndroid() ? "android" : "win"));
            _loaded = _assetBundle != null;
            return _assetBundle;
        }
    }

    public static T LoadAsset<T>(string path) where T : UnityEngine.Object
    {
        return Bundle.LoadAsset<T>(path.ToLowerInvariant())!;
    }

    public static bool TryLoadAsset<T>(string path, [MaybeNullWhen(false)] out T asset) where T : UnityEngine.Object
    {
        T? targetAsset = asset = LoadAsset<T>(path);
        return targetAsset != null;
    }

    public static Sprite LoadSprite(string path, float pixelsPerUnit = 100f, bool linear = false, int mipMapLevel = 0, Vector4 borderInPixels = default(Vector4))
    {
        path = path.ToLowerInvariant();
        var key = (path, pixelsPerUnit, linear, mipMapLevel, borderInPixels);
        if (Sprites.TryGetValue(key, out Sprite? cached) && cached != null) return cached;
        Sprite? originalSprite = Bundle.LoadAsset<Sprite>(path);
        if (originalSprite == null) return null!;

        Texture2D originalTexture = originalSprite.texture;
        Rect rect = originalSprite.rect;

        RenderTexture rt = RenderTexture.GetTemporary((int)rect.width, (int)rect.height, 0, RenderTextureFormat.ARGB32, linear ? RenderTextureReadWrite.Linear : RenderTextureReadWrite.sRGB);
        RenderTexture previous = RenderTexture.active;
        Texture2D? texture = null;
        try
        {
            Graphics.Blit(originalTexture, rt);
            RenderTexture.active = rt;
            texture = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.ARGB32, true, linear);
            texture.ReadPixels(new Rect(0, 0, rect.width, rect.height), 0, 0);
            texture.Apply(true);
        }
        catch
        {
            if (texture != null) UnityEngine.Object.Destroy(texture);
            throw;
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
        }

        Sprite sprite = Sprite.Create(texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            pixelsPerUnit,
            0,
            SpriteMeshType.FullRect,
            borderInPixels);
        sprite.texture.requestedMipmapLevel = mipMapLevel;
        Sprites[key] = sprite;

        return sprite;
    }

    public static bool TryLoadSprite(string path, [MaybeNullWhen(false)] out Sprite sprite, float pixelsPerUnit = 100f, bool linear = false, int mipMapLevel = 0, Vector4 borderInPixels = default(Vector4))
    {
        Sprite outputSprite = sprite = LoadSprite(path, pixelsPerUnit, linear, mipMapLevel, borderInPixels);
        return outputSprite != null!;
    }
}
