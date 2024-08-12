using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace RawSearch
{
    
    public partial class Form1 : Form
    {
        private IEnumerable<Encoding> encodings = new[]
            {
                Encoding.Unicode,
                Encoding.ASCII,
                Encoding.UTF8,
                Encoding.GetEncoding(1251)
            };

        private string _selectedDirectory = null;

        private int processedCount = 0;
        private List<FileInfo> files = null;
        private List<MatchedFile> matchedFiles = new List<MatchedFile>();

        public Form1()
        {
            InitializeComponent();            
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtSearch.Text))
            {
                MessageBox.Show("Search criteria is empty", "Validation error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (string.IsNullOrEmpty(_selectedDirectory))
            {
                MessageBox.Show("search directory is empty", "Validation error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (files == null)
            {
                DirectoryInfo directory = new DirectoryInfo(_selectedDirectory);
                files = new List<FileInfo>();
                IterateFolder(directory);
                UpdateLabel();
            }

            processedCount = 0;
            progressBar1.Maximum = files.Count;
            listBox1.Items.Clear();

            foreach (var file in files)
            {
                try
                {
                    foreach (var encoding in encodings)
                    {
                        byte[] criteriaBytes = encoding.GetBytes(txtSearch.Text);
                        int criteriaBytesLength = criteriaBytes.Length;

                        using (FileStream filestream = file.OpenRead())
                        {
                            byte[] fileBytes = new byte[filestream.Length];
                            filestream.Read(fileBytes, 0, (int)filestream.Length);

                            int matchedBytes = 0;
                            for (int i = 0; i < fileBytes.Length; i++)
                            {
                                if (fileBytes[i] == criteriaBytes[matchedBytes])
                                {
                                    matchedBytes++;

                                    if (matchedBytes == criteriaBytesLength)
                                    {
                                        var matchedFile = new MatchedFile() { Path = file.FullName, Position = i - matchedBytes + 1 };
                                        matchedFiles.Add(matchedFile);
                                        listBox1.Items.Add(matchedFile.Path + ":" + encoding.EncodingName + ":" + matchedFile.Position);
                                        matchedBytes = 0;
                                    }
                                }
                                else
                                {
                                    matchedBytes = 0;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    listBox1.Items.Add(file.FullName + ": " + ex.Message);
                }
                finally
                {
                    progressBar1.Value = ++processedCount;
                    progressBar1.Invalidate();
                    Application.DoEvents();
                    UpdateLabel();
                }
            }
        }

        private void btnChangeDirectory_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                _selectedDirectory = folderBrowserDialog1.SelectedPath;
                btnChangeDirectory.Text = _selectedDirectory;

                DirectoryInfo directory = new DirectoryInfo(_selectedDirectory);
                files = new List<FileInfo>();
                IterateFolder(directory);
                UpdateLabel();
            }
        }

        private void IterateFolder(DirectoryInfo directory)
        {
            try
            {
                foreach (var subfiles in directory.GetFiles())
                {
                    files.Add(subfiles);
                }
            }
            catch (Exception) { }

            UpdateLabel(true);

            try
            {
                foreach (var subdirectory in directory.GetDirectories())
                {
                    IterateFolder(subdirectory);
                }
            }
            catch (Exception) { }
        }

        private void UpdateLabel(bool isInProcess = false)
        {
            lblStatus.Text = string.Format("{0}/{1}{2}", processedCount, files.Count, isInProcess ? "..." : "");
            lblStatus.Invalidate();
            Application.DoEvents();
        }
    }

    public class MatchedFile
    {
        public string Path { get; set; }
        public int Position { get; set; }
    }

}
