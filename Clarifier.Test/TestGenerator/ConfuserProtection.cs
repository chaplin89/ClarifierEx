using Newtonsoft.Json;
using System.Collections.Generic;

namespace Clarifier.Test.TestGenerator
{
    public enum ProtectionType
    {
        AntiDebug,
        AntiILDasm,
        AntiTamper,
        Constants,
        ControlFlow,
        AntiDump,
        InvalidMetadata,
        ReferenceProxy,
        Resources,
        Rename
    }

    public enum SettingType
    {
        Exclusive,
        Combine,
        String,
        Integer,
        Boolean
    }

    public class ProtectionSettings
    {
        private string[] values;
        private SettingType type;

        public string[] Values
        {
            get
            {
                return values;
            }

            set
            {
                values = value;
            }
        }

        public SettingType Type
        {
            get
            {
                return type;
            }

            set
            {
                type = value;
            }
        }
    }

    public class ProtectionDescription
    {
        private string name;
        private Dictionary<string, ProtectionSettings> allowedSettings;

        public string Name
        {
            get
            {
                return name;
            }

            set
            {
                name = value;
            }
        }

        public Dictionary<string, ProtectionSettings> AllowedSettings
        {
            get
            {
                return allowedSettings;
            }

            set
            {
                allowedSettings = value;
            }
        }
    }
    public class ConfuserProtection
    {
        private static Dictionary<ProtectionType, ProtectionDescription> mapProtectionType;

        public static Dictionary<ProtectionType, ProtectionDescription> MapProtectionType
        {
            get
            {
                return mapProtectionType;
            }
            set
            {
                mapProtectionType = value;
            }
        }
    };
}
