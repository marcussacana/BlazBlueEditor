﻿using BlazBlueEditor;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BBEGui {
    public partial class Form1 : Form {
        public Form1() {
            InitializeComponent();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e) {
            openFileDialog1.ShowDialog();
        }

        ATFStringEditor Editor = null;
        private void openFileDialog1_FileOk(object sender, CancelEventArgs e) {
            listBox1.Items.Clear();
            Editor = new ATFStringEditor(System.IO.File.ReadAllBytes(openFileDialog1.FileName));
            string[] strs = Editor.Import();
            foreach (string str in strs)
                listBox1.Items.Add(str);
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e) {
            saveFileDialog1.ShowDialog();
        }

        private void saveFileDialog1_FileOk(object sender, CancelEventArgs e) {
            List<string> Strings = new List<string>();
            foreach (string item in listBox1.Items)
                Strings.Add(item);
            System.IO.File.WriteAllBytes(saveFileDialog1.FileName, Editor.Export(Strings.ToArray()));
            MessageBox.Show("File Saved.");
        }

        private void String_KeyPress(object sender, KeyPressEventArgs e) {
            if (e.KeyChar == '\n' || e.KeyChar == '\r') {
                int index = listBox1.SelectedIndex;
                listBox1.Items[index] = String.Text;
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e) {
            try {
                int index = listBox1.SelectedIndex;
                Text = "BBEGUI - " + index + "/" + listBox1.Items.Count;
                String.Text = listBox1.Items[index].ToString();
            }
            catch { }
        }

        private void openToolStripMenuItem1_Click(object sender, EventArgs e) {
            OpenFileDialog fd = new OpenFileDialog() {
                Filter = "All FPAC Files|*.pac"
            };
            if (fd.ShowDialog() == DialogResult.OK) {
                BlazBlueEditor.File[] Files = FPac.Open(new StreamReader(fd.FileName).BaseStream);
                string OutDir = fd.FileName + "~";
                if (System.IO.Directory.Exists(OutDir)) {
                    int cnt = 2;
                    while (System.IO.Directory.Exists(OutDir + cnt))
                        cnt++;
                    OutDir += cnt;
                }
                OutDir += '\\';
                StringBuilder List = new StringBuilder();
                System.IO.Directory.CreateDirectory(OutDir);
                int ind = 0;
                foreach (BlazBlueEditor.File File in Files) {
                    List.AppendLine(string.Format("{0}={1}", ind++, File.FileName));
                    Stream Writer = new StreamWriter(OutDir + File.FileName).BaseStream;
                    int Readed = 0;
                    byte[] Buffer = new byte[1024 * 1024];
                    do {
                        Readed = File.Data.Read(Buffer, 0, Buffer.Length);
                        Writer.Write(Buffer, 0, Readed);
                    } while (Readed > 0);
                    Writer.Close();
                    File.Data.Close();
                }
                System.IO.File.WriteAllText(OutDir + "BBE-IndexTree.txt", List.ToString(), Encoding.UTF8);
                MessageBox.Show("Packget Extracted.");
            }
        }

        private void repackToolStripMenuItem_Click(object sender, EventArgs e) {
            FolderBrowserDialog Folder = new FolderBrowserDialog();
            if (Folder.ShowDialog() == DialogResult.OK) {
                SaveFileDialog save = new SaveFileDialog();
                save.Filter = "All Pac Files|*.pac";
                if (save.ShowDialog() == DialogResult.OK) {
                    List<string> Files = new List<string>(Directory.GetFiles(Folder.SelectedPath, "*.*"));
                    int[] Keys = new int[Files.Count - 1];
                    string[] FList = System.IO.File.ReadAllLines(Folder.SelectedPath + "\\BBE-IndexTree.txt", Encoding.UTF8);
                    for (int i = 0; i < Files.Count; i++) {
                        string FN = System.IO.Path.GetFileName(Files[i]);
                        if (FN.ToLower() == "bbe-indextree.txt") {
                            Files.Remove(Files[i--]);
                            continue;
                        }
                        Keys[i] = GetFileIndex(FN, FList);
                    }
                    string[] FArr = Files.ToArray();
                    Array.Sort(Keys, FArr);
                    List<BlazBlueEditor.File> Arr = new List<BlazBlueEditor.File>();
                    foreach (string File in FArr) {
                        Arr.Add(new BlazBlueEditor.File() {
                            FileName = System.IO.Path.GetFileName(File),
                            Data = new StreamReader(File).BaseStream
                        });
                    }
                    FPac.Pack(Arr.ToArray(), new StreamWriter(save.FileName).BaseStream);
                    MessageBox.Show("Packget Created.");
                }
            }
        }
        public int GetFileIndex(string File, string[] IndexTree) {
            string fn = File.ToLower();
            foreach (string tree in IndexTree) {
                int ido = tree.IndexOf("=");
                int ID = int.Parse(tree.Substring(0, ido++));
                string FN = tree.Substring(ido, tree.Length - ido).ToLower();
                if (FN == fn)
                    return ID;
            }
            return -1;
        }
    }
}
