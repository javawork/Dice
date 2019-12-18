using Google.Protobuf;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Lidgren.Network;
using System;
using Shared;

namespace Server
{
    public class NetServer 
    {
        private const byte ChannelId = 0;
        private readonly Dictionary<long, NetConnection> _peers;
        private Lidgren.Network.NetServer _netServer;

        public Action<long> OnConnected = null;
        public Action<long> OnDisconnected = null;
        public Action<long> OnTimeout = null;
        public Action<long, ushort, CodedInputStream> OnReceived = null;

        public NetServer()
        {
            _peers = new Dictionary<long, NetConnection>();
        }

        public void OnPeerConnected(NetConnection conn)
        {
            InsertOrUpdatePeer(conn.RemoteUniqueIdentifier, conn);
            OnConnected?.Invoke(conn.RemoteUniqueIdentifier);
        }

        public void OnPeerDisconnected(NetConnection conn)
        {
            OnDisconnected?.Invoke(conn.RemoteUniqueIdentifier);
            RemovePeerIfExist(conn.RemoteUniqueIdentifier);
            //Log.Debug(this, "[Server] Peer disconnected: " + conn.RemoteEndPoint);
        }

        public void OnNetworkError(IPEndPoint endPoint, SocketError socketErrorCode)
        {
            //Log.Debug(this, "[Server] error: " + socketErrorCode);
        }

        public void OnNetworkReceive(NetConnection conn, byte[] data)
        {
            var readBuffer = data;
            var bufferLength = readBuffer.Length;
            var offset = 0;
            do
            {
                var headerStream = new CodedInputStream(readBuffer, offset, NetUtil.HeaderSize);
                var header = headerStream.ReadSFixed32();
                NetUtil.DeserializeHeader(header, out ushort size, out ushort code);
                offset += NetUtil.HeaderSize;
                var bodyStream = new CodedInputStream(readBuffer, offset, size);
                OnReceived?.Invoke(conn.RemoteUniqueIdentifier, code, bodyStream);
                offset += size;
            } while (offset + NetUtil.HeaderSize < bufferLength);
            
        }

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
            //Log.Debug(this, "OnNetworkLatencyUpdate: " + peer.EndPoint);
        }

        #region Override methods for NetServer

        public void Initialize()
        {
            Log.Debug(this, "Initialize");
        }

        public void AddListener(ushort port, int maxClient)
        {
            var config = new NetPeerConfiguration(SharedUtil.GetAppIdentifierForNet());
            config.Port = port;
            config.MaximumConnections = maxClient;
            //config.DualStack = true;

            _netServer = new Lidgren.Network.NetServer(config);
            _netServer.Start();
        }

        public void Disconnect(long peerId)
        {
            if (!_peers.ContainsKey(peerId))
                return;

            _peers[peerId].Disconnect("good-bye");
        }

        public void HandleEvent(int timeoutMs)
        {
            NetIncomingMessage netMessage;

            while ((netMessage = _netServer.ReadMessage()) != null)
            {
                switch (netMessage.MessageType)
                {
                    case NetIncomingMessageType.Data:
                        var receivedData = netMessage.ReadBytes(netMessage.LengthBytes);
                        OnNetworkReceive(netMessage.SenderConnection, receivedData);
                        break;

                    case NetIncomingMessageType.StatusChanged:
                        NetConnectionStatus status = (NetConnectionStatus)netMessage.ReadByte();
                        if (status == NetConnectionStatus.Connected)
                        {
                            //Log.Debug(this, $"Connected {netMessage.SenderConnection.Peer.UniqueIdentifier}");
                            OnPeerConnected(netMessage.SenderConnection);
                        }
                        else if (status == NetConnectionStatus.Disconnected)
                        {
                            //Log.Debug(this, $"Disconnected {netMessage.SenderConnection.Peer.UniqueIdentifier}");
                            OnPeerDisconnected(netMessage.SenderConnection);
                        }
                        break;

                    case NetIncomingMessageType.VerboseDebugMessage:
                    case NetIncomingMessageType.DebugMessage:
                    case NetIncomingMessageType.WarningMessage:
                    case NetIncomingMessageType.ErrorMessage:
                        //Log.Debug(this, netMessage.ReadString());
                        break;

                    default:
                        //Log.Debug(this, $"Unhandled message: {netMessage.MessageType}");
                        break;
                }
            }
        }

        public void Deinitialize()
        {
            //Log.Debug(this, "Deinitialize");
            _netServer.Shutdown("good-bye");
        }

        public bool Send(long peerIdParam, ushort code, IMessage message)
        {
            long peerId = peerIdParam;
            if (!_peers.ContainsKey(peerId))
                return false;

            //var packetCode = (PacketCode)code;
            var packetCode = code;
            //Log.Debug(this, $"Send {packetCode}");
            var netConnection = _peers[peerId];
            var buffer = NetUtil.SerializeMessageToBytes(code, message);
            var netMessage = _netServer.CreateMessage(buffer.Length);
            netMessage.Write(buffer);
            netConnection.SendMessage(netMessage, NetDeliveryMethod.ReliableOrdered, ChannelId);
            return true;
        }

        public void Broadcast(ushort code, IMessage message)
        {
            var buffer = NetUtil.SerializeMessageToBytes(code, message);
            var netMessage = _netServer.CreateMessage(buffer.Length);
            netMessage.Write(buffer);
            _netServer.SendToAll(netMessage, NetDeliveryMethod.ReliableOrdered);
        }

        #endregion // Override methods for NetServer

        private void InsertOrUpdatePeer(long peerId, NetConnection conn)
        {
            if (_peers.ContainsKey(peerId))
                _peers[peerId] = conn;
            else
                _peers.Add(peerId, conn);
        }

        private void RemovePeerIfExist(long peerId)
        {
            if (!_peers.ContainsKey(peerId))
                return;

            _peers.Remove(peerId);
        }
    }
}
