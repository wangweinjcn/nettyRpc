using NettyRPC;
using NettyRPC.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace testServer
{
    class Program
    {
        static void Main(string[] args)
        {
            RpcServer rs = new RpcServer( new mpSerializer());
           
            rs.start();
            var str = Console.ReadLine();
            while (str != "ccc")
            {
                 str=   Console.ReadLine();
                var fs = rs.getAllSessions().FirstOrDefault();
                if (fs == null)
                {
                    Console.WriteLine("fs is null");
                    continue;
                }
               var xx= fs.InvokeApi<string>("GetVersion").GetAwaiter().GetResult();
                Console.WriteLine(xx);
            }
        }
    }
}
