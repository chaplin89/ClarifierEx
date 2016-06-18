using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;

namespace FuzzyEngine
{
    public class Loader
    {
        public string LoadILDescription()
        {
            string result = string.Empty;
            using (Stream stream = GetType().Assembly.GetManifestResourceStream("FuzzyEngine.ILMetaInfo.xml"))
            {
                using (StreamReader sr = new StreamReader(stream))
                    result = sr.ReadToEnd();
            }
            return result;
        }
        
        public FuzzyNode LoadLanguage()
        {
            FuzzyNode desc = new FuzzyNode();
            XmlDocument langSpec = new XmlDocument();
            langSpec.LoadXml(LoadILDescription());

            XmlNode elemList = langSpec.GetElementsByTagName("InstructionSet")[0];
            Type opcodesType = typeof(OpCodes);

            foreach (XmlNode inst in elemList.ChildNodes)
            {
                if (inst.Name != "Instruction")
                    continue;

                string attrVal = inst.Attributes["Name"].Value;
                FieldInfo info = opcodesType.GetField(attrVal);

                if (info == null)
                    continue;

                OpCode currentOpcode = (OpCode)info.GetValue(null);

                foreach(XmlNode group in inst.ChildNodes)
                {
                    string groupName = group.Attributes["Name"].Value;
                    if (groupName == null || groupName.Length==0)
                        continue;

                    desc[groupName].Childs.Add(new FuzzyNode(currentOpcode));
                }
            }

            FuzzyNode callDescription = new FuzzyNode();
            callDescription.Childs.AddRange(desc["NumberConversion"].Clone().Childs);
            callDescription.Childs.AddRange(desc["wtf"].Clone().Childs);

            callDescription.Childs[0].MaxNumber = 10;

            return desc;
        }
    }
}
