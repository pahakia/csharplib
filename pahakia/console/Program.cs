using System;
using pahakia.fault;

namespace console
{
    class MainClass
    {
        public static void Main (string[] args)
        {
            FaultCode fc = new FaultCode ("main.test.fault", 2, "{0}: {1}");
            Fault ft = Fault.create (fc, "hello", "world");
            System.Console.WriteLine (ft.ToString ());
            Console.WriteLine ("Hello World!");
        }
    }
}
