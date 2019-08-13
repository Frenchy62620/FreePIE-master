using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZeroMQ;

namespace ZeroMQ_Server
{


    class Program
    {

        static void Main(string[] args)
        {
            
            string name = "World";

            // Create
            // using (var context = new ZContext())
            using (var responder = new ZSocket(ZSocketType.REP))
            {
                // Bind
                responder.Bind("tcp://*:5555");

                while (true)
                {
                    // Receive
                    using (ZFrame request = responder.ReceiveFrame())
                    {
                        Console.WriteLine("Received {0}", request.ReadString());

                        // Do some work
                        Thread.Sleep(1);

                        // Send
                        responder.Send(new ZFrame(name));
                    }
                }
            }
        }
    }
}
