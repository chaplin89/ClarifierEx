using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clarifier.Test.SimpleConsoleApplication
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("This is an obfuscated program! Deobfuscate me!");
            int[] testArray = new int[] { 0, 1, 1, 1, 2, 2, 3, 4};
            testArray[0] = 10;
            testArray[1] = 11;
            Console.WriteLine("{0}", testArray[0]);
        }
    }
}
