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
            FastClient rc = new FastClient();
            rc.connect().GetAwaiter();
          var res1=  rc.InvokeApi<string>("GetVersion").GetAwaiter().GetResult();
            Console.WriteLine("resutl1:{0}", res1);
            res1= rc.InvokeApi<string>("Echo","test1"," world").GetAwaiter().GetResult();
            Console.WriteLine("resutl2:{0}", res1);
            Console.ReadLine();

        }
    }
}
