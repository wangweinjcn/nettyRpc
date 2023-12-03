// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NettyRPC
{
    using System;
    using DotNetty.Common.Internal.Logging;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging.Console;
    using DotNetty.Buffers;
    using System.IO;
    using Microsoft.Extensions.Logging;

    public static class commSetting
    {
        public static int MAX_FRAME_LENGTH { get; set; }
        public static  int LENGTH_FIELD_LENGTH { get; set; }
        public static  int LENGTH_FIELD_OFFSET { get; set; }
        public static  int LENGTH_ADJUSTMENT { get; set; }
        public static  int INITIAL_BYTES_TO_STRIP { get; set; }
        /// <summary>
        /// 心跳检测机制，单位秒
        /// </summary>
        public static int IdleStateTime { get; set; }
  
        public static IByteBuffer[] httpDelimiter()
        {
            return new[]
           {
                Unpooled.WrappedBuffer(new[] { (byte)'\r', (byte)'\n',(byte)'0',(byte)'\r',(byte)'\n' }),
                Unpooled.WrappedBuffer(new[] { (byte)'\n',(byte)'0',(byte)'\n' }),
            };
        }

        public static IByteBuffer[] rpcDelimiter()
        {

            return new[]
           {
                Unpooled.WrappedBuffer(new[] { (byte)'\r', (byte)'\n',(byte)'0',(byte)'\r',(byte)'\n',(byte)'\r',(byte)'\n'  }),
                Unpooled.WrappedBuffer(new[] { (byte)'\n',(byte)'0',(byte)'\n',(byte)'\n' }),
            };
        }
        static commSetting()
        {
            
            string jsonfile = "";
            if(File.Exists(Path.Combine(ProcessDirectory,"configs", "appsettings.json")))
                jsonfile = Path.Combine("configs","appsettings.json");
            else
            if (File.Exists(Path.Combine(ProcessDirectory, "appsettings.json")))
                jsonfile = "appsettings.json";
            if (!string.IsNullOrEmpty(jsonfile))
            {
                Configuration = new ConfigurationBuilder()
                    .SetBasePath(ProcessDirectory)
                    .AddJsonFile(jsonfile)
                    .Build();
            }
            if (Configuration != null)
            {
                int tmpvalue;
                var str = Configuration["nettyComm:MAX_FRAME_LENGTH"];

                if (string.IsNullOrEmpty(str))
                    MAX_FRAME_LENGTH = 1024 * 1024 * 100;//包最大默认100M
                else
                {
                    if (!int.TryParse(str, out tmpvalue))
                        MAX_FRAME_LENGTH = 1024 * 1024 * 100;
                    else
                        MAX_FRAME_LENGTH = tmpvalue;
                }
                Console.WriteLine("max package {0}",MAX_FRAME_LENGTH);
                str = Configuration["nettyComm:LENGTH_FIELD_LENGTH"];//长度域占用长度，默认4字节（int）
                if (string.IsNullOrEmpty(str))
                    LENGTH_FIELD_LENGTH = 4;
                else
                {
                    if (!int.TryParse(str, out tmpvalue))
                        LENGTH_FIELD_LENGTH = 4;
                    else
                        LENGTH_FIELD_LENGTH = tmpvalue;
                }

                str = Configuration["nettyComm:LENGTH_FIELD_OFFSET"];//长度域读偏移量，默认1字节
                if (string.IsNullOrEmpty(str))
                    LENGTH_FIELD_OFFSET = 1;
                else
                {
                    if (!int.TryParse(str, out tmpvalue))
                        LENGTH_FIELD_OFFSET = 1;
                    else
                        LENGTH_FIELD_OFFSET = tmpvalue;
                }

                str = Configuration["nettyComm:LENGTH_ADJUSTMENT"];//数据长度修正，默认0字节
                if (string.IsNullOrEmpty(str))
                    LENGTH_ADJUSTMENT = 0;
                else
                {
                    if (!int.TryParse(str, out tmpvalue))
                        LENGTH_ADJUSTMENT = 0;
                    else
                        LENGTH_ADJUSTMENT = tmpvalue;
                }

                str = Configuration["nettyComm:INITIAL_BYTES_TO_STRIP"];//跳过的字节数。如果你需要接收header+body的所有数据，此值就是0
                if (string.IsNullOrEmpty(str))
                    INITIAL_BYTES_TO_STRIP = 0;
                else
                {
                    if (!int.TryParse(str, out tmpvalue))
                        INITIAL_BYTES_TO_STRIP = 0;
                    else
                        INITIAL_BYTES_TO_STRIP = tmpvalue;
                }

                str = Configuration["nettyComm:IdleStateTime"];//心跳检测间隔，默认15s
                if (string.IsNullOrEmpty(str))
                    IdleStateTime = 15;
                else
                {
                    if (!int.TryParse(str, out tmpvalue))
                        IdleStateTime = 15;
                    else
                        IdleStateTime = tmpvalue;
                }
            }
        }
        public static bool useConsoleLoger
        {
            get
            {
                string str = commSetting.Configuration != null ? commSetting.Configuration["nettyComm:useConsoleLoger"] : "false";
                return !string.IsNullOrEmpty(str) && bool.Parse(str);
            }
        }

        public static string ProcessDirectory
        {
            get
            {
 
                return AppDomain.CurrentDomain.BaseDirectory;
 
            }
        }

        public static IConfigurationRoot Configuration { get; }

#if NET6_0_OR_GREATER
        public static void SetConsoleLogger() => InternalLoggerFactory.DefaultFactory = LoggerFactory.Create(builder => builder.AddConsole());
#endif
#if NET472 || NET451_OR_GREATER  
        public static void SetConsoleLogger() => InternalLoggerFactory.DefaultFactory.AddProvider(new ConsoleLoggerProvider((s, level) => true, false));
#endif
        }
}