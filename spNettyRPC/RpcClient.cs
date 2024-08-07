﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using DotNetty.Codecs;
using DotNetty.Handlers.Timeout;
using DotNetty.Handlers.Tls;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using NettyRPC.codec;
using NettyRPC.Core;
using NettyRPC.Exceptions;
using NettyRPC.Fast;
using NettyRPC.Tasks;
using Newtonsoft.Json;

namespace NettyRPC
{
    /// <summary>
    /// 表示Fast协议的tcp客户端
    /// </summary>
    public abstract class RpcClient 
    {
        protected bool _connected { get; set; }
        private IPAddress host;
        private int port;
        private bool useSSl;
        private string sslFile;
        private string sslPassword;
        /// <summary>
        /// 所有Api行为
        /// </summary>
        private ApiActionTable apiActionTable;

        /// <summary>
        /// 数据包id提供者
        /// </summary>
        private PacketIdProvider packetIdProvider;

        /// <summary>
        /// 任务行为表
        /// </summary>
        private TaskSetterTable<long> taskSetterTable;


        /// <summary>
        /// 获取或设置序列化工具
        /// 默认是Json序列化
        /// </summary>
        public ISerializer Serializer { get; private set; }

        /// <summary>
        /// 获取或设置请求等待超时时间(毫秒) 
        /// 默认30秒
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public TimeSpan TimeOut { get; set; }
        /// <summary>
        /// 
        /// </summary>
        private string ClientId = null;
        /// <summary>
        /// 
        /// </summary>
        protected IChannel clientSession { get; set; }
        /// <summary>
        /// Fast协议的tcp客户端
        /// </summary>
        public RpcClient():this(ClientSettings.Host, ClientSettings.Port,false,"")
        {
          }
        public RpcClient(ISerializer _serializer) : this(ClientSettings.Host, ClientSettings.Port, false, "","",_serializer)
        {
        }
        public RpcClient(string host, int port, ISerializer _serializer) : this(IPAddress.Parse(host), port, false, "", "", _serializer)
        {
        }
        public RpcClient(IPAddress host,int port,ISerializer _serializer) : this(host, port, false, "","",_serializer)
        {
        }
        public RpcClient(IPAddress _host,int _port,bool _usessl,string _sslfile,string _sslpassword="",ISerializer _serializer=null)
        {
            host = _host;
            port =_port;
            useSSl = _usessl;
            sslFile = _sslfile;
            sslPassword = _sslpassword;
            this.apiActionTable = new ApiActionTable(Common.GetServiceApiActions(this.GetType()));
            this.packetIdProvider = new PacketIdProvider();
            this.taskSetterTable = new TaskSetterTable<long>();
            if (_serializer == null)
                this.Serializer = new DefaultSerializer();
            else
                this.Serializer = _serializer;
            this.TimeOut = TimeSpan.FromSeconds(20);

        }
        public virtual void refreshConnect()
        { }
        public async Task connect()
        {
          await  this.startClientAsync();          
           
        }
        public async Task DisposeAsync()
        {
            if(clientSession!=null)
                await clientSession.CloseAsync();

        }

        private bool Send(FastPacket pack)
        {
            lock(this.clientSession)
                this.clientSession.WriteAndFlushAsync(pack);
           
            return true;
        }
        



       
        /// <summary>
        /// 处理接收到服务发来的数据包
        /// </summary>
        /// <param name="packet">数据包</param>
        internal async void ProcessPacketAsync(FastPacket packet)
        {
            var requestContext = new RequestContext(null, packet);
            if (packet.IsException == true)
            {
                Common.SetApiActionTaskException(this.taskSetterTable, requestContext);
            }
            else if (packet.IsFromClient == true)
            {
                Common.SetApiActionTaskResult(requestContext, this.taskSetterTable, this.Serializer);
            }
            else
            {
                
                await TryProcessRequestPackageAsync(requestContext);
            }
        }

