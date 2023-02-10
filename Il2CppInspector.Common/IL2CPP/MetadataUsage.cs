/*
    Copyright (c) 2019-2020 Carter Bush - https://github.com/carterbush
    Copyright (c) 2020-2021 Katy Coe - http://www.djkaty.com - https://github.com/djkaty
    Copyright 2020 Robert Xiao - https://robertxiao.ca

    All rights reserved.
*/

namespace Il2CppInspector
{
    public enum MetadataUsageType
    {
        TypeInfo = 1,
        Type = 2,
        MethodDef = 3,
        FieldInfo = 4,
        StringLiteral = 5,
        MethodRef = 6,
    }

    public class MetadataUsage
    {
        public MetadataUsageType Type { get; }
        public int SourceIndex { get; }
        public uint DestinationIndex { get; }
        public ulong VirtualAddress { get; private set; }

        public MetadataUsage(MetadataUsageType type, int sourceIndex, uint destinationIndex, ulong virtualAddress = 0)
        {
            Type = type;
            SourceIndex = sourceIndex;
            VirtualAddress = virtualAddress;
            DestinationIndex = destinationIndex;
        }

        public static MetadataUsage FromEncodedIndex(Il2CppInspector package, uint encodedIndex,
            ulong virtualAddress = 0)
        {
            uint index;
            MetadataUsageType usageType;
            if (package.Version < 19)
            {
                /* These encoded indices appear only in vtables, and are decoded by IsGenericMethodIndex/GetDecodedMethodIndex */
                var isGeneric = encodedIndex & 0x80000000;
                index = package.Binary.VTableMethodReferences[encodedIndex & 0x7FFFFFFF];
                usageType = (isGeneric != 0) ? MetadataUsageType.MethodRef : MetadataUsageType.MethodDef;
            }
            else
            {
                /* These encoded indices appear in metadata usages, and are decoded by GetEncodedIndexType/GetDecodedMethodIndex */
                var encodedType = encodedIndex & 0xE0000000;
                usageType = (MetadataUsageType)(encodedType >> 29);
                index = encodedIndex & 0x1FFFFFFF;

                // From v27 the bottom bit is set to indicate the usage token hasn't been replaced with a pointer at runtime yet
                if (package.Version >= 27)
                    index >>= 1;
            }

            return new MetadataUsage(usageType, (int)index, 0, virtualAddress);
        }

        public static MetadataUsage FromUsagePairMihoyo(Il2CppInspector package, Il2CppMetadataUsagePair usagePair, ulong virtualAddress = 0)
        {
            //ulong mihoyoUsageVA = 0x1880F3BF0; // 2.6.50 Beta
            //ulong mihoyoUsageVA = 0x188111A10; // 2.6.51 Beta
            //ulong mihoyoUsageVA = 0x18813CA70; // 2.6.52 Beta
            //ulong mihoyoUsageVA = 0x18812C040; // 2.6.53 Beta
            //ulong mihoyoUsageVA = 0x18812AEC0; // 2.6.54 Beta
            //ulong mihoyoUsageVA = 0x18812BF30; // 2.7
            //ulong mihoyoUsageVA = 0x188390BB0; // 2.7.50 Beta
            //ulong mihoyoUsageVA = 0x188397410; // 2.7.51 Beta
            //ulong mihoyoUsageVA = 0x1883A0090; // 2.7.52 Beta
            //ulong mihoyoUsageVA = 0x1883AB080; // 2.7.53 Beta
            //ulong mihoyoUsageVA = 0x1883AB8D0; // 2.7.54 Beta
            //ulong mihoyoUsageVA = 0x1883A9A90; // OSRELWin2.8
            //ulong mihoyoUsageVA = 0x1883A9A90; // CNRELWin2.8
            //ulong mihoyoUsageVA = 0x188A8C7F0; // OSCBWin2.8.50 & CNCBWin2.8.50
            //ulong mihoyoUsageVA = 0x18879F1A0; // OSCBWin2.8.51 & CNCBWin2.8.51
            //ulong mihoyoUsageVA = 0x1887A4D40; // OSCBWin2.8.52 & CNCBWin2.8.52
            //ulong mihoyoUsageVA = 0x1887C0400; // OSCBWin2.8.53 & CNCBWin2.8.53
            //ulong mihoyoUsageVA = 0x1887BD4F0; // OSCBWin2.8.54 & CNCBWin2.8.54
            //ulong mihoyoUsageVA = 0x1887C46E0; // OSRELWin3.0
            //ulong mihoyoUsageVA = 0x1887C46E0; // CNRELWin3.0
            ulong mihoyoUsageVA = 0x188CBC4F0; // OSCBWin3.0.50 & CNCBWin3.0.50

            var mihoyoUsage = package.Binary.Image.ReadMappedObject<MihoyoUsages>(mihoyoUsageVA);

            uint index;
            MetadataUsageType usageType;

            /* These encoded indices appear in metadata usages, and are decoded by GetEncodedIndexType/GetDecodedMethodIndex */
            var encodedType = usagePair.encodedSourceIndex & 0xE0000000;
            usageType = (MetadataUsageType)(encodedType >> 29);
            index = usagePair.encodedSourceIndex & 0x1FFFFFFF;

            uint destinationIndex = usagePair.destinationindex;
            ulong baseAddress = 0;
            switch (usageType)
            {
                case MetadataUsageType.StringLiteral:
                    destinationIndex += (uint)mihoyoUsage.fieldInfoUsageCount
                                        + (uint)mihoyoUsage.methodDefRefUsageCount
                                        + (uint)mihoyoUsage.typeInfoUsageCount;
                    baseAddress = mihoyoUsage.stringLiteralUsage;
                    break;
                case MetadataUsageType.FieldInfo:
                    destinationIndex += (uint)mihoyoUsage.methodDefRefUsageCount
                                        + (uint)mihoyoUsage.typeInfoUsageCount;
                    baseAddress = mihoyoUsage.fieldInfoUsage;
                    break;
                case MetadataUsageType.MethodDef:
                case MetadataUsageType.MethodRef:
                    destinationIndex += (uint)mihoyoUsage.typeInfoUsageCount;
                    baseAddress = mihoyoUsage.methodDefRefUsage;
                    break;
                case MetadataUsageType.TypeInfo:
                case MetadataUsageType.Type:
                    baseAddress = mihoyoUsage.typeInfoUsage;
                    break;
            }

            virtualAddress = baseAddress + 8 * usagePair.destinationindex;

            return new MetadataUsage(usageType, (int)index, destinationIndex, virtualAddress);

        }

        public void SetAddress(ulong virtualAddress) => VirtualAddress = virtualAddress;
    }
}