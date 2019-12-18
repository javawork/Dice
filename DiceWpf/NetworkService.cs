using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Dice.Shared.Protocol;
using Google.Protobuf;
using Shared;

namespace DiceWpf
{
    class NetworkService
    {
        private readonly NetClient _netClient;
        private readonly Thread _thread;
        private bool _running;
        private readonly ConcurrentQueue<string> _inputQueue;
        private readonly Dictionary<string, Action<string[]>> _commandHandlers;
        private long _peerId = -1;

        public NetworkService()
        {
            _netClient = new NetClient();
            _inputQueue = new ConcurrentQueue<string>();
            _commandHandlers = new Dictionary<string, Action<string[]>>
            {
                { "connect", CmdConnect },
                { "enter", CmdEnter},
            };

            _thread = new Thread(ThreadFunc);
            
        }

        public void Initialize()
        {
            Log.EnsureInitialized();

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

            /*
            if (_networkHandlers.ContainsKey(packetCode))
                _networkHandlers[packetCode].Invoke(peerId, stream);
            else
                Log.Debug(this, $"Unhandled PacketCode: {packetCode}");
                */
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
            _peerId = _netClient.Connect("127.0.0.1", 5101);
        }

        private void CmdEnter(string[] parameters)
        {
            var enterReq = new EnterReq() { DeviceId = "MyDevice", Token = "MyToken", Name = "DiceCli" };
            _netClient.Send(_peerId, (ushort)PacketCode.EnterReq, enterReq);
        }
    }
}
