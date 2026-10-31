using System;
using System.Collections.Generic;
using System.IO;

namespace Lotus.Addons;

internal static class AddonLifecycle
{
    internal static bool IsAssembly(FileInfo file) => string.Equals(file.Extension, ".dll", StringComparison.OrdinalIgnoreCase);

    internal static void Initialize(LotusAddon addon, List<LotusAddon> loaded)
    {
        addon.Initialize();
        loaded.Add(addon);
    }

    internal static void PostInitialize(List<LotusAddon> loaded, Action<LotusAddon, Exception> failed)
    {
        foreach (var addon in loaded.ToArray())
        {
            try { addon.PostInitialize(new List<LotusAddon>(loaded)); }
            catch (Exception exception)
            {
                loaded.Remove(addon);
                failed(addon, exception);
            }
        }
    }
}