        /// <summary>
        /// 处理服务器请求的数据包
        /// </summary>
        /// <param name="requestContext">上下文</param>
        /// <returns></returns>
        private async Task TryProcessRequestPackageAsync(RequestContext requestContext)
        {
            try
            {
                var action = this.GetApiAction(requestContext);
                var actionContext = new ActionContext(requestContext, action);
                await this.ExecuteActionAsync(actionContext);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                var exceptionContext = new ExceptionContext(requestContext, ex);
                Common.SendRemoteException(this.clientSession, exceptionContext);
                this.OnException(requestContext.Packet, ex);
            }
        }

        /// <summary>
        /// 获取Api行为
        /// </summary>
        /// <param name="requestContext">请求上下文</param>
        /// <exception cref="ApiNotExistException"></exception>
        /// <returns></returns>
        private ApiAction GetApiAction(RequestContext requestContext)
        {
            var action = this.apiActionTable.TryGetAndClone(requestContext.Packet.ApiName);
            if (action != null)
            {
                return action;
            }
            throw new ApiNotExistException(requestContext.Packet.ApiName);
        }


        /// <summary>
        /// 执行Api行为
        /// </summary>
        /// <param name="actionContext">上下文</param>  
        /// <returns></returns>
        private async Task ExecuteActionAsync(ActionContext actionContext)
        {
            var action = actionContext.Action;
            var parameters = Common.GetAndUpdateParameterValues(this.Serializer, actionContext);
            var result = await action.ExecuteAsync(this, parameters);

            if (action.IsVoidReturn == false && this.clientSession.Active == true)
            {
                actionContext.Packet.Body = this.Serializer.Serialize(result);
                this.TrySendPackage(actionContext.Packet);
            }
        }

        /// <summary>
        /// 发送数据包
        /// </summary>
        /// <param name="package">数据包</param>
        /// <returns></returns>
        private bool TrySendPackage(FastPacket package)
        {
            try
            {
                
                return this.Send(package);
            }
            catch (Exception)
            {
                _connected = false;
                return false;
            }
        }


        /// <summary>
        ///  当操作中遇到处理异常时，将触发此方法
        /// </summary>
        /// <param name="packet">数据包对象</param>
        /// <param name="exception">异常对象</param> 
        protected virtual void OnException(FastPacket packet, Exception exception)
        {
            _connected = false;
        }

        /// <summary>
        /// 调用服务端实现的Api        
        /// </summary>       
        /// <param name="api">Api行为的api</param>
        /// <param name="parameters">参数列表</param>          
        /// <exception cref="SocketException"></exception> 
        /// <exception cref="SerializerException"></exception> 
        public void InvokeApi(string api, params object[] parameters)
        {
            var packet = new FastPacket(api, this.packetIdProvider.NewId(), true);
            packet.SetBodyParameters(this.Serializer, parameters);
            this.Send(packet);
        }
        public  async Task< T> InvokeApi<T>(string api, params object[] parameters)
        {
            int retrycount = 5;
            int i = 0;
            if (!this._connected)
            {

                refreshConnect();
            }
            while (i < retrycount)
            {
                try
                {
                    var id = this.packetIdProvider.NewId();
                    var packet = new FastPacket(api, id, true);
                    packet.SetBodyParameters(this.Serializer, parameters);
                    var x= Common.InvokeApi<T>(this.clientSession, this.taskSetterTable, this.Serializer, packet, this.TimeOut);
                    return await x.GetTask();
                }
                catch (Exception exception)
                {
                    Console.WriteLine("InvokeApi {0} ex: {1}",api, exception.Message.ToString());
                    i++;
                    this.clientSession.DisconnectAsync().GetAwaiter().GetResult();
                    this.clientSession.CloseAsync().GetAwaiter().GetResult();
                    Thread.Sleep(1000);
                    refreshConnect();
                }
            }
            throw new Exception("can't remote execute");
        }
        /// <summary>
        /// 调用服务端实现的Api   
        /// 并返回结果数据任务
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="api">Api行为的api</param>
        /// <param name="parameters">参数</param>
        /// <exception cref="SocketException"></exception>        
        /// <exception cref="SerializerException"></exception>
        /// <returns>远程数据任务</returns>    
        public  ApiResult<T> InvokeApiOld<T>(string api, params object[] parameters)
        {
            int retrycount = 5;
            int i = 0;
            if (!this._connected)
            {

               refreshConnect();
            }
            while (i < retrycount)
            {
                try
                {
                    var id = this.packetIdProvider.NewId();
                    var packet = new FastPacket(api, id, true);
                    packet.SetBodyParameters(this.Serializer, parameters);
                    return Common.InvokeApi<T>(this.clientSession, this.taskSetterTable, this.Serializer, packet, this.TimeOut);
                }
                catch (Exception exception)
                {
                    Console.WriteLine("InvokeApi ex:", exception.ToString());
                    i++;                    
                     this.clientSession.DisconnectAsync().GetAwaiter().GetResult();
                     this.clientSession.CloseAsync().GetAwaiter().GetResult();
                    Thread.Sleep(100);
                    refreshConnect();
                }
            }
            throw new Exception("can't remote execute");
        }

