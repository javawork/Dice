using System.Collections.Generic;
using System;
using System.Net.Http;
using Newtonsoft.Json;
using Shared;
using System.Threading;

namespace DiceCli
{
    class LoginState : iState
    {
        private readonly NetworkService _netService;
        private readonly string _apiUrl;
        private string _token;
        private string _serverAddress;
        private string _deviceId = "";
        public void RegisterMainPackageStates(iState package)
        {
            List<MainStates> states = new List<MainStates>()
            {
                MainStates.Login
            };
            StateManagerService.RegisterMainPackageStates(states, package);
        }

        public LoginState(NetworkService service, string apiUrl)
        {
            _netService = service;
            _apiUrl = apiUrl;
        }

        public bool SetState(string line)
        {
            return false;
        }

        public bool Loop()
        {
            Console.WriteLine("Requesting GetToken...");

            EventWaitHandle doneEvent = new ManualResetEvent(false);

            var getTokenResult = GetToken();
            if (getTokenResult)
            {
                _netService.EnterResponseState = EnterResponseState.NotYet;
                _netService.EnqueueCommand($"enter {_serverAddress} {_token} {GetDeviceId()}");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Login Failed");
                Console.ResetColor();
                StateManagerService.SetState(MainStates.Idle);
                return false;
            }

            for (var i=0;i<5;++i)
            {
                Thread.Sleep(300);
                if (_netService.EnterResponseState != EnterResponseState.NotYet)
                    break;
            }

            if (_netService.EnterResponseState == EnterResponseState.Succeed)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Login succeed");
                Console.ResetColor();
            }
            else if (_netService.EnterResponseState == EnterResponseState.Failed)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Login Failed");
                Console.ResetColor();
            }
            else if (_netService.EnterResponseState == EnterResponseState.NotYet)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Login Timeout");
                Console.ResetColor();

            }

            StateManagerService.SetState(MainStates.Idle);
            return false;
        }

        private bool GetToken()
        {
            try
            {
                var fullUrl = $"{_apiUrl}/GetToken?deviceId={GetDeviceId()}";
                var client = new HttpClient();
                var response = client.GetAsync(fullUrl).Result;

                if (!response.IsSuccessStatusCode)
                    return false;
                
                var responseContent = response.Content;

                string responseString = responseContent.ReadAsStringAsync().Result;
                var getTokenResponse = JsonConvert.DeserializeObject<GetTokenResponse>(responseString);
                Console.WriteLine($"Result: {getTokenResponse.Result}, Token: {getTokenResponse.Token}");
                Console.WriteLine($"ServerAddress: {getTokenResponse.GameServerAddress}");
                //_netService.EnqueueCommand($"enter {getTokenResponse.GameServerAddress} {getTokenResponse.Token}");
                if (getTokenResponse.Result != (int)ResultCode.Success)
                    return false;

                _token = getTokenResponse.Token;
                _serverAddress = getTokenResponse.GameServerAddress;
                return true;
            } 
            catch (Exception e)
            {
                Log.Warn(this, $"Exception while GetToken : {e}");
                return false;
            }
        }

        private string GetDeviceId()
        {
            if (string.IsNullOrEmpty(_deviceId))
            {
                _deviceId = Guid.NewGuid().ToString();
            }
            return _deviceId;
        }
    }
}
