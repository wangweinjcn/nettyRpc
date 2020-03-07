// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NettyRPC
{
    using System;
    using DotNetty.Common.Internal.Logging;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging.Console;
    using DotNetty.Buffers;
    public static class commSetting
    {
        public static  int MAX_FRAME_LENGTH = 1024 * 1024;
        public static  int LENGTH_FIELD_LENGTH = 4;
        public static  int LENGTH_FIELD_OFFSET = 1;
        public static  int LENGTH_ADJUSTMENT = 0;
        public static  int INITIAL_BYTES_TO_STRIP = 0;


        public static int maxPackageSize = 8196;
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
            Configuration = new ConfigurationBuilder()
                .SetBasePath(ProcessDirectory)
                .AddJsonFile("appsettings.json")
                .Build();
        }

        public static string ProcessDirectory
        {
            get
            {
#if NETSTANDARD1_3
                return AppContext.BaseDirectory;
#else
                return AppDomain.CurrentDomain.BaseDirectory;
#endif
            }
        }

        public static IConfigurationRoot Configuration { get; }

        public static void SetConsoleLogger() => InternalLoggerFactory.DefaultFactory.AddProvider(new ConsoleLoggerProvider((s, level) => true, false));
    }
}