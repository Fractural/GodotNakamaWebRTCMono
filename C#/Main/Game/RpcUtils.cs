using Godot;
using System;

namespace NakamaWebRTCDemo
{

    [Flags]
    public enum RpcType
    {
        // We assume all Rpcs are ran on the network
        Local = 1,      // Run locally
        Master = 2,     // Run online if node is master
        Puppet = 4,     // Run online if node is puppet (not master)
        Server = 8,     // Run online if current client is the server.
        Unreliable = 16, // Run through unreliable (reliable by default)
    }

    public static class RpcUtils
    {
        public static object TryRpcId(this Node node, int id, string name, params object[] args)
        {
            return node.TryRpcId(RpcType.Local, id, name, args);
        }

        public static object TryRpcId(this Node node, RpcType type, int id, string name, params object[] args)
        {
            if (GameState.Global.OnlinePlay)
            {
                // If we have both master and puppet, then they cancel each other out
                if (!(type.HasFlag(RpcType.Master) && type.HasFlag(RpcType.Puppet)))
                {
                    if (type.HasFlag(RpcType.Master) && !node.IsNetworkMaster())
                        return null;
                    if (type.HasFlag(RpcType.Puppet) && node.IsNetworkMaster())
                        return null;
                }
                if (type.HasFlag(RpcType.Server) && !node.GetTree().IsNetworkServer())
                    return null;
                if (type.HasFlag(RpcType.Unreliable))
                    return node.RpcUnreliableId(id, name, args);
                return node.RpcId(id, name, args);
            }
            else
            {
                if (type.HasFlag(RpcType.Local))
                    return node.Call(name, args);
            }
            return null;
        }

        public static object TryRpc(this Node node, string name, params object[] args)
        {
            return node.TryRpc(RpcType.Local, name, args);
        }

        public static object TryRpc(this Node node, RpcType type, string name, params object[] args)
        {
            if (GameState.Global.OnlinePlay)
            {
                // If we have both master and puppet, then they cancel each other out
                if (!(type.HasFlag(RpcType.Master) && type.HasFlag(RpcType.Puppet)))
                {
                    if (type.HasFlag(RpcType.Master) && !node.IsNetworkMaster())
                        return null;
                    if (type.HasFlag(RpcType.Puppet) && node.IsNetworkMaster())
                        return null;
                }
                if (type.HasFlag(RpcType.Server) && !node.GetTree().IsNetworkServer())
                    return null;
                if (type.HasFlag(RpcType.Unreliable))
                    return node.RpcUnreliable(name, args);
                return node.Rpc(name, args);
            }
            else
            {
                if (type.HasFlag(RpcType.Local))
                    return node.Call(name, args);
            }
            return null;
        }

        public static bool TryIsNetworkMaster(this Node node)
        {
            return GameState.Global.OnlinePlay && node.IsNetworkMaster();
        }

        public static bool TryIsNotNetworkMaster(this Node node)
        {
            return GameState.Global.OnlinePlay && !node.IsNetworkMaster();
        }
    }
}
