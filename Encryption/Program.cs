using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ZeroMQ;

namespace Encryption
{
    class Program
    {
        static void Main(string[] args)
        {
            //Grasslands.Start();
            //Strawhouse.Start();
            Woodhouse.Start();

            Console.WriteLine("Press <ENTER> To Continue.");
            Console.ReadLine();

        }
    }
}
