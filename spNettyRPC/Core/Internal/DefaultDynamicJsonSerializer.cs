﻿using NettyRPC.Exceptions;
using NettyRPC.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NettyRPC.Core
{
    /// <summary>
    /// 默认提供的动态Json序列化工具
    /// </summary>
    internal class DefaultDynamicJsonSerializer : IDynamicJsonSerializer
    {
        /// <summary>
        /// 序列化为Json
        /// </summary>
        /// <param name="model">实体</param>
        /// <exception cref="SerializerException"></exception>
        /// <returns></returns>
        public string Serialize(object model)
        {
            return this.Serialize(model, null);
        }

        /// <summary>
        /// 序列化为Json
        /// </summary>
        /// <param name="model">实体</param>
        /// <param name="datetimeFomat">时期时间格式</param>
        /// <exception cref="SerializerException"></exception>
        /// <returns></returns>
        public string Serialize(object model, string datetimeFomat)
        {
            try
            {

                var setting = new JsonSerializerSettings { DateFormatString = datetimeFomat };
                return JsonConvert.SerializeObject(model, setting);

            }
            catch (Exception ex)
            {
                throw new SerializerException(ex);
            }
        }

        /// <summary>
        /// 反序列化json为动态类型
        /// 异常时抛出SerializerException
        /// </summary>
        /// <param name="json">json数据</param>      
        /// <exception cref="SerializerException"></exception>
        /// <returns></returns>
        public dynamic Deserialize(string json)
        {
            try
            {

                return JsonConvert.DeserializeObject<dynamic>(json);

            }
            catch (Exception ex)
            {
                throw new SerializerException(ex);
            }
        }

        /// <summary>
        /// 反序列化为实体
        /// </summary>
        /// <param name="json">json</param>
        /// <param name="type">实体类型</param>
        /// <exception cref="SerializerException"></exception>
        /// <returns></returns>
        public object Deserialize(string json, Type type)
        {
            if (string.IsNullOrEmpty(json) || type == null)
            {
                return null;
            }

            try
            {

                return JsonConvert.DeserializeObject(json, type);

            }
            catch (Exception ex)
            {
                throw new SerializerException(ex);
            }
        }

        /// <summary>
        /// 将值转换为目标类型
        /// 这些值有可能是反序列化得到的动态类型的值
        /// </summary>       
        /// <param name="value">要转换的值，可能</param>
        /// <param name="targetType">转换的目标类型</param>   
        /// <returns>转换结果</returns>
        public object Convert(object value, Type targetType)
        {
            // JObject解析JSON得到动态类型是DynamicObject
            // 默认的Converter实例能转换
            // 如果要加入其它转换单元，请使用new Converter(params IConvert[] customConverts)


            var jToken = value as JToken;
            if (jToken != null)
            {
                return jToken.ToObject(targetType);
            }


            return Converter.Cast(value, targetType);
        }


    }
}
