// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NettyRPC
{
    public static class ServerSettings
    {
        public static bool IsSsl
        {
            get
            {
                string ssl =commSetting.Configuration!=null? commSetting.Configuration["nettyServer:ssl"]:"false";
                return !string.IsNullOrEmpty(ssl) && bool.Parse(ssl);
            }
        }
        public static int backLength =>commSetting.Configuration!=null? int.Parse(commSetting.Configuration["nettyServer:backLength"]):100;
        public static int Port =>commSetting.Configuration!=null? int.Parse(commSetting.Configuration["nettyServer:port"]):-1;

        public static bool UseLibuv
        {
            get
            {
                string libuv =commSetting.Configuration!=null? commSetting.Configuration["nettyServer:libuv"]:"false";
                return !string.IsNullOrEmpty(libuv) && bool.Parse(libuv);
            }
        }
    }
}