using System.Collections.Generic;
using System.Linq;

namespace Lotus.Addons;

internal static class AddonValidation
{
    internal static void Remember(Dictionary<byte, List<AddonInfo>> players, byte player, List<AddonInfo> addons)
        => players[player] = new List<AddonInfo>(addons);

    internal static List<AddonInfo> Compare(List<AddonInfo> host, List<AddonInfo> client)
    {
        var result = new List<AddonInfo>();
        foreach (var local in host.DistinctBy(a => a.UUID))
        {
            var remote = client.FirstOrDefault(a => a.UUID == local.UUID);
            if (remote == null) result.Add(Copy(local, Mismatch.ClientMissingAddon));
            else if (remote.Version != local.Version) result.Add(Copy(remote, Mismatch.Version));
        }
        foreach (var remote in client.DistinctBy(a => a.UUID))
            if (!host.Any(a => a.UUID == remote.UUID)) result.Add(Copy(remote, Mismatch.HostMissingAddon));
        return result;
    }

    private static AddonInfo Copy(AddonInfo info, Mismatch mismatch) => new()
    {
        UUID = info.UUID, Name = info.Name, Version = info.Version,
        AssemblyShortName = info.AssemblyShortName, AssemblyFullName = info.AssemblyFullName,
        Mismatches = mismatch
    };
}