        /// <summary>
        /// 断开时清除数据任务列表  
        /// </summary>
        internal  void OnDisconnected()
        {
            Console.WriteLine("now disconnected");
            _connected = false;
            var taskSetActions = this.taskSetterTable.RemoveAll();
            foreach (var taskSetAction in taskSetActions)
            {
                var exception = new SocketException(SocketError.Shutdown.GetHashCode());
                taskSetAction.SetException(exception);
            }
        }
        public async Task<int> disconnect()
        {
         await   this.clientSession.DisconnectAsync();
            return 0;
        }
        /// <summary>
        /// 释放资源
        /// </summary>
        public  void Dispose()
        {
          

            this.apiActionTable = null;
            this.taskSetterTable.Clear();
            this.taskSetterTable = null;
            this.packetIdProvider = null;
            this.Serializer = null;
            this.clientSession.CloseSafe();
        }
        async Task startClientAsync()
        {
            ClientId = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            if(commSetting.useConsoleLoger)
                 commSetting.SetConsoleLogger();

            var group = new MultithreadEventLoopGroup();

            X509Certificate2 cert = null;
            string targetHost = null;
            if (useSSl)
            {
                cert = new X509Certificate2(Path.Combine(commSetting.ProcessDirectory,sslFile), sslPassword);
                targetHost = cert.GetNameInfo(X509NameType.DnsName, false);
            }
            try
            {
                var bootstrap = new Bootstrap();
                bootstrap
                    .Group(group)
                    .Channel<CustTcpSocketChannel>()
                    .Option(ChannelOption.TcpNodelay, true)
                    .Handler(new ActionChannelInitializer<ISocketChannel>(channel =>
                    {

                        IChannelPipeline pipeline = channel.Pipeline;

                        if (cert != null)
                        {
                            pipeline.AddLast(new TlsHandler(stream => new SslStream(stream, true, (sender, certificate, chain, errors) => true), new ClientTlsSettings(targetHost)));
                        }
                         pipeline.AddLast(new IdleStateHandler(0,commSetting.IdleStateTime,0));
                        pipeline.AddLast(new FastPacketDecode(commSetting.MAX_FRAME_LENGTH, commSetting.LENGTH_FIELD_OFFSET, commSetting.LENGTH_FIELD_LENGTH, commSetting.LENGTH_ADJUSTMENT, commSetting.INITIAL_BYTES_TO_STRIP, false));
                        pipeline.AddLast(new FastPacketEncoder(), new RpcClientHandler(this));
                       
                    }));

                this.clientSession = AsyncHelpers.RunSync<IChannel>(()=> bootstrap.ConnectAsync(new IPEndPoint(host, port)));
                _connected = true;
                Console.WriteLine("now connect");

            }
            finally
            {

            }
        }
    }
   
}
