using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using DotNetty.Codecs;
using DotNetty.Handlers.Logging;
using DotNetty.Handlers.Tls;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using NettyRPC.codec;
using NettyRPC.Core;
using NettyRPC.Exceptions;
using NettyRPC.Fast;
using NettyRPC.Tasks;

namespace NettyRPC
{
    
    public class RpcServer:IDisposable
    {
        private int backLength;
        private int port;
        private bool useSSl;
        private string sslFile;
        private string sslPassword;

        IChannel rpcServerChannel;
        /// <summary>
        /// 所有Api行为
        /// </summary>
        private ApiActionTable apiActionTable;

        /// <summary>
        /// 获取数据包id提供者
        /// </summary>
        internal PacketIdProvider PacketIdProvider { get; private set; }

        /// <summary>
        /// 获取任务行为记录表
        /// </summary>
        internal TaskSetterTable<long> TaskSetterTable { get; private set; }

        /// <summary>
        /// 获取或设置请求等待超时时间(毫秒)    
        /// 默认30秒
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public TimeSpan TimeOut { get; set; }

       

        /// <summary>
        /// 获取或设置序列化工具
        /// 默认提供者是Json序列化
        /// </summary>
        public ISerializer Serializer { get; set; }

        /// <summary>
        /// 获取全局过滤器管理者
        /// </summary>
        public IGlobalFilters GlobalFilters { get; private set; }

        /// <summary>
        /// 获取或设置依赖关系解析提供者
        /// 默认提供者解析为单例模式
        /// </summary>
        public IDependencyResolver DependencyResolver { get; set; }

        /// <summary>
        /// 获取或设置Api行为特性过滤器提供者
        /// </summary>
        public IFilterAttributeProvider FilterAttributeProvider { get; set; }
        private ConcurrentDictionary<string, FastSession> allchannels = new ConcurrentDictionary<string, FastSession>();

        public RpcServer() : this(ServerSettings.backLength, ServerSettings.Port, false, "")
        {
        }

        public RpcServer(int _backLength, int _port, bool _usessl, string _sslfile, string _sslpassword = "")
        {
            backLength = _backLength;
            port = _port;
            useSSl = _usessl;
            sslFile = _sslfile;
            sslPassword = _sslpassword;
            this.apiActionTable = new ApiActionTable();
            this.PacketIdProvider = new PacketIdProvider();
            this.TaskSetterTable = new TaskSetterTable<long>();

            this.TimeOut = TimeSpan.FromSeconds(30);
            this.Serializer = new DefaultSerializer();
            this.GlobalFilters = new FastGlobalFilters();
            this.DependencyResolver = new DefaultDependencyResolver();
            this.FilterAttributeProvider = new DefaultFilterAttributeProvider();

            DomainAssembly.GetAssemblies().ForEach(item => this.BindService(item));

        }
        public void start()
        {
            this.RunServerAsync();
        }

        /// <summary>
        /// 绑定程序集下所有实现IFastApiService的服务
        /// </summary>
        /// <param name="assembly">程序集</param>
        private void BindService(Assembly assembly)
        {
            var fastApiServices = assembly.GetTypes().Where(item =>
                item.IsAbstract == false
                && item.IsInterface == false
                && typeof(IFastApiService).IsAssignableFrom(item));

            foreach (var type in fastApiServices)
            {
                var actions = Common.GetServiceApiActions(type);
                this.apiActionTable.AddRange(actions);
            }
        }
        /// <summary>
        /// 连接事件
        /// </summary>
        /// <param name="channel"></param>
        internal void onConnect(IChannel channel)
        {
            FastSession fs = null;
            if (!allchannels.ContainsKey(channel.Id.AsLongText()))
            {
                if (allchannels.TryRemove(channel.Id.AsLongText(), out fs))
                {
                    fs.Close();
                }
            }
        }
        /// <summary>
        /// 处理断开连接
        /// </summary>
        /// <param name="channel"></param>
        internal void onDisconnect(IChannel channel)
        {
            FastSession fs = null;
            if (allchannels.ContainsKey(channel.Id.AsLongText()))
            {
                fs = new FastSession(channel, this);
                allchannels.TryAdd(channel.Id.AsLongText(), fs);
            }
        }
        internal async void ProcessPacketAsync(IChannel channel, FastPacket packet)
        {
            FastSession fs = null;
            if (allchannels.ContainsKey(channel.Id.AsLongText()))
            {
                fs = allchannels[channel.Id.AsLongText()];
            }
            else
            {
                fs = new FastSession(channel, this);
                allchannels.TryAdd(channel.Id.AsLongText(), fs);
            }
            var requestContext = new RequestContext(fs, packet);
            this.OnRecvFastPacketAsync(requestContext);
        }

        /// <summary>
        /// 接收到会话对象的数据包
        /// </summary>
        /// <param name="requestContext">请求上下文</param>
        private async void OnRecvFastPacketAsync(RequestContext requestContext)
        {
            if (requestContext.Packet.IsException == true)
            {
                Common.SetApiActionTaskException(this.TaskSetterTable, requestContext);
            }
            else
            {
                await this.ProcessRequestAsync(requestContext);
            }
        }


