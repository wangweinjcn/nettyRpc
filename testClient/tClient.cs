using NettyRPC;
using NettyRPC.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace testClient
{
   public class tClient:RpcClient
    {
        public tClient(ISerializer serializer):base(serializer)
        { }
        [Api]

        public string GetVersion()
        {
            Console.WriteLine("receive server call");
            var asm = typeof(tClient).Assembly;
            return asm.GetName()+ asm.GetName().Version.ToString();
        }
        [Api]
        public string EchoClient(string param1, string param2)
        {

            return "client echo:"+ param2 + param1;
        }
    }
}
