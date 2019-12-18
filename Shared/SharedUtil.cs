using Google.Protobuf;
using Dice.Shared.Protocol;
using System;

namespace Shared
{
    public static class SharedUtil
    {
        public static PbPosition GetRandomPosition(Random rnd)
        {
            var x = rnd.Next(-326, 280);
            var y = rnd.Next(-100, 85);
            return new PbPosition() { X = x, Y = y };
        }
        public static string GetAppIdentifierForNet()
        {
            return "My2Dice5Network9";
        }
    }
}
