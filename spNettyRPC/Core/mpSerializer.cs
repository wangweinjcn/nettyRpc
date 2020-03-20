using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NettyRPC.Core
{
    public class mpSerializer : ISerializer
    {

        public object Deserialize(byte[] bytes, Type type)
        {
         //   Console.WriteLine("mpSerializer Deserialize");
         var obj=   MessagePackSerializer.Typeless.Deserialize(bytes) ;
        
            return obj;
        }

        public byte[] Serialize(object model)
        {
           //   Console.WriteLine("mpSerializer Serialize");
            var bin =  MessagePackSerializer.Typeless.Serialize(model);
            return bin;
        }
    }
}
