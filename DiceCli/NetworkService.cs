using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Dice.Shared.Protocol;
using Google.Protobuf;
using Shared;

namespace DiceCli
{
    public enum EnterResponseState
    {
        NotYet,
        Succeed,
        Failed
    }
    class NetworkService
    {
        private readonly NetClient _netClient;
        private readonly Thread _thread;
        private bool _running;
        private readonly ConcurrentQueue<string> _inputQueue;
        private readonly Dictionary<string, Action<string[]>> _commandHandlers;
        private readonly Dictionary<PacketCode, Action<long, CodedInputStream>> _networkHandlers;
        private long _peerId = -1;
        private string _token;
        private string _deviceId;

        public EnterResponseState EnterResponseState { get; set; }

        public NetworkService()
        {
            _netClient = new NetClient();
            _inputQueue = new ConcurrentQueue<string>();
            _commandHandlers = new Dictionary<string, Action<string[]>>
            {
                { "connect", CmdConnect },
                { "enter", CmdEnter},
                { "move", CmdMove},
            };

            _networkHandlers = new Dictionary<PacketCode, Action<long, CodedInputStream>>
            {
                { PacketCode.EnterRes, HandleEnterResponse },
            };

            _thread = new Thread(ThreadFunc);
        }

        public void Initialize()
        {
            Log.EnsureInitialized();
            //LoadConfig();

            _netClient.Initialize();
            _netClient.OnConnected += OnConnected;
            _netClient.OnDisconnected += OnDisconnected;
            _netClient.OnTimeout += OnTimeout;
            _netClient.OnReceived += OnReceived;

            _running = true;
            _thread.Start();
        }

        public void Deinitialize()
        {
            Log.Debug(this, "Deinitialize");
            _netClient.Disconnect();
            _running = false;
            _thread.Abort();
            _netClient.Deinitialize();
            Log.Shutdown();
        }

        private void ThreadFunc()
        {
            Log.Debug(this, "Service thread has been started");
            do
            {
                try
                {
                    _netClient.HandleEvent(30);
                    ConsumeInputQueue();
                    //UpdatePosition();
                    Thread.Sleep(30);
                }
                catch (Exception e)
                {
                    Log.Error(this, e.ToString());
                }
            } while (_running);

            Log.Debug(this, "Service thread has been terminated");
        }

        private void OnConnected(long peerId)
        {
            Log.Debug(this, $"OnConnected peerID: {peerId}");
            SendEnter();
        }

        private void OnDisconnected(long peerId)
        {
            Log.Debug(this, $"OnDisconnected peerID: {peerId}");
        }

        private void OnTimeout(long peerId)
        {
            Log.Debug(this, $"OnTimeout peerID: {peerId}");
        }

        private void OnReceived(long peerId, ushort code, CodedInputStream stream)
        {
            var packetCode = (PacketCode)code;
            Log.Debug(this, $"OnReceived code: {packetCode}");

            if (_networkHandlers.ContainsKey(packetCode))
                _networkHandlers[packetCode].Invoke(peerId, stream);
            else
                Log.Debug(this, $"Unhandled PacketCode: {packetCode}");
           
        }

        public void EnqueueCommand(string command)
        {
            _inputQueue.Enqueue(command);
        }

        private void ConsumeInputQueue()
        {
            if (_inputQueue.IsEmpty)
                return;

            _inputQueue.TryDequeue(out string commandLine);

            var commandArray = commandLine.Split(' ');
            var command = commandArray[0];

            var parameters = commandArray.Length > 1 ? new string[commandArray.Length - 1] : null;
            if (parameters != null)
                Array.Copy(commandArray, 1, parameters, 0, commandArray.Length - 1);

            Log.Debug(this, $"Input command: {command}");

            if (_commandHandlers.ContainsKey(command))
                _commandHandlers[command].Invoke(parameters);
            else
                Log.Debug(this, $"Unknown command: {command}");
        }

        private void CmdConnect(string[] parameters)
        {
            var address = parameters[0];
            _token = parameters[1];
            var addressArray = address.Split(':');
            _peerId = _netClient.Connect(addressArray[0], (ushort)int.Parse(addressArray[1]));
        }

        private void CmdEnter(string[] parameters)
        {
            if (_peerId == -1)
            {
                var address = parameters[0];
                _token = parameters[1];
                _deviceId = parameters[2];
                var addressArray = address.Split(':');
                _peerId = _netClient.Connect(addressArray[0], (ushort)int.Parse(addressArray[1]));
                return;
            }

            SendEnter();
        }

        private void CmdMove(string[] parameters)
        {
            if (_peerId == -1)
            {
                Log.Warn(this, $"connect first");
                return;
            }

            var x = int.Parse(parameters[0]);
            var y = int.Parse(parameters[1]);
            SendMove(x, y);
        }

        private void SendEnter()
        {
            Random rnd = new Random(Guid.NewGuid().GetHashCode());
            var index = rnd.Next(0, 1000);

            var enterReq = new EnterReq() { DeviceId = _deviceId, Token = _token, Name = $"Bot{index}" };
            _netClient.Send(_peerId, (ushort)PacketCode.EnterReq, enterReq);
        }

        private void SendMove(int x, int y)
        {
            var moveReq = new MoveReq();
            moveReq.Trail.Add(new PbPosition() { X = x, Y = y });
            _netClient.Send(_peerId, (ushort)PacketCode.MoveReq, moveReq);
        }

        private void HandleEnterResponse(long peerId, CodedInputStream stream)
        {
            var enterRes = EnterRes.Parser.ParseFrom(stream);
            Log.Debug(this, $"Result: {(ResultCode)enterRes.Result}");
            if (enterRes.Result != (int)ResultCode.Success)
            {
                EnterResponseState = EnterResponseState.Failed;
                return;
            }

            EnterResponseState = EnterResponseState.Succeed;
        }
    }
}
