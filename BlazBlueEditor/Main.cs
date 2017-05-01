using AdvancedBinary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BlazBlueEditor
{
    public class ATFStringEditor
    {
        byte[] Script;
        public ATFStringEditor(byte[] Script) {
            this.Script = Script;
        }
        private struct ATFHeader {
#pragma warning disable 0169, 0649
            internal uint Signature;
            private int Unk1;
            private int Unk2;
            private int Unk3;
            internal uint ByteCodeStart;
            private int Unk4;
            private int Unk5;
            private int Unk6;
            private int Unk7;
            private int Unk8;
            private int Unk9;
            private int Unk10;
            internal uint StringsTableOffset;
            internal uint StringsTableLenth;
            private int Unk11;
            private int Unk12;
            internal uint TextTableOffset;
            internal uint TextTableLength;
            private int Unk13;
            private int Unk14;
#pragma warning restore 0169, 0649
        }
        List<uint> EntryPos = new List<uint>();
        ATFHeader Header;
        public string[] Import() {
            byte[] HData = new byte[0x50];
            EntryPos = new List<uint>();
            Array.Copy(Script, HData, HData.Length);
            dynamic OHeader = new ATFHeader();
            Tools.ReadStruct(HData, ref OHeader);
            Header = OHeader;
            if (Header.Signature != 0x00465441)
                throw new Exception("Invalid Input File.");
            StructReader Reader = new StructReader(new MemoryStream(Script), Encoding.Unicode);
            Reader.Seek(Header.TextTableOffset, SeekOrigin.Begin);
            List<string> Strings = new List<string>();
            uint Length = 0;
            while (Reader.BaseStream.Position < Script.LongLength - 2) {
                string Str = Reader.ReadString(StringStyle.UCString);
                byte[] Data = GenBC(Length, Str.Length);
                EntryPos.Add((uint)Search(Script, Data, Header.ByteCodeStart, Header.StringsTableOffset));
                Strings.Add(Str);
                Length += (uint)Str.Length + 1;
            }
            return Strings.ToArray();
        }

        public byte[] Export(string[] Strings) {
            byte[] NewScript = new byte[this.Header.TextTableOffset];
            Array.Copy(Script, NewScript, NewScript.Length);
            MemoryStream StringBuffer = new MemoryStream();
            StructWriter Writer = new StructWriter(StringBuffer, Encoding.Unicode);
            uint[] Offsets = EntryPos.ToArray();
            uint Length = 0;
            for (int i = 0; i < Strings.Length; i++) {
                Overwrite(ref NewScript, GenBC(Length, Strings[i].Length), Offsets[i]);
                Length += (uint)Strings[i].Length + 1;
                Writer.Write(Strings[i], StringStyle.UCString);
            }
            Header.TextTableLength = (uint)StringBuffer.Length/2;
            dynamic H = Header;
            Tools.BuildStruct(ref H).CopyTo(NewScript, 0);
            NewScript = NewScript.Concat(StringBuffer.ToArray()).ToArray();
            Writer.Close();
            StringBuffer?.Close();
            return NewScript;
        }

        private void Overwrite(ref byte[] Target, byte[] Data, uint At) => Data.CopyTo(Target, At);
        private byte[] GenBC(uint Int1, int Int2) {
            byte[] Rst = new byte[8];
            BitConverter.GetBytes(Int1).CopyTo(Rst, 0);
            BitConverter.GetBytes(Int2).CopyTo(Rst, 4);
            return Rst;
        }

        public long Search(byte[] Script, byte[] Data, uint Start, uint Max) {
            for (uint i = 0; i < Max; i++)
                if (EqualsAt(Script, Data, i))
                    return i;
            throw new Exception("Offset not found.");
        }
        public bool EqualsAt(byte[] Arr1, byte[] Arr2, uint Pos) {
            if (Arr2.Length + Pos >= Arr1.Length)
                return false;
            for (uint i = 0; i < Arr2.Length; i++)
                if (Arr1[i + Pos] != Arr2[i])
                    return false;
            return true;
        }
    }
}
