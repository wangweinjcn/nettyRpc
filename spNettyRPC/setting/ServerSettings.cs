// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NettyRPC
{
    public static class ServerSettings
    {
        private static int getconfigInt(string str,int defaultvalue)
        {
            int ret = defaultvalue;
            var tmp=commSetting.Configuration!=null? commSetting.Configuration[str]:defaultvalue.ToString();
            int.TryParse(tmp, out ret);
            return ret;
        }
        public static bool IsSsl
        {
            get
            {
                string ssl =commSetting.Configuration!=null? commSetting.Configuration["nettyServer:ssl"]:"false";
                return !string.IsNullOrEmpty(ssl) && bool.Parse(ssl);
            }
        }
        public static int backLength =>commSetting.Configuration!=null? getconfigInt("nettyServer:backLength",100):100;
        public static int Port =>commSetting.Configuration!=null?  getconfigInt("nettyServer:port",-1):-1;
        public static int TimeOut =>commSetting.Configuration!=null?  getconfigInt("nettyServer:TimeOut",60):60;

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