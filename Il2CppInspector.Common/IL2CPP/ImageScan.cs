/*
    Copyright 2020-2021 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Il2CppInspector
{
    partial class Il2CppBinary
    {
        // Find a sequence of bytes
        // Adapted from https://stackoverflow.com/a/332667
        private int FindBytes(byte[] blob, byte[] signature, int requiredAlignment = 1, int startOffset = 0)
        {
            var firstMatchByte = Array.IndexOf(blob, signature[0], startOffset);
            var test = new byte[signature.Length];

            while (firstMatchByte >= 0 && firstMatchByte <= blob.Length - signature.Length)
            {
                Buffer.BlockCopy(blob, firstMatchByte, test, 0, signature.Length);
                if (test.SequenceEqual(signature) && firstMatchByte % requiredAlignment == 0)
                    return firstMatchByte;

                firstMatchByte = Array.IndexOf(blob, signature[0], firstMatchByte + 1);
            }
            return -1;
        }

        // Find all occurrences of a sequence of bytes, using word alignment by default
        private IEnumerable<uint> FindAllBytes(byte[] blob, byte[] signature, int alignment = 0)
        {
            var offset = 0;
            while (offset != -1)
            {
                offset = FindBytes(blob, signature, alignment != 0 ? alignment : Image.Bits / 8, offset);
                if (offset != -1)
                {
                    yield return (uint)offset;
                    offset += Image.Bits / 8;
                }
            }
        }

        // Find strings
        private IEnumerable<uint> FindAllStrings(byte[] blob, string str) => FindAllBytes(blob, Encoding.ASCII.GetBytes(str), 1);

        // Find 32-bit words
        private IEnumerable<uint> FindAllDWords(byte[] blob, uint word) => FindAllBytes(blob, BitConverter.GetBytes(word), 4);

        // Find 64-bit words
        private IEnumerable<uint> FindAllQWords(byte[] blob, ulong word) => FindAllBytes(blob, BitConverter.GetBytes(word), 8);

        // Find words for the current binary size
        private IEnumerable<uint> FindAllWords(byte[] blob, ulong word)
            => Image.Bits switch
            {
                32 => FindAllDWords(blob, (uint)word),
                64 => FindAllQWords(blob, word),
                _ => throw new InvalidOperationException("Invalid architecture bit size")
            };

        // Find all valid virtual address pointers to a virtual address
        private IEnumerable<ulong> FindAllMappedWords(byte[] blob, ulong va)
        {
            var fileOffsets = FindAllWords(blob, va);
            foreach (var offset in fileOffsets)
                if (Image.TryMapFileOffsetToVA(offset, out va))
                    yield return va;
        }

        // Find all valid virtual address pointers to a set of virtual addresses
        private IEnumerable<ulong> FindAllMappedWords(byte[] blob, IEnumerable<ulong> va) => va.SelectMany(a => FindAllMappedWords(blob, a));

        // Find all valid pointer chains to a set of virtual addresses with the specified number of indirections
        private IEnumerable<ulong> FindAllPointerChains(byte[] blob, IEnumerable<ulong> va, int indirections)
        {
            IEnumerable<ulong> vas = va;
            for (int i = 0; i < indirections; i++)
                vas = FindAllMappedWords(blob, vas);
            return vas;
        }

        // Scan the image for the needed data structures
        private (ulong, ulong) ImageScan(Metadata metadata)
        {
            //return (codeRegistration, metadataRegistration);
            //return (0x1880E92F0, 0x1880F3B80); // 2.6.50 Beta
            //return (0x188107110, 0x1881119A0); // 2.6.51 Beta
            //return (0x188132170, 0x18813CA00); // 2.6.52 Beta
            //return (0x188121740, 0x18812BFD0); // 2.6.53 Beta
            //return (0x1881205C0, 0x18812AE50); // 2.6.54 Beta
            //return (0x188121630, 0x18812BEC0); // 2.7
            //return (0x18833FC70, 0x18834A310); // 2.7.50 Beta
            //return (0x188347BE0, 0x188352280); // 2.7.51 Beta
            //return (0x18834F2A0, 0x188359940); // 2.7.52 Beta
            //return (0x188359FF0, 0x188364740); // 2.7.53 Beta
            //return (0x18835A900, 0x188365000); // 2.7.54 Beta
            //return (0x188358AC0, 0x1883631C0); // OSRELWin2.8
            //return (0x18839E0E0, 0x1883A87E0); // CNRELWin2.8
            //return (0x188A40600, 0x188A47D20); // OSCBWin2.8.50 & CNCBWin2.8.50
            //return (0x18874F970, 0x18875A070); // OSCBWin2.8.51 & CNCBWin2.8.51
            //return (0x188755540, 0x18875FC40); // OSCBWin2.8.52 & CNCBWin2.8.52
            //return (0x188770B90, 0x18877B290); // OSCBWin2.8.53 & CNCBWin2.8.53
            //return (0x18876DC50, 0x188778350); // OSCBWin2.8.54 & CNCBWin2.8.54
            //return (0x188774E70, 0x18877F570); // OSRELWin3.0
            //return (0x188774E70, 0x18877F570); // CNRELWin3.0
            return (0x188C71310, 0x188C7B700); // OSCBWin3.0.50 & CNCBWin3.0.50
        }
    }
}