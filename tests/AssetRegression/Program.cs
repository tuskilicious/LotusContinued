using System.Globalization;
using Lotus.GUI;
using Lotus.Managers.Announcements.Helpers;
using UnityEngine;
using YamlDotNet.Serialization;
var cases = new (string, Action)[] {
("Repeated sprites reuse the GPU copy", () => { int before=Graphics.Copies; var a=LotusAssets.LoadSprite("repeat.png"); var b=LotusAssets.LoadSprite("repeat.png"); Check(ReferenceEquals(a,b)); Check(Graphics.Copies-before==1); }),
("All rendering options have distinct cache entries", () => { var a=LotusAssets.LoadSprite("keys.png"); Check(!ReferenceEquals(a,LotusAssets.LoadSprite("keys.png",200))); Check(!ReferenceEquals(a,LotusAssets.LoadSprite("keys.png",linear:true))); Check(!ReferenceEquals(a,LotusAssets.LoadSprite("keys.png",mipMapLevel:1))); Check(!ReferenceEquals(a,LotusAssets.LoadSprite("keys.png",borderInPixels:new(1,2,3,4)))); }),
("Failed GPU reads restore state and release textures", () => { var previous=RenderTexture.active=new(); int releases=RenderTexture.Released, destroys=UnityEngine.Object.DestroyCount; Texture2D.FailRead=true; try { LotusAssets.LoadSprite("failure.png"); throw new Exception("Expected failure"); } catch(InvalidOperationException) {} finally { Texture2D.FailRead=false; } Check(ReferenceEquals(previous,RenderTexture.active)); Check(RenderTexture.Released==releases+1); Check(UnityEngine.Object.DestroyCount==destroys+1); Check(LotusAssets.LoadSprite("failure.png")!=null); }),
("Asset paths ignore the OS culture", () => { var original=CultureInfo.CurrentCulture; try { CultureInfo.CurrentCulture=new("tr-TR"); LotusAssets.LoadAsset<Sprite>("ICONS/ICON.PNG"); Check(AssetBundle.LastPath=="icons/icon.png"); } finally { CultureInfo.CurrentCulture=original; } }),
("Missing sprites remain optional", () => Check(!LotusAssets.TryLoadSprite("missing.png",out _))),
("YAML dates round trip across calendars", () => { var original=CultureInfo.CurrentCulture; try { foreach(var culture in new[]{"en-US","th-TH","ar-SA"}) { CultureInfo.CurrentCulture=new(culture); var serializer=new SerializerBuilder().WithTypeConverter(new DateOnlyYamlConverter()).Build(); var parser=new DeserializerBuilder().WithTypeConverter(new DateOnlyYamlConverter()).Build(); var date=new DateOnly(2026,10,17); string yaml=serializer.Serialize(date); Check(yaml.Contains("2026-10-17")); Check(parser.Deserialize<DateOnly>(yaml)==date); } } finally { CultureInfo.CurrentCulture=original; } })
};
int failures=0;
foreach(var (name, test) in cases) { try { test(); Console.WriteLine("PASS "+name); } catch(Exception e) { failures++; Console.WriteLine("FAIL "+name+": "+e.Message); } }
Console.WriteLine($"{cases.Length-failures}/{cases.Length} checks passed"); return failures==0?0:1;
static void Check(bool value) { if(!value) throw new Exception("Assertion failed"); }
