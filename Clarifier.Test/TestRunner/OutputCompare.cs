using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace TestRunner
{
    [TestClass]
    public class OutputCompare
    {
        const string unobfuscatedPath = "Unobfuscated";
        const string obfuscatedPath = "Obfuscated";
        const string deobfuscatedPath = "Deobfuscated";
        const string testcasePath = "TestCase";

        const string deobfuscatoPath = @"..\Clarifier.CLI.exe";

        class TestCase
        {
            public string standardOutputExpect = null;
            public string standardErrorExpect = null;
            public string standardInput = null;
            public Dictionary<string,byte[]> fileStream = null;
        }

        /// <summary>
        /// This test ensure that applications are created correctly 
        /// and they produce an expected output.
        /// </summary>
        [TestMethod]
        public void RunTests()
        {
            string testCaseFolder = Path.Combine(Directory.GetCurrentDirectory(), testcasePath);
            string obfuscatedFolder = Path.Combine(Directory.GetCurrentDirectory(), obfuscatedPath);
            string deobfuscatedFolder = Path.Combine(Directory.GetCurrentDirectory(), deobfuscatedPath);
            string unobfuscatedFolder = Path.Combine(Directory.GetCurrentDirectory(), unobfuscatedPath);

            Dictionary<string, TestCase> testCaseList = new Dictionary<string, TestCase>();
            List<string> ignored = new List<string>();

            foreach (var v in Directory.GetFiles(testCaseFolder))
            {
                int firstDot = 0, lastBackslash = 0;
                string appName;
                string streamType;
                TestCase currentTestCase;

                if ((firstDot = v.IndexOf('.')) == -1)
                    continue;

                lastBackslash = v.LastIndexOf('\\');
                appName = Path.Combine(deobfuscatedFolder, string.Format("{0}.exe",v.Substring(lastBackslash+1, firstDot-1-lastBackslash)));

                if (!testCaseList.TryGetValue(appName, out currentTestCase))
                    currentTestCase = testCaseList[appName] = new TestCase();

                streamType = v.Substring(firstDot+1, v.Length-1 - firstDot);

                if(streamType == "StdOut")
                    currentTestCase.standardOutputExpect = File.ReadAllText(v);
                else if (streamType == "StdIn")
                    currentTestCase.standardInput = File.ReadAllText(v);
                else if (streamType == "StdErr")
                    currentTestCase.standardErrorExpect = File.ReadAllText(v);
                else
                    currentTestCase.fileStream[streamType] = File.ReadAllBytes(v);
            }

            foreach(var currentDeobfuscatedFile in Directory.GetFiles(deobfuscatedFolder))
            {
                TestCase currentTestCase;
                string standardOutput, standardError;
                if (!testCaseList.TryGetValue(currentDeobfuscatedFile, out currentTestCase))
                    continue;

                ProcessStartInfo startInfo = new ProcessStartInfo(currentDeobfuscatedFile)
                {
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                };
                Process testCaseProcess = new Process() { StartInfo = startInfo };
                testCaseProcess.Start();
                standardOutput = testCaseProcess.StandardOutput.ReadToEnd();
                standardError = testCaseProcess.StandardError.ReadToEnd();

                testCaseProcess.WaitForExit();

                if (currentTestCase.standardOutputExpect != null)
                    Assert.AreEqual(standardOutput, currentTestCase.standardOutputExpect);
                if (currentTestCase.standardErrorExpect != null)
                    Assert.AreEqual(standardError, currentTestCase.standardErrorExpect);

                if(currentTestCase.fileStream != null)
                {
                    foreach (var file in currentTestCase.fileStream)
                    {
                        Assert.IsTrue(File.Exists(file.Key));
                        Assert.IsTrue(file.Value.SequenceEqual(File.ReadAllBytes(file.Key)));
                    }
                }
            }
        }
    }
}
