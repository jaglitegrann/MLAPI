#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
using MLAPI.MonoBehaviours.Core;
using System;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace MLAPI.Data.Transports.UNET
{
    [Serializable]
    public class UnetTransport : IUDPTransport
    {
        public ChannelType InternalChannel { get => ChannelType.ReliableFragmentedSequenced; }

        public uint HostDummyId { get => new NetId(0,0,true,false).GetClientId(); }

        public uint InvalidDummyId { get => new NetId(0, 0, false, true).GetClientId(); }

        public uint ServerNetId { get => new NetId((byte)serverHostId, (ushort)serverConnectionId, false, false).GetClientId(); }

        public int serverConnectionId;
        public int serverHostId;

        public List<TransportHost> ServerTransports = new List<TransportHost>()
        {
            new TransportHost()
            {
                Name = "UDP Socket",
                Port = 7777,
                Websockets = false
            }
        };

        public int Connect(string address, int port, object settings, bool websocket, out byte error)
        {
            NetworkTransport.Init();
            int hostId = NetworkTransport.AddHost((HostTopology)settings);
            return NetworkTransport.Connect(hostId, address, port, 0, out error);
        }

        public void DisconnectClient(uint clientId)
        {
            NetId netId = new NetId(clientId);
            if (netId.IsHost() || netId.IsInvalid())
                return;
            byte error;
            NetworkTransport.Disconnect(netId.HostId, netId.ConnectionId, out error);
        }

        public void DisconnectFromServer()
        {
            byte error;
            NetworkTransport.Disconnect(serverHostId, serverConnectionId, out error);
        }

        public int GetCurrentRTT(uint clientId, out byte error)
        {
            NetId netId = new NetId(clientId);
            return NetworkTransport.GetCurrentRTT(netId.HostId, netId.ConnectionId, out error);
        }

        public int GetNetworkTimestamp()
        {
            return NetworkTransport.GetNetworkTimestamp();
        }

        public int GetRemoteDelayTimeMS(uint clientId, int remoteTimestamp, out byte error)
        {
            NetId netId = new NetId(clientId);
            return NetworkTransport.GetRemoteDelayTimeMS(netId.HostId, netId.ConnectionId, remoteTimestamp, out error);
        }

        public NetEventType PollReceive(out uint clientId, out int channelId, ref byte[] data, int bufferSize, out int receivedSize, out byte error)
        {
            int hostId;
            int connectionId;
            byte err;
            NetworkEventType eventType = NetworkTransport.Receive(out hostId, out connectionId, out channelId, data, bufferSize, out receivedSize, out err);
            clientId = new NetId((byte)hostId, (ushort)connectionId, false, false).GetClientId();
            NetworkError errorType = (NetworkError)err;
            if (errorType == NetworkError.Timeout)
                eventType = NetworkEventType.DisconnectEvent; //In UNET. Timeouts are not disconnects. We have to translate that here.
            error = 0;

            //Translate NetworkEventType to NetEventType
            switch (eventType)
            {
                case NetworkEventType.DataEvent:
                    return NetEventType.Data;
                case NetworkEventType.ConnectEvent:
                    return NetEventType.Connect;
                case NetworkEventType.DisconnectEvent:
                    return NetEventType.Disconnect;
                case NetworkEventType.Nothing:
                    return NetEventType.Nothing;
                case NetworkEventType.BroadcastEvent:
                    return NetEventType.Nothing;
            }
            return NetEventType.Nothing;
        }

        public void QueueMessageForSending(uint clientId, ref byte[] dataBuffer, int dataSize, int channelId, bool skipqueue, out byte error)
        {
            NetId netId = new NetId(clientId);
            if (netId.IsHost() || netId.IsInvalid())
            {
                error = 0;
                return;
            }
            if (skipqueue)
                NetworkTransport.Send(netId.HostId, netId.ConnectionId, channelId, dataBuffer, dataSize, out error);
            else
                NetworkTransport.QueueMessageForSending(netId.HostId, netId.ConnectionId, channelId, dataBuffer, dataSize, out error);
        }

        public void Shutdown()
        {
            NetworkTransport.Shutdown();
        }

        public void SendQueue(uint clientId, out byte error)
        {
            NetId netId = new NetId(clientId);
            NetworkTransport.SendQueuedMessages(netId.HostId, netId.ConnectionId, out error);
        }

        public void RegisterServerListenSocket(object settings, System.Action OnRegisterServerListenSocketComplete)
        {
            HostTopology topology = new HostTopology((ConnectionConfig)settings, NetworkingManager.singleton.NetworkConfig.MaxConnections);
            for (int i = 0; i < ServerTransports.Count; i++)
            {
                if (ServerTransports[i].Websockets)
                    NetworkTransport.AddWebsocketHost(topology, ServerTransports[i].Port);
                else
                    NetworkTransport.AddHost(topology, ServerTransports[i].Port);
            }

            OnRegisterServerListenSocketComplete.Invoke();
        }

        public int AddChannel(ChannelType type, object settings)
        {
            ConnectionConfig config = (ConnectionConfig)settings;
            switch (type)
            {
                case ChannelType.Unreliable:
                    return config.AddChannel(QosType.Unreliable);
                case ChannelType.UnreliableFragmented:
                    return config.AddChannel(QosType.UnreliableFragmented);
                case ChannelType.UnreliableSequenced:
                    return config.AddChannel(QosType.UnreliableSequenced);
                case ChannelType.Reliable:
                    return config.AddChannel(QosType.Reliable);
                case ChannelType.ReliableFragmented:
                    return config.AddChannel(QosType.ReliableFragmented);
                case ChannelType.ReliableSequenced:
                    return config.AddChannel(QosType.ReliableSequenced);
                case ChannelType.StateUpdate:
                    return config.AddChannel(QosType.StateUpdate);
                case ChannelType.ReliableStateUpdate:
                    return config.AddChannel(QosType.ReliableStateUpdate);
                case ChannelType.AllCostDelivery:
                    return config.AddChannel(QosType.AllCostDelivery);
                case ChannelType.UnreliableFragmentedSequenced:
                    return config.AddChannel(QosType.UnreliableFragmentedSequenced);
                case ChannelType.ReliableFragmentedSequenced:
                    return config.AddChannel(QosType.ReliableFragmentedSequenced);
            }
            return 0;
        }

        public object GetSettings()
        {
            NetworkTransport.Init();
            return new ConnectionConfig()
            {
                SendDelay = 0
            };
        }

        public void Connect(string address, int port, object settings, out byte error, System.Action OnConnectComplete)
        {
            serverHostId = NetworkTransport.AddHost(new HostTopology((ConnectionConfig)settings, 1));
            serverConnectionId = NetworkTransport.Connect(serverHostId, address, port, 0, out error);

            OnConnectComplete.Invoke();
        }
    }
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
