﻿using NettyRPC;
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
            RpcServer rs = new RpcServer();
            rs.start();
            Console.ReadLine();

        }
    }
}
