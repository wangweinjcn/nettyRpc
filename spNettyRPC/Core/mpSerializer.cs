using MessagePack;
using System;
#if NET451
#else
using System.Buffers;
#endif
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NettyRPC.Core
{
    

    public class mpSerializer : ISerializer
    {
        private MethodInfo mDeserializeFunc;
        private Type mpType = typeof(MessagePackSerializer);
        private bool useZip = true;
        public mpSerializer():this(true) { }
        public mpSerializer(bool _compress) {
            useZip = _compress;
            #if NET451
#else
            mDeserializeFunc = GetGenericMethod(mpType, "Deserialize",
                                System.Reflection.BindingFlags.IgnoreCase
                                | System.Reflection.BindingFlags.Public
                                | System.Reflection.BindingFlags.Static, new Type[] { typeof(ReadOnlySequence<byte>), typeof(MessagePackSerializerOptions), typeof(CancellationToken) });
#endif

        }

        private static MethodInfo GetGenericMethod(Type targetType, string name, BindingFlags flags, params Type[] parameterTypes)
        {
            var methods = targetType.GetMethods(flags).Where(m => m.Name == name && m.IsGenericMethod);
            foreach (MethodInfo method in methods)
            {
                var parameters = method.GetParameters();
                if (parameters.Length != parameterTypes.Length)
                    continue;

                for (var i = 0; i < parameters.Length; i++)
                {
                    if (parameters[i].ParameterType != parameterTypes[i])
                        break;
                }
                return method;
            }
            return null;
        }
        public object Deserialize(byte[] bytes, Type type)
        {
            try
            {
                //   Console.WriteLine("mpSerializer Deserialize");
                object obj;
#if NET451
        
        
            if (useZip)
            {
                obj = LZ4MessagePackSerializer.Typeless.Deserialize(bytes);
            }
            else
            {
                obj = MessagePackSerializer.Typeless.Deserialize(bytes);
            }
#else
                MessagePackSerializerOptions options;
                if (useZip)
                {
                    options = MessagePack.Resolvers.ContractlessStandardResolver.Options.WithCompression(MessagePackCompression.Lz4BlockArray);
                }
                else
                    options = MessagePack.Resolvers.ContractlessStandardResolver.Options;
                ReadOnlySequence<byte> inputbytes = new ReadOnlySequence<byte>(bytes);
                obj = mDeserializeFunc.MakeGenericMethod(type).Invoke(mpType, new object[] { inputbytes, options, new CancellationToken() });
#endif
                return obj;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw ex;
            }
        }

        public byte[] Serialize(object model)
        {
            //   Console.WriteLine("mpSerializer Serialize");
            byte[] bin;
#if NET451
            if (useZip)
            {
                bin = LZ4MessagePackSerializer.Typeless.Serialize(model);
            }
            else
            {
                bin = MessagePackSerializer.Typeless.Serialize(model);
            }
#else
            MessagePackSerializerOptions options;
            if (useZip)
            {
                options = MessagePack.Resolvers.ContractlessStandardResolver.Options.WithCompression(MessagePackCompression.Lz4BlockArray);
            }
            else
                options = MessagePack.Resolvers.ContractlessStandardResolver.Options;
            
              bin =  MessagePackSerializer.Serialize(model, options);
#endif
            return bin;
        }
    }
}
