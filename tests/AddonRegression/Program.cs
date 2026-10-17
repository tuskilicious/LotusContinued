using Lotus.Addons;
var tests = new (string, Action)[] {
 ("Version mismatch detected without mutating client data", () => {
 var client=Info(1,"2"); var result=AddonValidation.Compare([Info(1,"1")],[client]);
 Equal(Mismatch.Version,result.Single().Mismatches); Equal(Mismatch.None,client.Mismatches);
 }),
 ("Matching versions ignore supplied mismatch flags", () => {
 var client=Info(1,"1"); client.Mismatches=Mismatch.Version;
 Equal(0,AddonValidation.Compare([Info(1,"1")],[client]).Count);
 }),
 ("Missing addons distinguished in both directions", () => {
 var result=AddonValidation.Compare([Info(1,"1")],[Info(2,"1")]);
 Equal(Mismatch.ClientMissingAddon,result[0].Mismatches); Equal(Mismatch.HostMissingAddon,result[1].Mismatches);
 }),
 ("Same display names do not hide distinct addons", () => {
 Equal(2,AddonValidation.Compare([Info(1,"1"),Info(2,"1")],[Info(1,"2"),Info(2,"2")]).Count);
 }),
 ("Repeated handshakes replace previous state", () => {
 var players=new Dictionary<byte,List<AddonInfo>>(); var input=new List<AddonInfo>{Info(1,"1")};
 AddonValidation.Remember(players,3,input); input.Clear(); Equal(1,players[3].Count);
 AddonValidation.Remember(players,3,[Info(2,"2")]); Equal((ulong)2,players[3].Single().UUID);
 }),
 ("Failed initialization is not advertised as loaded", () => {
 var loaded=new List<LotusAddon>(); var addon=new LotusAddon { Init=()=>throw new Exception("test") };
 try { AddonLifecycle.Initialize(addon,loaded); } catch(Exception) {}
 Equal(0,loaded.Count);
 }),
 ("Failed post-initialization does not prevent later addons", () => {
 bool ran=false; int failures=0;
 var bad=new LotusAddon { Post=_=>throw new Exception("test") };
 var good=new LotusAddon { Post=others=>{ Equal(false,others.Contains(bad)); ran=true; } };
 var loaded=new List<LotusAddon>{bad,good};
 AddonLifecycle.PostInitialize(loaded,(_,_)=>failures++);
 Equal(true,ran); Equal(1,failures); Equal(good,loaded.Single());
 }),
 ("Only assembly extensions accepted regardless of case", () => {
 Equal(true,AddonLifecycle.IsAssembly(new FileInfo("Cosmetics.DLL")));
 Equal(true,AddonLifecycle.IsAssembly(new FileInfo("Cosmetics.dll")));
 Equal(false,AddonLifecycle.IsAssembly(new FileInfo("readme.txt")));
 Equal(false,AddonLifecycle.IsAssembly(new FileInfo("addon.dll.config")));
 })
};
int failed=0;
foreach(var (name,test) in tests) { try { test(); Console.WriteLine("PASS "+name); } catch(Exception e) { failed++; Console.WriteLine("FAIL "+name+": "+e.Message); } }
Console.WriteLine($"{tests.Length-failed}/{tests.Length} addon checks passed.");
return failed==0?0:1;
static AddonInfo Info(ulong id,string version)=>new(){UUID=id,Name="Same name",Version=version,AssemblyShortName="Addon",AssemblyFullName="Addon, Version="+version};
static void Equal<T>(T expected,T actual) { if(!EqualityComparer<T>.Default.Equals(expected,actual)) throw new Exception($"Expected {expected}, got {actual}"); }
