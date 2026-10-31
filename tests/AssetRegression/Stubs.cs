namespace Lotus.Extensions { }
namespace Lotus.Logging { }
namespace Lotus.Utilities
{
    public static class AssetLoader
    {
        public static UnityEngine.AssetBundle LoadAssetBundle(string path) => new();
    }
}
namespace UnityEngine
{
    public class Object { public static int DestroyCount; public static void Destroy(Object value) => DestroyCount++; }
    public record struct Vector4(float X, float Y, float Z, float W);
    public record struct Vector2(float X, float Y);
    public record struct Rect(float x, float y, float width, float height);
    public enum RenderTextureFormat { ARGB32 }
    public enum RenderTextureReadWrite { Linear, sRGB }
    public enum TextureFormat { ARGB32 }
    public enum SpriteMeshType { FullRect }
    public class Texture2D : Object
    {
        public int width, height, requestedMipmapLevel;
        public static bool FailRead;
        public Texture2D(int width, int height, TextureFormat format, bool mipmap, bool linear) { this.width=width; this.height=height; }
        public void ReadPixels(Rect rect, int x, int y) { if (FailRead) throw new InvalidOperationException("read failed"); }
        public void Apply(bool mipmap) { }
    }
    public class RenderTexture : Object
    {
        public static RenderTexture? active;
        public static int Acquired, Released;
        public static RenderTexture GetTemporary(int width,int height,int depth,RenderTextureFormat format,RenderTextureReadWrite mode) { Acquired++; return new(); }
        public static void ReleaseTemporary(RenderTexture texture) => Released++;
    }
    public static class Graphics { public static int Copies; public static void Blit(Texture2D texture,RenderTexture target) => Copies++; }
    public class Sprite : Object
    {
        public Texture2D texture = new(16,16,TextureFormat.ARGB32,true,false);
        public Rect rect = new(0,0,16,16);
        public static Sprite Create(Texture2D texture,Rect rect,Vector2 pivot,float ppu,uint extrude,SpriteMeshType mesh,Vector4 border) => new() {texture=texture,rect=rect};
    }
    public class AssetBundle : Object
    {
        public static string LastPath="";
        public T? LoadAsset<T>(string path) where T : Object { LastPath=path; return path.Contains("missing") ? null : (T)(Object)new Sprite(); }
    }
}
