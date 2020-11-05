using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DataMender
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            DirectoryInfo dataDir = new DirectoryInfo("..\\..\\..\\Data");
            List<FileInfo> files = new List<FileInfo>();
            RecurseFolder(dataDir, files);
            foreach(var file in files)
            {
                //G:\Users\baker\source\repos\QCLean\Data\equity\usa\daily
                //e.g. iqa.zip date, open, high, low, close, volume, newer zips are 10/20, older are 10/15

                //G: \Users\baker\source\repos\QCLean\Data\equity\usa\hour
                //skip

                //G:\Users\baker\source\repos\QCLean\Data\equity\usa\minute\lom
                //same as above except datetime is in seconds. 34260000




            }

            foreach(var dd in delets)
            {
                txtOutput.Text += "rmdir " + dd.FullName + Environment.NewLine;
            }
        }
        List<DirectoryInfo> delets = new List<DirectoryInfo>();

        private void RecurseFolder(DirectoryInfo dir, List<FileInfo> files)
        {
            var s = dir.GetFileSystemInfos();
            txtOutput.Text += dir.FullName + ": " + s.Length + Environment.NewLine;
            DateTime prev = DateTime.MinValue;
            if (dir.Name.Length == 3 && dir.Parent.Name == "minute" && dir.Parent.Parent.Name == "usa" && s.Length == 0)
            {
                delets.Add(dir);
            }

            foreach (var sysInfo in s.OrderBy(n => n.Name))
            {
                //txtOutput.Text += sysInfo.FullName + Environment.NewLine;
                if (sysInfo is FileInfo)
                {
                    if (dir.Name.Length == 3 && dir.Parent.Name == "minute" && dir.Parent.Parent.Name == "usa")
                    {
                        var n = sysInfo.Name.Substring(0, 8);
                        DateTime dt = new DateTime(int.Parse(n.Substring(0, 4)), int.Parse(n.Substring(4, 2)), int.Parse(n.Substring(6, 2)));
                        //files.Add(sysInfo as FileInfo);
                        if (dt - prev > TimeSpan.FromDays(4))
                        {
                            txtOutput.Text += dt.ToString() + " - " + prev.ToString() + " = " + (dt - prev).ToString() + Environment.NewLine;
                        }
                        prev = dt;
                    }
                }
                else
                {
                    RecurseFolder(sysInfo as DirectoryInfo, files);
                }
            }
        }
    }
}
