// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NettyRPC
{
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using DotNetty.Transport.Channels;
    using DotNetty.Transport.Channels.Sockets;
    using NettyRPC.Fast;

    public class RpcServerHandler : SimpleChannelInboundHandler<FastPacket>
    {

        private RpcServer ownerServer;

        public RpcServerHandler(RpcServer _server)
        {
            this.ownerServer = _server;

        }
        public override void ChannelRegistered(IChannelHandlerContext context)

        {
           string ClientId = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            base.ChannelRegistered(context);
            var type = context.Channel.GetType();
            var ctssc = context.Channel as CustTcpSocketChannel;
            if (ctssc != null)
            {
                Console.WriteLine("new client CustTcpServerSocketChannel:{0}",ClientId);
                ctssc.ChannelMata.tags.TryAdd("custId", ClientId);
            }
        }
        public override void ChannelActive(IChannelHandlerContext contex)
        {
            this.ownerServer.onConnect(contex.Channel);
           
        }
        public override void ChannelInactive(IChannelHandlerContext context)
        {
            this.ownerServer.onDisconnect(context.Channel);
            context.CloseAsync();
        }

        protected override void ChannelRead0(IChannelHandlerContext contex, FastPacket msg)
        {
            
            this.ownerServer.ProcessPacketAsync(contex.Channel, msg);
         
           
        }
       
        public override void ChannelReadComplete(IChannelHandlerContext contex)
        {
            contex.Flush();
        }

        public override void ExceptionCaught(IChannelHandlerContext contex, Exception e)
        {
            Console.WriteLine("{0}", e.StackTrace);
            contex.CloseAsync();
        }

        public override bool IsSharable => true;
    }
}