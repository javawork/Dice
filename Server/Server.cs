using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Dice.Shared.Protocol;
using Google.Protobuf;
using Newtonsoft.Json;
using Shared;

namespace Server
{
    class Server
    {
        class Player
        {
            public long Id;
            public string Name;
            public PbPosition Src;
        }

        private readonly NetServer _netServer;
        private readonly Dictionary<long, Player> _players;
        private readonly string _apiUrl;

        public Server(string apiUrl)
        {
            _apiUrl = apiUrl;
            _netServer = new NetServer();
            _players = new Dictionary<long, Player>();
        }

        public void Start()
        {
            _netServer.Initialize();
            _netServer.OnConnected += OnConnected;
            _netServer.OnDisconnected += OnDisconnected;
            _netServer.OnTimeout += OnTimeout;
            _netServer.OnReceived += OnReceived;
            var servicePortStr = System.Configuration.ConfigurationManager.AppSettings["servicePort"];
            var maxClientStr = System.Configuration.ConfigurationManager.AppSettings["maxClient"];
            int port = int.Parse(servicePortStr);
            int maxClient = int.Parse(maxClientStr);
            _netServer.AddListener((ushort)port, maxClient);
            Log.Debug(typeof(Program), $"Starting Server in port: {port}");
            Log.Debug(typeof(Program), $"ApiUrl: {_apiUrl}");
        }

        public void Terminate()
        {
            _netServer.Deinitialize();
        }

        public void Update()
        {
            const int TimeOutInMs = 15;
            _netServer.HandleEvent(TimeOutInMs);
        }

        private static void OnConnected(long peerId)
        {
            Log.Debug(typeof(Program), $"OnConnected peerId: {peerId}");
        }

        private void OnDisconnected(long peerId)
        {
            Log.Debug(typeof(Program), $"OnDisconnected peerId: {peerId}");
            if (_players.ContainsKey(peerId))
            {
                _players.Remove(peerId);
            }
            BroadcastLeaveEvt(peerId);
        }

        private static void OnTimeout(long peerId)
        {
            Log.Debug(typeof(Program), $"OnTimeout peerId: {peerId}");
        }

        private void OnReceived(long peerId, ushort code, CodedInputStream stream)
        {
            PacketCode packetCode = (PacketCode)code;
            Log.Debug(typeof(Program), $"OnReceived PacketCode: {packetCode}, peerId: {peerId}");
            if (packetCode == PacketCode.EnterReq)
            {
                HandleEnterReq(peerId, stream);
            }
            else if (packetCode == PacketCode.MoveReq)
            {
                HandleMoveReq(peerId, stream);
            }
            else if (packetCode == PacketCode.StopReq)
            {
                HandleStopReq(peerId, stream);
            }
        }

        private void BroadcastLeaveEvt(long peerId)
        {
            var leaveEvt = new LeaveEvt() { Id = peerId };
            _netServer.Broadcast((ushort)PacketCode.LeaveEvt, leaveEvt);
        }

        private PbPosition GetRandomPos()
        {
            var rnd = new Random(Guid.NewGuid().GetHashCode());
            return SharedUtil.GetRandomPosition(rnd);
        }

        public void HandleEnterReq(long peerId, CodedInputStream stream)
        {
            var request = EnterReq.Parser.ParseFrom(stream);
            Log.Debug(typeof(Program), $"PlayerName: {request.Name}, Token: {request.Token}, DeviceId: {request.DeviceId}");

            var isValidToken = IsValidToken(request.Token, request.DeviceId);
            if (isValidToken.GetAwaiter().GetResult() == false)
            {
                var failedRes = new EnterRes() { Result = (int)ResultCode.InvalidToken };
                _netServer.Send(peerId, (ushort)PacketCode.EnterRes, failedRes);
                return;
            }

            var enterRes = new EnterRes() { Result = (int)ResultCode.Success, MyPlayer = new PbPlayer() { Id = peerId, Name = request.Name, Pos = GetRandomPos(), Vel = new PbPosition() {X=0.0f, Y = 0.0f} } };
            _netServer.Send(peerId, (ushort)PacketCode.EnterRes, enterRes);

            {
                var enterEvt = new EnterEvt() { Players = {} };
                foreach (var p in _players)
                {
                    var player = new PbPlayer() {Id = p.Key, Name = p.Value.Name, Pos = p.Value.Src};
                    enterEvt.Players.Add(player);
                }
                _netServer.Send(peerId, (ushort)PacketCode.EnterEvt, enterEvt);
            }

            {
                var enterEvt = new EnterEvt() { Players = { enterRes.MyPlayer } };
                _netServer.Broadcast((ushort)PacketCode.EnterEvt, enterEvt);
            }

            _players[peerId] = new Player() {Id = peerId, Name = request.Name, Src = enterRes.MyPlayer.Pos};
        }

        public void HandleMoveReq(long peerId, CodedInputStream stream)
        {
            var request = MoveReq.Parser.ParseFrom(stream);
            if (!_players.ContainsKey(peerId))
            {
                Log.Warn(this, $"No peer ID: {peerId} in players while MoveReq");
                return;
            }

            var player = _players[peerId];
            player.Src = request.Trail[0];

            var moveEvt = new MoveEvt() {Id = peerId};
            moveEvt.Trail.Add(request.Trail);
            _netServer.Broadcast((ushort)PacketCode.MoveEvt, moveEvt);
        }

        public void HandleStopReq(long peerId, CodedInputStream stream)
        {
            if (!_players.ContainsKey(peerId))
            {
                Log.Warn(this, $"No peer ID: {peerId} in players while StopReq");
                return;
            }
            var request = StopReq.Parser.ParseFrom(stream);
            Log.Debug(typeof(Program), $"Pos: {request.Pos.X}, {request.Pos.Y}");

            var player = _players[peerId];
            player.Src = request.Pos;

            var stopEvt = new StopEvt() { Id = peerId, Pos = request.Pos};
            _netServer.Broadcast((ushort)PacketCode.StopEvt, stopEvt);
        }

        private async Task<bool> IsValidToken(string token, string deviceId)
        {
            HttpClient client = new HttpClient();
            var values = new Dictionary<string, string>
            {
                { "Token", token },
                { "DeviceId", deviceId }
            };

            var requestParam = new ValidateTokenRequest() { Token = token, DeviceId = deviceId };
            var content = new StringContent(JsonConvert.SerializeObject(requestParam), System.Text.Encoding.UTF8, "application/json");

            var fullUrl = $"{_apiUrl}/validatetoken";
            var responseTask = await client.PostAsync(fullUrl, content);
            var responseString = await responseTask.Content.ReadAsStringAsync();
            var response = JsonConvert.DeserializeObject<ValidateTokenResponse>(responseString);
            Log.Debug(this, $"ValidateToken result: {(ResultCode)response.Result}");
            return (ResultCode)response.Result == ResultCode.Success;
        }

    }
}
