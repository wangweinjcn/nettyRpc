// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NettyRPC
{
    using System.Net;

    public class ClientSettings
    {
        

        public static IPAddress Host =>commSetting.Configuration!=null? IPAddress.Parse(commSetting.Configuration["nettyClient:host"]):null;

        public static int Port =>commSetting.Configuration!=null? int.Parse(commSetting.Configuration["nettyClient:port"]):-1;

        public static int Size =>commSetting.Configuration!=null? int.Parse(commSetting.Configuration["nettyClient:size"]):-1;

        public static bool UseLibuv
        {
            get
            {
                string libuv =commSetting.Configuration!=null? commSetting.Configuration["nettyClient:libuv"]:"false";
                return !string.IsNullOrEmpty(libuv) && bool.Parse(libuv);
            }
        }
    }
}