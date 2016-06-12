using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Xml;

namespace FuzzyEngine
{
    // This represent either a single 
    // instruction or a group of instructions
    public class FuzzyNode
    {
        public object Child { get; set; }
        public uint  MinNumber { get; set; }
        public uint? MaxNumber { get; set; }
        public string Name { get; set; }
        public FuzzyNode()
        {
            Child = new List<FuzzyNode>();
            MinNumber = 1;
            MaxNumber = 1;
            Name = "";
        }
        public FuzzyNode(OpCode opcode)
        {
            Child = opcode;
            MinNumber = 1;
            MaxNumber = 1;
            Name = "";
        }

        public IEnumerable<FuzzyNode> EnumerateChilds()
        {
            OpCode? opcode = Child as OpCode?;
            if (opcode != null)
            {
                yield return this;
            }

            List<FuzzyNode> childs = Child as List<FuzzyNode>;

            foreach (var v in childs)
            {
                foreach(var vv in v.EnumerateChilds())
                    yield return vv;
            }
        }
    }

    public class LanguageDescription
    {
        public LanguageDescription()
        {
            groups = new Dictionary<string, FuzzyNode>();
        }
        public Dictionary<string, FuzzyNode> groups;
    }

    public class Loader
    {
        public string LoadILDescription()
        {
            string result = string.Empty;
            string [] wtf = this.GetType().Assembly.GetManifestResourceNames();
            using (Stream stream = this.GetType().Assembly.GetManifestResourceStream("FuzzyEngine.ILMetaInfo.xml"))
            {
                using (StreamReader sr = new StreamReader(stream))
                {
                    result = sr.ReadToEnd();
                }
            }
            return result;
        }



        public LanguageDescription LoadLanguage()
        {
            LanguageDescription desc = new LanguageDescription();
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

                OpCode currentOpcode = OpCodes.Nop;
                if (info != null)
                {
                    currentOpcode = (OpCode)info.GetValue(null);
                }

                foreach(XmlNode group in inst.ChildNodes)
                {
                    string groupName = group.Attributes["Name"].Value;
                    if (groupName == null || groupName.Length==0)
                        continue;
                    FuzzyNode groupList = null;
                    if (!desc.groups.TryGetValue(groupName, out groupList))
                        groupList = desc.groups[groupName] = new FuzzyNode();
                                        
                    ((List<FuzzyNode>)groupList.Child).Add(new FuzzyNode(currentOpcode));

                }
            }

            return desc;
        }

    }
}
