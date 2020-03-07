using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using NettyRPC.Core;
using NettyRPC.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NettyRPC.Fast
{
    /// <summary>
    /// 表示fast协议的会话对象  
    /// </summary>
    public sealed class FastSession 
    {
        /// <summary>
        /// 会话对象
        /// </summary>
        public  IChannel channel { get;private set; }

        /// <summary>
        /// 中间件实例
        /// </summary>
        private  RpcServer rpcServer { get;  set; }

        /// <summary>
        /// 获取用户数据字典
        /// </summary>
        public ITag Tag
        {
            get
            {
                return (this.channel  as CustTcpSocketChannel).ChannelMata.tags;
            }
        }

        /// <summary>
        /// 获取远程终结点
        /// </summary>
        public EndPoint RemoteEndPoint
        {
            get
            {
                return this.channel.RemoteAddress;
            }
        }

        /// <summary>
        /// 获取本机终结点
        /// </summary>
        public EndPoint LocalEndPoint
        {
            get
            {
                return this.channel.LocalAddress;
            }
        }

        /// <summary>
        /// 获取是否已连接到远程端
        /// </summary>
        public bool IsConnected
        {
            get
            {
                return this.channel.Active;
            }
        }

        /// <summary>
        /// fast协议的会话对象  
        /// </summary>
        /// <param name="session">会话对象</param>
        /// <param name="middleware">中间件实例</param>
        public FastSession(IChannel session, RpcServer middleware)
        {
            this.channel = session;
            this.rpcServer = middleware;
        }

        /// <summary>      
        /// 断开和远程端的连接
        /// </summary>
        public void Close()
        {
            this.channel.CloseSafe();
        }

        /// <summary>
        /// 调用远程端实现的Api        
        /// </summary>        
        /// <param name="api">数据包Api名</param>
        /// <param name="parameters">参数列表</param>      
        /// <exception cref="SocketException"></exception>     
        /// <exception cref="SerializerException"></exception>   
        public void InvokeApi(string api, params object[] parameters)
        {
            var id = this.rpcServer.PacketIdProvider.NewId();
            var packet = new FastPacket(api, id, false);
            packet.SetBodyParameters(this.rpcServer.Serializer, parameters);
            this.channel.WriteAndFlushAsync(packet);
        }

        /// <summary>
        /// 调用远程端实现的Api      
        /// 并返回结果数据任务
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>        
        /// <param name="api">数据包Api名</param>
        /// <param name="parameters">参数</param>       
        /// <exception cref="SocketException"></exception>      
        /// <exception cref="SerializerException"></exception>
        /// <returns>远程数据任务</returns>         
        public ApiResult<T> InvokeApi<T>(string api, params object[] parameters)
        {
            var id = this.rpcServer.PacketIdProvider.NewId();
            var packet = new FastPacket(api, id, false);
            packet.SetBodyParameters(this.rpcServer.Serializer, parameters);
            return Common.InvokeApi<T>(this.channel, this.rpcServer.TaskSetterTable, this.rpcServer.Serializer, packet, this.rpcServer.TimeOut);
        }

        /// <summary>
        /// 同步发送数据
        /// </summary>
        /// <param name="pack">数据范围</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="SocketException"></exception>
        /// <returns></returns>
        public  int Send(object pack)
        {
            if (pack == null)
            {
                throw new ArgumentNullException();
            }

            if (this.IsConnected == false)
            {
                throw new SocketException((int)SocketError.NotConnected);
            }

            this.channel.WriteAndFlushAsync(pack);
            return 0;

        }

      


        /// <summary>
        /// 字符串显示
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.channel.ToString();
        }
    }
}
