using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clarifier.Test.SimpleConsoleApplication
{

    public struct DummyStruct
    {
        public int i;
        public float f;
        public string wtf;

    }
    class Program
    {


        static void Main(string[] args)
        {
            char[] wttff = { 'a', 'a', 'a', 'a', 'a', 'a', 'a', 'a', 'a', 'a', 'a', 'a', 'a', 'a', 'a', 'a' };
            int[] testtt = new int[] { 0, 5, 3, 2, 4, 5, 6, 78, 9 };
            Console.WriteLine("This is an obfuscated program! De obfuscate me!");
            int[] testArray = new int[] { 0, 1, 1, 1, 2, 2, 3, 4};
            testArray[0] = 10;
            testArray[1] = 11;
            Console.WriteLine("{0}", testArray[0]);

            GC.KeepAlive(wttff);
            GC.KeepAlive(testtt);
        }
    }
}