        /// <summary>
        /// 处理正常的数据请求
        /// </summary>
        /// <param name="requestContext">请求上下文</param>
        /// <returns></returns>
        private async Task ProcessRequestAsync(RequestContext requestContext)
        {
            if (requestContext.Packet.IsFromClient == false)
            {
                Common.SetApiActionTaskResult(requestContext, this.TaskSetterTable, this.Serializer);
            }
            else
            {
                await this.TryExecuteRequestAsync(requestContext);
            }
        }

        /// <summary>
        /// 执行请求
        /// </summary>
        /// <param name="requestContext">上下文</param>
        /// <returns></returns>
        private async Task TryExecuteRequestAsync(RequestContext requestContext)
        {
            try
            {
                var action = this.GetApiAction(requestContext);
                var actionContext = new ActionContext(requestContext, action);
                var fastApiService = this.GetFastApiService(actionContext);
                await fastApiService.ExecuteAsync(actionContext);
                this.DependencyResolver.TerminateService(fastApiService);
            }
            catch (Exception ex)
            {
                var context = new ExceptionContext(requestContext, ex);
                this.OnException(requestContext.Session, context);
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
            if (action == null)
            {
                throw new ApiNotExistException(requestContext.Packet.ApiName);
            }
            return action;
        }

        /// <summary>
        /// 获取FastApiService实例
        /// </summary>
        /// <param name="actionContext">Api行为上下文</param>
        /// <exception cref="ResolveException"></exception>
        /// <returns></returns>
        private IFastApiService GetFastApiService(ActionContext actionContext)
        {
            try
            {
                var serviceType = actionContext.Action.DeclaringService;
                var fastApiService = this.DependencyResolver.GetService(serviceType) as FastApiService;
                return fastApiService.Init(this);
            }
            catch (Exception ex)
            {
                throw new ResolveException(actionContext.Action.DeclaringService, ex);
            }
        }

        /// <summary>
        /// 异常时
        /// </summary>
        /// <param name="sessionWrapper">产生异常的会话</param>
        /// <param name="context">上下文</param>
        protected virtual void OnException(FastSession sessionWrapper, ExceptionContext context)
        {
            Common.SendRemoteException(sessionWrapper.channel, context);
        }
        #region IDisponse
        /// <summary>
        /// 获取对象是否已释放
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// 关闭和释放所有相关资源
        /// </summary>
        public void Dispose()
        {
            if (this.IsDisposed == false)
            {
                this.Dispose(true);
                GC.SuppressFinalize(this);
            }
            this.IsDisposed = true;
        }

        /// <summary>
        /// 析构函数
        /// </summary>
        ~RpcServer()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        /// <param name="disposing">是否也释放托管资源</param>
        protected virtual void Dispose(bool disposing)
        {
             rpcServerChannel.CloseAsync();
            bossGroup.ShutdownGracefullyAsync();
            workerGroup.ShutdownGracefullyAsync();
            foreach (var obj in allchannels.Values)
                obj.Close();
            allchannels.Clear();
        }
        #endregion
        MultithreadEventLoopGroup bossGroup;
        MultithreadEventLoopGroup workerGroup;

        async Task RunServerAsync()
        {          

             bossGroup = new MultithreadEventLoopGroup(1);
             workerGroup = new MultithreadEventLoopGroup();

            var SERVER_HANDLER = new RpcServerHandler(this);

            X509Certificate2 tlsCertificate = null;
            if (useSSl)
            {
                tlsCertificate = new X509Certificate2(Path.Combine(commSetting.ProcessDirectory, sslFile), sslPassword);
            }
            try
            {
                var bootstrap = new ServerBootstrap();
                bootstrap
                    .Group(bossGroup, workerGroup)
                    .Channel<CustTcpServerSocketChannel>()
                    .Option(ChannelOption.SoBacklog, backLength)
                    .Handler(new LoggingHandler(LogLevel.INFO))
                    .ChildHandler(new ActionChannelInitializer<ISocketChannel>(channel =>
                    {
                        IChannelPipeline pipeline = channel.Pipeline;
                        if (tlsCertificate != null)
                        {
                            pipeline.AddLast(TlsHandler.Server(tlsCertificate));
                        }

                        pipeline.AddLast(new FastPacketDecode(commSetting.MAX_FRAME_LENGTH, commSetting.LENGTH_FIELD_OFFSET, commSetting.LENGTH_FIELD_LENGTH, commSetting.LENGTH_ADJUSTMENT, commSetting.INITIAL_BYTES_TO_STRIP, false));
                        pipeline.AddLast(new FastPacketEncoder(), SERVER_HANDLER);
                    }));

                rpcServerChannel =  bootstrap.BindAsync(port).GetAwaiter().GetResult();

                               
            }
            finally
            {
                
            }
        }
    }
}
