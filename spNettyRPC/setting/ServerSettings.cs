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
                string ssl = commSetting.Configuration["ssl"];
                return !string.IsNullOrEmpty(ssl) && bool.Parse(ssl);
            }
        }
        public static int backLength => int.Parse(commSetting.Configuration["backLength"]);
        public static int Port => int.Parse(commSetting.Configuration["port"]);

        public static bool UseLibuv
        {
            get
            {
                string libuv = commSetting.Configuration["libuv"];
                return !string.IsNullOrEmpty(libuv) && bool.Parse(libuv);
            }
        }
    }
}