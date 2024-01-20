// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NettyRPC
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;
    using DotNetty.Handlers.Timeout;
    using DotNetty.Transport.Channels;
    using NettyRPC.Exceptions;
    using NettyRPC.Fast;
    using Newtonsoft.Json;

    

    public class RpcClientHandler : SimpleChannelInboundHandler<FastPacket>
    {
        private RpcClient client;
        public RpcClientHandler(RpcClient _client)
        {
            this.client = _client;
        }
        protected override void ChannelRead0(IChannelHandlerContext contex, FastPacket msg)
        {
            try
            {
                client.ProcessPacketAsync(msg);
            }
            catch (Exception ex)
            {

            }
            finally
            { }
            
        }
        public override void UserEventTriggered(IChannelHandlerContext context, object evt)
        {
            Console.WriteLine("客户端循环心跳监测发送: " + DateTime.Now);
            if (evt is IdleStateEvent eventState)
            {
                if (eventState.State == IdleState.WriterIdle)
                {
                    FastPacket fp = new FastPacket("$$$", -1, true);
                    lock (context.Channel)
                        context.WriteAndFlushAsync(fp);
                }
            }
        }
        public override void ChannelInactive(IChannelHandlerContext context)
        {
            Console.WriteLine("channel inactive");
            this.client.OnDisconnected();
            context.CloseAsync();
        }

        public override void ExceptionCaught(IChannelHandlerContext contex, Exception e)
        {
            Console.WriteLine(DateTime.Now.Millisecond);
            Console.WriteLine("{0}", e.StackTrace);
            contex.CloseAsync();
        }
    }
}