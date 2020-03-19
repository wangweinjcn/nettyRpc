using NettyRPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace testClient
{
    class Program
    {
        static  void Main(string[] args)
        {
            FastClient rc = new tClient();
            rc.connect().GetAwaiter();
            var res1=  rc.InvokeApi<string>("GetVersion").GetAwaiter().GetResult();
      
            Console.WriteLine("resutl1:{0}", res1);
            var str= Console.ReadLine();
            while (str != "ccc")
            {
                res1 = rc.InvokeApi<string>("Echo", str, " world").GetAwaiter().GetResult();
                Console.WriteLine("resutl2:{0}", res1);
                str= Console.ReadLine();
            }

        }
    }
}
