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
            char[] charArrayTest = { 'a', 'a', 'a', 'a', 'a', 'a', 'a', 'a', 'a', 'a', 'a', 'a', 'a', 'a', 'a', 'a' };
            int[] testArray = { 0, 1, 1, 1, 2, 2, 3, 4 };

            Console.WriteLine("This is an obfuscated program! De obfuscate me!");
            testArray[0] = 10;
            testArray[1] = 11;
            Console.WriteLine(testArray[0]);

            GC.KeepAlive(charArrayTest);
            GC.KeepAlive(testArray);
        }
    }
}
