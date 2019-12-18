using System.Collections;
using System.Collections.Generic;
using Dice.Shared.Protocol;
using Google.Protobuf;
using Shared;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour
{
    class PlayerObject
    {
        public GameObject Body;
        public GameObject Text;
        public Queue<Vector2> Trail;
        public bool Stop;
    }

    public static GameManager Instance;
    private NetClient _netClient;
    public GameObject myPlayerFactory;
    public GameObject playerFactory;
    public GameObject playerNameTextFactory;
    public string apiUrl;
    private PlayerObject _myPlayer;
    private readonly Dictionary<long, PlayerObject> _players = new Dictionary<long, PlayerObject>();
    private long _myPlayerId;
    private long _peerId;
    private string _authToken;
    private string _deviceId = "myDeviceId";

    void Awake()
    {
        MakeSingleton();
    }

    private void MakeSingleton()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            _netClient = new NetClient();
            _netClient.OnConnected += OnConnected;
            _netClient.OnReceived += OnReceived;
            NetInit();
            DontDestroyOnLoad(gameObject);
        }
    }
    
    private void NetInit()
    {
        Debug.Log("NetInit");
        _netClient.Initialize();
    }

    public void Connect(string address)
    {
        var arr = address.Split(':');
        Debug.Log($"{arr[0]}, {arr[1]}");

        _netClient.Connect(arr[0], (ushort)int.Parse(arr[1]));
    }

    public void GetToken()
    {
        var fullUrl = $"{apiUrl}/GetToken?deviceId={_deviceId}";
        Debug.Log(fullUrl);
        StartCoroutine(GetRequest(fullUrl));
    }

    private void OnConnected(long peerId)
    {
        Debug.Log($"OnConnected {peerId}");
        
        GameObject inputFieldGo = GameObject.Find("PlayerNameInput");
        InputField inputFieldCo = inputFieldGo.GetComponent<InputField>();
        var playerName = inputFieldCo.text;

        UnityEngine.SceneManagement.SceneManager.LoadScene(1);
        var x = Random.Range(-5.0f, 5.0f);
        var y = Random.Range(-5.0f, 5.0f);
        var enterReq = new EnterReq() { Token = _authToken, DeviceId = _deviceId, Name = playerName};
        _netClient.Send(peerId, (ushort)PacketCode.EnterReq, enterReq);
        _peerId = peerId;
    }

    private void OnReceived(long peerId, ushort code, CodedInputStream stream)
    {
        var packetCode = (PacketCode) code;
        Debug.Log($"OnReceived PacketCode: {packetCode}");
        if (packetCode == PacketCode.EnterRes)
        {
            var enterRes = EnterRes.Parser.ParseFrom(stream);
            var resultCode = (ResultCode)enterRes.Result;
            if (resultCode != ResultCode.Success)
            {
                Debug.Log($"EnterRes failed. ResultCode: {resultCode}");
                return;
            }

            _myPlayerId = enterRes.MyPlayer.Id;
            _myPlayer = new PlayerObject() {Body = Instantiate(myPlayerFactory)};
            //_myPlayer.Body.transform.position = new Vector2(enterRes.MyPlayer.Pos.X, enterRes.MyPlayer.Pos.X);
            _myPlayer.Body.transform.position = ToViewportPos(enterRes.MyPlayer.Pos);
            //var screenPosition = Camera.main.WorldToScreenPoint(_myPlayer.Body.transform.position);
            //var nameTextMesh = _myPlayer.Body.GetComponent<TextMesh>();
            //nameTextMesh.text = enterRes.MyPlayer.Name;

            var canvas = GameObject.Find("Canvas");
            _myPlayer.Text = Instantiate(playerNameTextFactory);
            _myPlayer.Text.transform.SetParent(canvas.transform, false);

            //_myPlayer.Text.transform.position = screenPosition + new Vector3(0.0f, 24.0f, 0.0f);
            //var textPos = new PbPosition() {X = enterRes.MyPlayer.Pos.X, Y = enterRes.MyPlayer.Pos.Y + 24.0f};
            _myPlayer.Text.transform.position = GetNameTextPos(_myPlayer.Body.transform.position);
            _myPlayer.Stop = true;
            var textComponent = _myPlayer.Text.GetComponent<Text>();
            textComponent.text = enterRes.MyPlayer.Name;
        }
        else if (packetCode == PacketCode.EnterEvt)
        {
            var enterEvt = EnterEvt.Parser.ParseFrom(stream);

            foreach (var p in enterEvt.Players)
            {
                if (_myPlayerId == p.Id)
                    continue;

                Debug.Log($"Add Player {p.Id}, {p.Name}");

                var player = Instantiate(playerFactory);

                //player.transform.position = new Vector2(p.Pos.X, p.Pos.X);
                player.transform.position = ToViewportPos(p.Pos);

                //var screenPosition = Camera.main.WorldToScreenPoint(player.transform.position);

                var canvas = GameObject.Find("Canvas");
                var playerNameText = Instantiate(playerNameTextFactory);
                playerNameText.transform.SetParent(canvas.transform, false);
                //playerNameText.transform.position = screenPosition + new Vector3(0.0f, 24.0f, 0.0f);
                playerNameText.transform.position = GetNameTextPos(player.transform.position);
                var textComponent = playerNameText.GetComponent<Text>();
                textComponent.text = p.Name;

                var playerObject = new PlayerObject() {Body = player, Text = playerNameText, Trail = new Queue<Vector2>(), Stop = true};
                _players[p.Id] = playerObject;
            }
        }
        else if (packetCode == PacketCode.LeaveEvt)
        {
            var leaveEvt = LeaveEvt.Parser.ParseFrom(stream);
            var playerId = leaveEvt.Id;
            if (_players.ContainsKey(playerId))
            {
                var playerObject = _players[playerId];
                Destroy(playerObject.Body);
                Destroy(playerObject.Text);
                _players.Remove(playerId);
            }
        }
        else if (packetCode == PacketCode.MoveEvt)
        {
            var moveEvt = MoveEvt.Parser.ParseFrom(stream);
            var playerId = moveEvt.Id;
            if (_players.ContainsKey(playerId))
            {
                var playerObject = _players[playerId];
                //playerObject.Body.transform.position = ToViewportPos(moveEvt.Src);
                //playerObject.DstPos = ToViewportPos(moveEvt.Dst);

                foreach (var p in moveEvt.Trail)
                {
                    playerObject.Trail.Enqueue(ToViewportPos(p));
                }

                //if (playerObject.Stop)
                //    playerObject.Stop = false;
                //Debug.DrawLine(playerObject.Body.transform.position, playerObject.DstPos);
            }

        }
        else if (packetCode == PacketCode.StopEvt)
        {
            var stopEvt = StopEvt.Parser.ParseFrom(stream);
            var playerId = stopEvt.Id;
            if (_players.ContainsKey(playerId))
            {
                var playerObject = _players[playerId];
                //playerObject.Body.transform.position = ToViewportPos(stopEvt.Pos);
                //playerObject.Text.transform.position = GetNameTextPos(playerObject.Body.transform.position);
                playerObject.Trail.Enqueue(ToViewportPos(stopEvt.Pos));
                /*
                playerObject.DstPos = ToViewportPos(stopEvt.Pos);
                if (!playerObject.DstPos.Equals(playerObject.Body.transform.position))
                {
                    playerObject.Stop = false;
                }
                */
                //playerObject.Velocity = Vector2.zero;
            }
        }
    }

    public void FixedUpdate()
    {
        //Debug.Log("GameManager FixedUpdate");
        _netClient.HandleEvent(30);

        if (_myPlayer != null)
        {
            //var screenPosition = Camera.main.WorldToScreenPoint(_myPlayer.Body.transform.position);

            _myPlayer.Text.transform.position = GetNameTextPos(_myPlayer.Body.transform.position);
        }

        
        foreach (var p in _players)
        {
            //if (p.Value.Stop)
            //    continue;

            if (p.Value.Trail.Count == 0)
                continue;

            var srcPos = new Vector2(p.Value.Body.transform.position.x, p.Value.Body.transform.position.y);
            var dstPos = p.Value.Trail.Peek();
            while (p.Value.Trail.Count > 0)
            {
                //var dstPos = p.Value.DstPos;
                if (Vector2.Distance(dstPos, srcPos) > 0.01f)
                {
                    break;
                }
                p.Value.Trail.Dequeue();
                if (p.Value.Trail.Count == 0)
                    break;
                dstPos = p.Value.Trail.Peek();
            }

            //var curPos2 = new Vector2(p.Value.Body.transform.position.x, p.Value.Body.transform.position.y);
            //var newPos2 = curPos2 + (p.Value.Velocity * (Time.fixedDeltaTime*0.8f));
            //p.Value.Body.transform.position = new Vector3(newPos2.x, newPos2.y, p.Value.Body.transform.position.z);

            const int speed = 5;
            float step = speed * Time.deltaTime;
            p.Value.Body.transform.position = Vector2.MoveTowards(srcPos, dstPos, step);
            

            //var screenPosition = Camera.main.WorldToScreenPoint(p.Value.Body.transform.position);
            p.Value.Text.transform.position = GetNameTextPos(p.Value.Body.transform.position);
        }
        
    }

    void OnApplicationQuit()
    {
        _netClient.Disconnect();
        _netClient.Deinitialize();
    }

    public void SendMove(Queue<Vector2> trail)
    {
        var moveReq = new MoveReq();
        foreach (var p in trail)
        {
            moveReq.Trail.Add(ToWorldPos(p));
        }
        
        _netClient.Send(_peerId, (ushort)PacketCode.MoveReq, moveReq);
    }

    public void SendStop(Vector2 pos)
    {
        var stopReq = new StopReq() {Pos = ToWorldPos(pos)};
        _netClient.Send(_peerId, (ushort)PacketCode.StopReq, stopReq);
        /*
        Vector2 edgeVector = Camera.main.ViewportToWorldPoint(pos);
        float height = edgeVector.y * 2;
        float width = edgeVector.x * 2;
        Debug.Log($"Pos W: {width}, H: {height}");

        var viewerPosition = Camera.main.WorldToViewportPoint(edgeVector);
        Debug.Log($"Pos X: {viewerPosition.x}, Y: {viewerPosition.y}");
        */
    }

    IEnumerator GetRequest(string uri)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
        {
            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();

            if (webRequest.isNetworkError)
            {
                Debug.Log(": Error: " + webRequest.error);
            }
            else
            {
                Debug.Log(":\nReceived: " + webRequest.downloadHandler.text);
                var response = JsonUtility.FromJson<GetTokenResponse>(webRequest.downloadHandler.text);
                Debug.Log($"GameServerAddress : {response.GameServerAddress}");
                _authToken = response.Token;
                Connect(response.GameServerAddress);
                //var arr = response.gameServerAddress.Split(':');
                //Debug.Log($"{arr[0]}, {arr[1]}");
                //var sceneManager = GameObject.FindObjectOfType<SceneManager>();
                //sceneManager.LoadScene(1);
            }
        }
    }

    private static Vector2 ToViewportPos(PbPosition src)
    {
        var v = new Vector2(src.X/2.0f, src.Y/2.0f);
        var viewportPos = Camera.main.WorldToViewportPoint(v);
        return viewportPos;
    }

    private static PbPosition ToWorldPos(Vector2 src)
    {
        Vector2 worldVector = Camera.main.ViewportToWorldPoint(src);
        var v = new PbPosition() {X = worldVector.x * 2.0f, Y =worldVector.y * 2.0f};
        return v;
    }

    private static Vector2 GetNameTextPos(Vector2 bodyPos)
    {
        //return bodyPos + new Vector2(0.0f, 0.2f);
        var screenPosition = Camera.main.WorldToScreenPoint(bodyPos);
        
        var sss = screenPosition + new Vector3(0.0f, Screen.height/50.0f, 0.0f);
        //Debug.Log($"{bodyPos.y}, {sss.y}, {sss.y - bodyPos.y}");
        return sss;

        //var worldPos = ToWorldPos(bodyPos);
        //worldPos.X -= 200.0f;
        //worldPos.Y += 4.0f;
        //return ToViewportPos(worldPos);
    }
}
