using System;
using JWT;
using JWT.Algorithms;
using JWT.Builder;
using Newtonsoft.Json;

namespace DiceApi
{
    public class JwtUtil
    {
        private static int ExpiryMinutes = 30;
        private static readonly string Secret = "yourOYa5MbnJ1dT0uSiwDVvVBrksecretAAAABBBBB";
        private static readonly string FirstPartOfToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9";

        public static string GenerateToken(string deviceId)
        {
            var rowToken = new JwtBuilder()
                .WithAlgorithm(new HMACSHA256Algorithm())
                .WithSecret(Secret)
                .AddClaim("exp", DateTimeOffset.UtcNow.AddMinutes(ExpiryMinutes).ToUnixTimeSeconds())
                .AddClaim("deviceId", deviceId)
                .Build();

            var tokenArray = rowToken.Split('.');
            var customToken = $"{tokenArray[1]}.{tokenArray[2]}";
            return customToken;
        }

        public enum ValidateTokenResult
        {
            Success,
            Expired,
            InvalidSignature,
            InvalidDeviceId
        };

        public class MyCustomClaims
        {
            public string exp;
            public string deviceId;
        }

        public static ValidateTokenResult IsValidToken(string token, string deviceId)
        {
            var fullToken = $"{FirstPartOfToken}.{token}";
            try
            {
                var json = new JwtBuilder()
                    .WithSecret(Secret)
                    .MustVerifySignature()
                    .Decode(fullToken);

                var customClaims = JsonConvert.DeserializeObject<MyCustomClaims>(json);
                if (customClaims.deviceId != deviceId)
                    return ValidateTokenResult.InvalidDeviceId;

                return ValidateTokenResult.Success;
            }
            catch (TokenExpiredException)
            {
                //Console.WriteLine("Token has expired");
                return ValidateTokenResult.Expired;
            }
            catch (SignatureVerificationException)
            {
                //Console.WriteLine("Token has invalid signature");
                return ValidateTokenResult.InvalidSignature;
            }
        }
    }
}
