using System;
using System.Net;
using System.Net.Sockets;
using Google.Protobuf;
using System.Collections.Generic;
using Lidgren.Network;

namespace Shared
{
    public class NetClient
    {
        private const byte ChannelId = 0;
        private readonly Dictionary<long, Lidgren.Network.NetClient> _clients;
        private NetPeerConfiguration _peerConfig;
        private byte[] _savedBuffer = null;

        public Action<long> OnConnected = null;
        public Action<long> OnDisconnected = null;
        public Action<long> OnTimeout = null;
        public Action<long, ushort, CodedInputStream> OnReceived = null;
        
        public NetClient()
        {
            _clients = new Dictionary<long, Lidgren.Network.NetClient>();
        }

        public void OnPeerConnected(long peerId)
        {
            OnConnected?.Invoke(peerId);
        }

        public void OnPeerDisconnected(long peerId)
        {
            OnDisconnected?.Invoke(peerId);
            // Todo: Remove Peer
        }

        public void OnNetworkError(IPEndPoint endPoint, SocketError socketErrorCode)
        {
            //Log.Debug(this, "[Client] error! " + socketErrorCode);
        }

        public void OnNetworkReceive(long peerId, byte[] data)
        {
            var readBuffer = data;
            var offset = 0;

            readBuffer = ApplyRemainedBuffer(readBuffer);
            var bufferLength = readBuffer.Length;
            do
            {
                var headerStream = new CodedInputStream(readBuffer, offset, NetUtil.HeaderSize);
                var header = headerStream.ReadSFixed32();
                NetUtil.DeserializeHeader(header, out ushort size, out ushort code);
                
                if (readBuffer.Length < NetUtil.HeaderSize + size)
                {
                    //Log.Warn(this,
                    //    $"Received buffer is smaller than header size. buffer_length: {readBuffer.Length}, sizeInHeader: {size}, offset: {offset}, code: {code}");
                    break;
                }
                offset += NetUtil.HeaderSize;

                var bodyStream = new CodedInputStream(readBuffer, offset, size);
                OnReceived?.Invoke(peerId, code, bodyStream);
                offset += size;
            } while (offset + NetUtil.HeaderSize < bufferLength);

            if (offset < readBuffer.Length)
            {
                SaveRemainedBuffer(readBuffer, offset, readBuffer.Length - offset);
            }
        }

        #region NetClient

        public void Initialize()
        {
            //Log.Debug(this, $"Initialize. maxClient: {maxClient}");
            _peerConfig = new NetPeerConfiguration(SharedUtil.GetAppIdentifierForNet());
            //_peerConfig.DualStack = true;
        }

        public long Connect(string host, ushort port)
        {
            var netClient = new Lidgren.Network.NetClient(_peerConfig);
            netClient.Start();
            _clients.Add(netClient.UniqueIdentifier, netClient);
            var conn = netClient.Connect(host, port);
            return netClient.UniqueIdentifier;
        }

        public void Disconnect(long peerIdParam)
        {
            long peerId = peerIdParam;
            if (!_clients.ContainsKey(peerId))
                return;

            _clients[peerId].Disconnect("good-bye");
        }

        public void Disconnect()
        {
            foreach (var item in _clients.Values)
                item.Disconnect("all-bye");
        }

        public void HandleEvent(int timeoutMs)
        {
            foreach (var item in _clients.Values)
                HandleEvent(item);

            // Todo: Remove disconnected Peer
        }

        public void Deinitialize()
        {
            //Log.Debug(this, "Deinitialize");
            foreach (var item in _clients.Values)
                item.Shutdown("bye");
        }

        public bool Send(long peerIdParam, ushort code, IMessage message)
        {
            long peerId = peerIdParam;
            if (!_clients.ContainsKey(peerId))
                return false;

            var netClient = _clients[peerId];
            var buffer = NetUtil.SerializeMessageToBytes(code, message);
            var netMessage = netClient.CreateMessage(buffer.Length);
            netMessage.Write(buffer);
            netClient.SendMessage(netMessage, NetDeliveryMethod.ReliableOrdered, ChannelId);
            return true;
        }

        public bool IsConnected(long peerIdParam)
        {
            long peerId = peerIdParam;
            if (!_clients.ContainsKey(peerId))
                return false;

            return (_clients[peerId].ConnectionStatus == NetConnectionStatus.Connected);
        }

        #endregion // NetClient

        private void HandleEvent(Lidgren.Network.NetClient netClient)
        {
            NetIncomingMessage netMessage;

            while ((netMessage = netClient.ReadMessage()) != null)
            {
                switch (netMessage.MessageType)
                {
                    case NetIncomingMessageType.Data:
                        var receivedData = netMessage.ReadBytes(netMessage.LengthBytes);
                        OnNetworkReceive(netClient.UniqueIdentifier, receivedData);
                        break;

                    case NetIncomingMessageType.StatusChanged:
                        NetConnectionStatus status = (NetConnectionStatus)netMessage.ReadByte();
                        if (status == NetConnectionStatus.Connected)
                        {
                            //Log.Debug(this, $"Connected {netMessage.SenderConnection.Peer.UniqueIdentifier}");
                            OnPeerConnected(netClient.UniqueIdentifier);
                        }
                        else if (status == NetConnectionStatus.Disconnected)
                        {
                            //Log.Debug(this, $"Disconnected {netMessage.SenderConnection.Peer.UniqueIdentifier}");
                            OnPeerDisconnected(netClient.UniqueIdentifier);
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

        private void SaveRemainedBuffer(byte[] buffer, int offset, int length)
        {
            if (_savedBuffer == null)
            {
                _savedBuffer = new byte[length];
                Array.Copy(buffer, offset, _savedBuffer, 0, length );
            }
            else
            {
                var temp = new byte[_savedBuffer.Length];
                Array.Copy(_savedBuffer, temp, _savedBuffer.Length);
                _savedBuffer = new byte[temp.Length + length];
                Array.Copy(temp, _savedBuffer, temp.Length);
                Array.Copy(buffer, offset, _savedBuffer, temp.Length, length);
            }
        }

        private byte[] ApplyRemainedBuffer(byte[] buffer)
        {
            if (_savedBuffer == null)
                return buffer;

            var newBuffer = new byte[_savedBuffer.Length + buffer.Length];
            Array.Copy(_savedBuffer, newBuffer, _savedBuffer.Length);
            Array.Copy(buffer, 0, newBuffer, _savedBuffer.Length, buffer.Length);
            _savedBuffer = null;
            return newBuffer;
        }
    }
}
