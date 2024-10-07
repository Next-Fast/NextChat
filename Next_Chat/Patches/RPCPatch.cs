using System;
using System.Linq;
using HarmonyLib;
using Hazel;
using InnerNet;
using Next_Chat.Core;

namespace Next_Chat.Patches;

[Harmony]
public static class RPCPatch
{
    public static readonly byte[] RpcIds = Enum.GetValues<RPCFlag>().Select(n => (byte)n).ToArray();
    public static Dictionary<RPCFlag, Action<MessageReader>> rpcHandlers = new();
        
    [HarmonyPatch(typeof(InnerNetClient._HandleGameDataInner_d__41), nameof(InnerNetClient._HandleGameDataInner_d__41.MoveNext))]
    public static bool Prefix(InnerNetClient._HandleGameDataInner_d__41 __instance, ref bool __result)
    {
        if (__instance.reader.Tag != 2 || __instance.__1__state != 0) return true;
        var reader = MessageReader.Get(__instance.reader);
        _ = reader.ReadPackedUInt32();
        var rpc = reader.ReadByte();
        if (!RpcIds.Contains(rpc))
        {
            reader.Recycle();
            return true;
        }

        if (rpcHandlers.TryGetValue((RPCFlag)rpc, out var action))
            action.Invoke(reader);
        
        reader.Recycle();
        __result = false;
        return false;
    }

    public static byte ToByte(this RPCFlag flag) => (byte)flag;

    public static void SendRpcToAll(this RPCFlag flag, SendOption option, Action<MessageWriter> write, uint netId = 25)
    => SendRpcToAll(flag.ToByte(), option, write, netId);
    
    public static void SendRpcToPlayer(this RPCFlag flag, SendOption option, Action<MessageWriter> write, int playerId, uint netId = 25)
    => SendRpcToPlayer(flag.ToByte(), option, write, playerId, netId);

    public static void SendRpcToAll(byte rpc, SendOption option, Action<MessageWriter> write, uint netId = 25)
    {
        var writer = MessageWriter.Get(option);
        writer.StartMessage(5);
        writer.Write(AmongUsClient.Instance.GameId);
        writer.StartMessage(2);
        writer.WritePacked(netId);
        writer.Write(rpc);
        write(writer);
        writer.EndMessage();
        writer.EndMessage();
        AmongUsClient.Instance.SendOrDisconnect(writer);
        writer.Recycle();
    }
    
    public static void SendRpcToPlayer(byte rpc, SendOption option, Action<MessageWriter> write, int playerId, uint netId = 25)
    {
        var writer = MessageWriter.Get(option);
        writer.StartMessage(6);
        writer.Write(AmongUsClient.Instance.GameId);
        writer.WritePacked(playerId);
        writer.StartMessage(2);
        writer.WritePacked(netId);
        writer.Write(rpc);
        write(writer);
        writer.EndMessage();
        writer.EndMessage();
        AmongUsClient.Instance.SendOrDisconnect(writer);
        writer.Recycle();
    }
}