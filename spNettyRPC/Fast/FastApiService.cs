﻿using NettyRPC.Core;
using NettyRPC.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NettyRPC.Fast
{
    /// <summary>
    /// 表示Fast协议的Api服务基类
    /// </summary>
    public abstract class FastApiService : FastFilterAttribute, IFastApiService
    {
        /// <summary>
        /// 获取关联的服务器实例
        /// </summary>
        protected RpcServer rpcServer { get; private set; }

        /// <summary>
        /// 获取当前Api行为上下文
        /// </summary>
        protected ActionContext CurrentContext { get; private set; }


        /// <summary>
        /// Fast协议的Api服务基类
        /// </summary>
        public FastApiService()
        {
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="middleware">关联的中间件</param>
        /// <returns></returns>
        internal FastApiService Init(RpcServer middleware)
        {
            this.rpcServer = middleware;
            return this;
        }
        /// <summary>
        /// 执行前
        /// </summary>
        /// <param name="actionContext"></param>
        protected virtual void OnActionExecuting(ActionContext actionContext)
        { }
        /// <summary>
        /// 执行后
        /// </summary>
        /// <param name="actionContext"></param>
        protected virtual void OnActionExecuted(ActionContext actionContext)
        { }
        /// <summary>
        /// 执行Api行为
        /// </summary>   
        /// <param name="actionContext">上下文</param>      
        /// <returns></returns>
        async Task IFastApiService.ExecuteAsync(ActionContext actionContext)
        {
            var filters = Enumerable.Empty<IFilter>();
            try
            {
                this.CurrentContext = actionContext;
                filters = this.rpcServer.FilterAttributeProvider.GetActionFilters(actionContext.Action);
                await this.ExecuteActionAsync(actionContext, filters);
            }
            catch (Exception ex)
            {
                Console.WriteLine("IFastApiService.ExecuteAsync exception:{0}",ex.Message+ex.StackTrace);
                this.ProcessExecutingException(actionContext, filters, ex);
            }
        }

        /// <summary>
        /// 处理Api行为执行过程中产生的异常
        /// </summary>
        /// <param name="actionContext">上下文</param>
        /// <param name="filters">过滤器</param>
        /// <param name="exception">异常项</param>      
        private void ProcessExecutingException(ActionContext actionContext, IEnumerable<IFilter> filters, Exception exception)
        {
            var exceptionContext = new ExceptionContext(actionContext, new ApiExecuteException(exception));
            this.ExecAllExceptionFilters(filters, exceptionContext);
            Common.SendRemoteException(actionContext.Session.channel, exceptionContext);
        }

        /// <summary>
        /// 调用自身实现的Api行为            
        /// </summary>       
        /// <param name="actionContext">上下文</param>       
        /// <param name="filters">过滤器</param>
        /// <returns></returns>
        private async Task ExecuteActionAsync(ActionContext actionContext, IEnumerable<IFilter> filters)
        {
            Console.WriteLine("ExecuteActionAsync "+actionContext.Action.ApiName);
            Common.GetAndUpdateParameterValues(this.rpcServer.Serializer, actionContext);

            this.ExecFiltersBeforeAction(filters, actionContext);

            if (actionContext.Result == null)
            {
                await this.ExecutingActionAsync(actionContext, filters);
            }
            else
            {
                 Console.WriteLine("ExecuteActionAsync  have exception:{0}", actionContext.Result);
                var exceptionContext = new ExceptionContext(actionContext, actionContext.Result);
                Common.SendRemoteException(actionContext.Session.channel, exceptionContext);
            }
        }

        /// <summary>
        /// 异步执行Api
        /// </summary>
        /// <param name="actionContext">上下文</param>
        /// <param name="filters">过滤器</param>
        /// <returns></returns>
        private async Task ExecutingActionAsync(ActionContext actionContext, IEnumerable<IFilter> filters)
        {
            Console.WriteLine("ExecutingActionAsync 1");
            var paramters = actionContext.Action.Parameters.Select(p => p.Value).ToArray();

            OnActionExecuting(actionContext);
            var result = await actionContext.Action.ExecuteAsync(this, paramters);
            OnActionExecuted(actionContext);
            this.ExecFiltersAfterAction(filters, actionContext);
             Console.WriteLine("ExecutingActionAsync 2");
            if (actionContext.Result != null)
            {
                 Console.WriteLine("ExecutingActionAsync now exception :{0}",actionContext.Result);
                var exceptionContext = new ExceptionContext(actionContext, actionContext.Result);
                Common.SendRemoteException(actionContext.Session.channel, exceptionContext);
            }
            else if (actionContext.Action.IsVoidReturn == false && actionContext.Session.IsConnected)  // 返回数据
            {
                actionContext.Packet.Body = this.rpcServer.Serializer.Serialize(result);
                lock( actionContext.Session)
                    actionContext.Session.Send(actionContext.Packet);
            }
        }

        /// <summary>
        /// 在Api行为前 执行过滤器
        /// </summary>       
        /// <param name="filters">Api行为过滤器</param>
        /// <param name="actionContext">上下文</param>   
        private void ExecFiltersBeforeAction(IEnumerable<IFilter> filters, ActionContext actionContext)
        {
            var totalFilters = this.GetTotalFilters(filters);
            foreach (var filter in totalFilters)
            {
                filter.OnExecuting(actionContext);
                if (actionContext.Result != null) break;
            }
        }

        /// <summary>
        /// 在Api行为后执行过滤器
        /// </summary>       
        /// <param name="filters">Api行为过滤器</param>
        /// <param name="actionContext">上下文</param>       
        private void ExecFiltersAfterAction(IEnumerable<IFilter> filters, ActionContext actionContext)
        {
            var totalFilters = this.GetTotalFilters(filters);
            foreach (var filter in totalFilters)
            {
                filter.OnExecuted(actionContext);
                if (actionContext.Result != null) break;
            }
        }

        /// <summary>
        /// 执行异常过滤器
        /// </summary>       
        /// <param name="filters">Api行为过滤器</param>
        /// <param name="exceptionContext">上下文</param>       
        private void ExecAllExceptionFilters(IEnumerable<IFilter> filters, ExceptionContext exceptionContext)
        {
            var totalFilters = this.GetTotalFilters(filters);
            foreach (var filter in totalFilters)
            {
                filter.OnException(exceptionContext);
                if (exceptionContext.ExceptionHandled == true) break;
            }
        }


        /// <summary>
        /// 获取全部的过滤器
        /// </summary>
        /// <param name="filters">行为过滤器</param>
        /// <returns></returns>
        private IEnumerable<IFilter> GetTotalFilters(IEnumerable<IFilter> filters)
        {
            return this.rpcServer
                .GlobalFilters
                .Cast<IFilter>()
                .Concat(new[] { this })
                .Concat(filters);
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
        ~FastApiService()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        /// <param name="disposing">是否也释放托管资源</param>
        protected virtual void Dispose(bool disposing)
        {
        }
        #endregion
    }
}
