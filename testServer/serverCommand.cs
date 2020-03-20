using NettyRPC.Core;
using NettyRPC.Fast;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
namespace testServer
{
    public class serverCommand : FastApiService
    {
        private bool testclient = false;
        /// <summary>
        /// 获取服务组件版本号
        /// </summary>       
        /// <returns></returns>
        [Api]

        public string GetVersion()
        {
            return typeof(serverCommand).Assembly.GetName().Version.ToString();
        }

        [Api]
        public string Echo2(List<object> data)
        {
            Console.WriteLine("echo2");
            foreach(var obj in data)
                Console.WriteLine(JsonConvert.SerializeObject(obj));
            return "ok";


        }
        [Api]

        public string Echo(string param1,string param2)
        {

            Console.WriteLine(param1 + "--" + param2);
            return param2 + param1;
        }
        [Api]
        public string getClientVersion()
        {
          return  this.CurrentContext.Session.InvokeApi<string>("GetVersion").GetAwaiter().GetResult();
            
        }
    }
}
