// Guids.cs
// MUST match guids.h
using System;

namespace BohemiaInteractive.vscli
{
    static class GuidList
    {
        public const string guidvscliPkgString = "10e2173a-9cf1-4ab2-ae2a-773bed0fa909";
        public const string guidvscliCmdSetString = "6a2d01ca-6539-4e10-a154-d569e8e329b5";

        public static readonly Guid guidvscliCmdSet = new Guid(guidvscliCmdSetString);
    };
}