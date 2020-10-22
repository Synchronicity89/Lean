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
        }
        private void RecurseFolder(DirectoryInfo dir, List<FileInfo> files)
        {
            foreach (var sysInfo in dir.GetFileSystemInfos())
            {
                txtOutput.Text += sysInfo.FullName + Environment.NewLine;
                if(sysInfo is FileInfo)
                {
                    files.Add(sysInfo as FileInfo);
                }
                else
                {
                    RecurseFolder(sysInfo as DirectoryInfo, files);
                }
            }
        }
    }
}
