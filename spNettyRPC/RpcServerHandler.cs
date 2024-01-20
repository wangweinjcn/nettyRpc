// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NettyRPC
{
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using DotNetty.Handlers.Timeout;
    using DotNetty.Transport.Channels;
    using DotNetty.Transport.Channels.Sockets;
    using NettyRPC.Fast;

    public class RpcServerHandler : SimpleChannelInboundHandler<FastPacket>
    {

        private RpcServer ownerServer;


        private int lossConnectCount = 0;
        public override async void UserEventTriggered(IChannelHandlerContext context, object evt)
        {
            await Task.Run(() =>
            {
                //已经15秒未收到客户端的消息了！
                Console.WriteLine("已经15秒未收到客户端的消息了");
                if (evt is IdleStateEvent eventState)
                {
                    if (eventState.State == IdleState.ReaderIdle)
                    {
                        lossConnectCount++;
                        if (lossConnectCount > 20)
                        {
                            //("关闭这个不活跃通道！");
                            Console.WriteLine("close 不活跃通道！");
                            context.CloseAsync();
                        }
                    }
                }
                else
                {
                    base.UserEventTriggered(context, evt);
                }
            });
        }

        public RpcServerHandler(RpcServer _server)
        {
            this.ownerServer = _server;

        }
        public override void ChannelRegistered(IChannelHandlerContext context)

        {
           string ClientId = DateTime.Now.ToString("yyyyMMdd-HHmmss.fff");
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
              var ctssc = contex.Channel as CustTcpSocketChannel;
            if (ctssc != null)
            {
                Console.WriteLine("CustTcpSocketChannel");
            }
            this.ownerServer.onConnect(contex.Channel);
           
        }
        public override void ChannelInactive(IChannelHandlerContext context)
        {
            Console.WriteLine("channel inactive");
            this.ownerServer.onDisconnect(context.Channel);
            context.CloseAsync();
        }
      
        protected override void ChannelRead0(IChannelHandlerContext contex, FastPacket msg)
        {
            lossConnectCount = 0;

            if (msg.ApiName == "$$$")
            {
                Console.WriteLine("receive idle message");
                contex.WriteAndFlushAsync("");
            }
            else
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

     public class ServerIdleHandler : ChannelHandlerAdapter, IDisposable
    {


 

 
 
        private int lossConnectCount = 0;
        public override async void UserEventTriggered(IChannelHandlerContext context, object evt)
        {
            await Task.Run(() =>
            {
                //已经15秒未收到客户端的消息了！
                Console.WriteLine("已经15秒未收到客户端的消息了");
                if (evt is IdleStateEvent eventState)
                {
                    if (eventState.State == IdleState.ReaderIdle)
                    {
                        lossConnectCount++;
                        if (lossConnectCount > 2)
                        {
                            //("关闭这个不活跃通道！");
                              Console.WriteLine("close ");
                            context.CloseAsync();
                        }
                    }
                }
                else
                {
                    base.UserEventTriggered(context, evt);
                }
            });
        }
 
        public override bool IsSharable => true;//标注一个channel handler可以被多个channel安全地共享。
 
        //  重写基类的方法，当消息到达时触发，这里收到消息后，在控制台输出收到的内容，并原样返回了客户端
        public override async void ChannelRead(IChannelHandlerContext context, object message)
        {
            Console.WriteLine("now read ,"+lossConnectCount);
            lossConnectCount = 0;
            context.FireChannelRead(message);
            
        }
 

 
        // 输出到客户端，也可以在上面的方法中直接调用WriteAndFlushAsync方法直接输出
        public override async void ChannelReadComplete(IChannelHandlerContext context) => await Task.Run(() => { context.Flush(); });
 
        //捕获 异常，并输出到控制台后断开链接，提示：客户端意外断开链接，也会触发
        public override async void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            await Task.Run(() =>
            {
                //Console.WriteLine("异常: " + exception);
                context.CloseAsync();
            });
        }
 
        public async void Dispose()
        {

        }
    }

}