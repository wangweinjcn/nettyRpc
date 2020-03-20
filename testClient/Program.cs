using NettyRPC;
using NettyRPC.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace testClient
{
    class Program
    {
        static  void Main(string[] args)
        {

            //Task.Run(() => testthreadMain());
            //Console.ReadLine();
            //return;
            RpcClient rc = new tClient( new mpSerializer());
           
            rc.connect().GetAwaiter();
            var res1=  rc.InvokeApi<string>("GetVersion").GetAwaiter().GetResult();
            
            Console.WriteLine("resutl1:{0}", res1);
            var str= Console.ReadLine();
            while (str != "ccc")
            {
                res1 = rc.InvokeApi<string>("Echo", str, " world").GetAwaiter().GetResult();
                Console.WriteLine("resutl2:{0}", res1);
                List<object> testdata = new List<object>();
                testdata.Add(new { dd = str });
                testdata.Add(new { dd = res1 });
                var res2 = rc.InvokeApi<string>("Echo2",testdata).GetAwaiter().GetResult();
                Console.WriteLine("resutl:{0}", res2);
                str= Console.ReadLine();
            }

        }
        static void testThread()
        {
            var cp = System.Threading.Thread.CurrentPrincipal;
            var ident = cp.Identity as ClaimsIdentity;
            if (ident != null)
                foreach (var obj in ident.Claims)
                {
                    Console.Write("thread id:" + System.Threading.Thread.CurrentThread.ManagedThreadId +" ");
                    Console.WriteLine(obj.Subject + ":" + obj.Value);
                }

            System.Threading.Thread.Sleep(2000);
        }
        static void testthreadMain()
        {
            List<Claim> claims = new List<Claim>();
            claims.Add(new Claim("UserId", "admin"));
            ClaimsIdentity ci = new ClaimsIdentity(claims);
            var userPrincipal = new ClaimsPrincipal(ci);
            System.Threading.Thread.CurrentPrincipal = userPrincipal;
            for (int i = 0; i < 5; i++)
            {
                Task.Run(() => testThread());

                System.Threading.Thread.Sleep(1000);
            }
        }
    }
}
