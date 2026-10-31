namespace Hazel {
 public class MessageReader { public ulong ReadUInt64()=>0; public string ReadString()=>""; public int ReadInt32()=>0; }
 public class MessageWriter { public void Write(object value) {} }
}
namespace VentLib.Networking.Interfaces { public interface IRpcSendable<T> { T Read(Hazel.MessageReader reader); void Write(Hazel.MessageWriter writer); } }
namespace Lotus.Addons {
 public class VersionStub { public string ToSimpleName()=>"1"; }
 public class LotusAddon {
 public ulong UUID; public string Name="Test"; public VersionStub Version=new(); public System.Reflection.Assembly BundledAssembly=typeof(LotusAddon).Assembly;
 public Action Init=()=>{}; public Action<List<LotusAddon>> Post=_=>{};
 public void Initialize()=>Init(); public void PostInitialize(List<LotusAddon> addons)=>Post(addons);
 }
}
