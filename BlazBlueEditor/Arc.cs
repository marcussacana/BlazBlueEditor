using AdvancedBinary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BlazBlueEditor {
    public static class FPac {

#pragma warning disable 0169, 0649
        internal struct FPacHeader {
            internal uint Signature;
            internal uint BaseOffset;
            internal uint PackgetSize;
            internal uint FilesCount;
            internal int Dummy1;
            internal uint FilesNameLen;
            internal long Dummy2;
        }
        internal struct Entry {
            [CString]
            internal string FileName;
            internal FieldInvoke Dummy;
            internal uint FileIndex;
            internal uint Offset;
            internal uint Length;
            internal FieldInvoke FixPadding;
        }

#pragma warning restore 0169, 0649

        static FPacHeader Header;
        public static File[] Open(Stream Packget) {
            StructReader Reader = new StructReader(Packget, Encoding.UTF8);
            dynamic tmp = new FPacHeader();
            Reader.ReadStruct(ref tmp);
            Header = tmp;
            if (Header.Signature != 0x43415046)
                throw new Exception("Invalid Packget");
            List<Entry> Entries = new List<Entry>();
            for (uint i = 0; i < Header.FilesCount; i++) {
                tmp = new Entry() {
                    Dummy = Dummy,
                    FixPadding = Padding
                };
                Reader.ReadStruct(ref tmp);
                Entries.Add(tmp);
            }
            List<File> Files = new List<File>();
            foreach (Entry Entry in Entries) {
                File FileEntry = new File() {
                    FileName = Entry.FileName,
                    Data = new MemoryStream()
                };
                uint Offset = Entry.Offset + Header.BaseOffset;
                Reader.Seek(Offset, SeekOrigin.Begin);
                byte[] Buffer = new byte[1024 * 1040];
                int TotalReaded = 0;
                int Readed = 0;
                do {
                    if (TotalReaded + Buffer.Length >= Entry.Length)
                        Buffer = new byte[Entry.Length - TotalReaded];
                    Readed = Reader.Read(Buffer, 0, Buffer.Length);
                    TotalReaded += Readed;
                    FileEntry.Data.Write(Buffer, 0, Readed);
                } while (Readed > 0 && TotalReaded < Entry.Length);
                FileEntry.Data.Position = 0;
                Files.Add(FileEntry);
            }
            Reader.Close();
            return Files.ToArray();
        }

        public static void Pack(File[] Files, Stream Output) {
            Header = new FPacHeader() {
                Signature = 0x43415046,
                Dummy1 = 1,
                Dummy2 = 0,
                FilesCount = (uint)Files.Length
            };
            uint Tmp = 0;
            foreach (File File in Files) {
                if (Encoding.UTF8.GetByteCount(File.FileName) > Tmp)
                    Tmp = (uint)Encoding.UTF8.GetByteCount(File.FileName);
            }
            while (Tmp % 4 != 0)
                Tmp++;
            int EntryLen = (int)Tools.GetStructLength(new Entry(), (int)Tmp);
            while (EntryLen % 16 != 0)
                EntryLen++;
            uint DataLength = ((uint)EntryLen*Header.FilesCount) + (uint)Tools.GetStructLength(Header);
            Header.BaseOffset = DataLength;
            foreach (File File in Files) {
                DataLength += (uint)File.Data.Length;
            }
            Header.PackgetSize = DataLength;
            Header.FilesNameLen = Tmp;
            StructWriter Writer = new StructWriter(Output, Encoding.UTF8);
            dynamic obj = Header;
            Writer.WriteStruct(ref obj);

            DataLength = Header.BaseOffset;
            Tmp = 0;
            foreach (File File in Files) {
                dynamic Entry = new Entry() {
                    FileName = File.FileName,
                    Offset = DataLength - Header.BaseOffset,
                    Dummy = Dummy,
                    FileIndex = Tmp++,
                    FixPadding = Padding,
                    Length = (uint)File.Data.Length
                };
                DataLength += (uint)File.Data.Length;
                Writer.WriteStruct(ref Entry);
            }
            DataLength = Header.BaseOffset;
            foreach (File File in Files) {
                int Readed = 0;
                byte[] Buffer = new byte[1024 * 1024];
                do {
                    Readed = File.Data.Read(Buffer, 0, Buffer.Length);
                    Writer.Write(Buffer, 0, Readed);
                } while (Readed > 0);
                File.Data.Close();
            }
            Writer.Close();
            Output?.Close();
        }

        private static FieldInvoke Dummy = new FieldInvoke((Stream stm, bool Read, dynamic inst) => {
            Entry Entry = inst;
            uint Length = (uint)Encoding.UTF8.GetByteCount(Entry.FileName) + 1;
            Length = Header.FilesNameLen - Length;
            while (Length-- > 0)
                if (Read)
                    stm.ReadByte();
                else
                    stm.WriteByte(0x00);
            return Entry;
        });

        private static FieldInvoke Padding = new FieldInvoke((Stream stm, bool Read, dynamic inst) => {
            while (stm.Position % 16 != 0) {
                if (Read)
                    stm.Position++;
                else
                    stm.WriteByte(0x00);
            }
            return inst;
        });
    }

    public struct File {
        public string FileName;
        public Stream Data;
    }
}
