using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using Lotus.RPC;
using Lotus.Extensions;
using Lotus.Roles;
using VentLib;
using VentLib.Networking.RPC.Attributes;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;
using Lotus.API.Player;
using VentLib.Networking.RPC;
using Rewired;
using UnityEngine;

namespace Lotus.Addons;

public class AddonManager
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(AddonManager));

    public static LogLevel AddonLL = LogLevel.Info.Similar("ADDON", ConsoleColor.Magenta);
    public static List<LotusAddon> Addons = new();
    public static Dictionary<byte, List<AddonInfo>> PlayerAddons = new();

    public static readonly List<string> FailedAddons = [];

    internal static void ImportAddons()
    {
        DirectoryInfo addonDirectory = new(Path.Combine(Vents.BasePath,
            OperatingSystem.IsAndroid() ? AssemblyUtils.GetAssemblyRefName(Assembly.GetExecutingAssembly()) : string.Empty,
            "addons")
        );
        if (!addonDirectory.Exists)
            addonDirectory.Create();
        #if RELEASE
            if (OperatingSystem.IsAndroid()) return; // xtra wants to disable this on android for now.
        #endif
        log.Info($"Loading addons from: {addonDirectory.FullName}");
        addonDirectory.EnumerateFiles().Where(AddonLifecycle.IsAssembly).Do(LoadAddon);
        AddonLifecycle.PostInitialize(Addons, (addon, exception) =>
        {
            log.Exception($"Post-initialization failed for addon {addon.Name}.", exception);
            FailedAddons.Add(addon.Name + ".");
        });
    }

    private static void LoadAddon(FileInfo file)
    {
        try
        {
            Assembly assembly = Assembly.LoadFile(file.FullName);
            Type? lotusType = assembly.GetTypes().FirstOrDefault(t => t.IsAssignableTo(typeof(LotusAddon)) && !t.IsAbstract && !t.ContainsGenericParameters);
            if (lotusType == null)
                throw new ConstraintException($"Lotus Addons requires ONE class file that extends {nameof(LotusAddon)}");
            LotusAddon addon = (LotusAddon)AccessTools.Constructor(lotusType).Invoke([]);

            log.Log(AddonLL, $"Loading Addon [{addon.Name} {addon.Version}]", "AddonManager");
            Vents.Register(assembly);

            AddonLifecycle.Initialize(addon, Addons);
        }
        catch (Exception e)
        {
            log.Exception($"Error occured while loading addon. Addon File Name: {file.Name}", e);
            log.Exception(e);
            FailedAddons.Add(file.Name + ".");
        }
    }

    internal static void SendAddonsToHost()
    {
        if (AmongUsClient.Instance.AmHost) return;
        List<AddonInfo> addonsToSend = Addons.Select(AddonInfo.From).ToList();
        Vents.FindRPC((uint)ModCalls.ReceiveAddons)!.Send(null, addonsToSend);
    }

    [ModRPC((uint)ModCalls.ReceiveAddons, RpcActors.NonHosts, RpcActors.Host)]
    public static void VerifyClientAddons(List<AddonInfo> receivedAddons)
    {
        List<AddonInfo> hostInfo = Addons.Select(AddonInfo.From).ToList();
        PlayerControl? senderControl = Vents.GetLastSender((uint)ModCalls.ReceiveAddons);
        if (senderControl == null) return;
        int senderId = senderControl.GetClientId();
        log.Debug($"Last Sender: {senderId}");
        AddonValidation.Remember(PlayerAddons, senderControl.PlayerId, receivedAddons);
        List<AddonInfo> mismatchInfo = AddonValidation.Compare(hostInfo, receivedAddons);
        foreach (var mismatch in mismatchInfo)
        {
            var local = Addons.FirstOrDefault(a => a.UUID == mismatch.UUID);
            if (local != null) Vents.BlockClient(local.BundledAssembly, senderId);
            if (mismatch.Mismatches != Mismatch.ClientMissingAddon)
                Vents.FindRPC(1017)!.Send([senderId], mismatch.AssemblyFullName, (int)VentControlFlag.Denied);
        }
        ReceiveAddonVerification(mismatchInfo, senderId);
    }

    [ModRPC((uint)ModCalls.ReceiveAddons, RpcActors.Host, RpcActors.NonHosts, MethodInvocation.ExecuteAfter)]
    public static void ReceiveAddonVerification(List<AddonInfo> mismatchedAddons, int senderId)
    {
        if (mismatchedAddons.Count == 0) return;
        PlayerControl? senderControl = Players.GetAllPlayers().FirstOrDefault(p => p.GetClientId() == senderId);
        string clientName = senderControl == null ? "(could not find sender)" : (senderControl.PlayerId == PlayerControl.LocalPlayer.PlayerId ? "this client" : senderControl.name);
        log.Exception($"VerifyAddons - Error Validating Addons. All CustomRPCs on the afffected addons between the host and {clientName} have been disabled.");
        log.Exception(" -=-=-=-=-=-=-=-=-=[Errored Addons]=-=-=-=-=-=-=-=-=-");
        string rejectReason = mismatchedAddons.Where(info => info.Mismatches is not Mismatch.None).Select(addonInfo => addonInfo.Mismatches
             switch
        {
            Mismatch.Version => $" {addonInfo.Name}:{addonInfo.Version} => Local version is not compatible with the host version of the addon",
            Mismatch.ClientMissingAddon => $" {addonInfo.Name}:{addonInfo.Version} => Client Missing Addon ",
            Mismatch.HostMissingAddon => $" {addonInfo.Name}:{addonInfo.Version} => Host Missing Addon ",
            _ => throw new ArgumentOutOfRangeException()
        }).StrJoin();
        log.Exception("VerifyAddons - " + rejectReason);
        if (clientName != "this client")
        {
            // block rpcs from this player
            mismatchedAddons.ForEach(a =>
            {
                LotusAddon? myAddon = Addons.FirstOrDefault(ma => ma.UUID == a.UUID);
                if (myAddon == null) return;
                Vents.BlockClient(myAddon.BundledAssembly, senderId);
            });
            return;
        }
        mismatchedAddons.ForEach(a =>
        {
            if (a.Mismatches.HasFlag(Mismatch.ClientMissingAddon))
            {
                // disable rpc from an addon we dont have
                // theres nothing we need to do here as if they have unique numbers as ids for their rpcs, then
                // our client wont even be able to find the rpc and run it. essentially acting like it never existed.
            }
            // disabling other rpcs are easy using the builtin function
            else VentRPC.SetControlFlag(a.AssemblyFullName, (int)VentControlFlag.Denied);
        });
    }

    public static List<AddonInfo> GetPlayerAddons(byte playerId) => PlayerAddons.GetOrCompute(playerId, () => new List<AddonInfo>());
}