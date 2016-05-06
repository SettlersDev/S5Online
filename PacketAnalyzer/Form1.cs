using S5GameServices;
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

namespace PacketAnalyzer
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            this.AllowDrop = true;
            this.DragEnter += new DragEventHandler(Form1_DragEnter);
            this.DragDrop += new DragEventHandler(Form1_DragDrop);
            tbOutput.Text = "Capture single TCP-frames: In Wireshark select the frame, right-click on Data, select 'Export Packet Bytes'" + Environment.NewLine
                          + "Capture a TCP stream: In Wireshark, right-click on a frame, Follow > TCP-Stream, Save as .yaml";
        }
        void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        void Form1_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            string data = "";

            Array.Sort(files);
            foreach (string file in files)
            {
                var parts = Path.GetFileName(file).Split('.');
                if (parts.Contains("cdk"))
                    data += ProcessYamlStream(file, true);
                else if (parts.Contains("yaml"))
                    data += ProcessYamlStream(file, false);
                else
                    data += ProcessBinFile(file);

                data += Environment.NewLine;
            }

            tbOutput.Text = data;
        }

        string ProcessBinFile(string file)
        {
            string info = file + Environment.NewLine;
            try
            {
                var data = File.ReadAllBytes(file);
                var msgList = S5GameServices.Message.ParseIncoming(data);
                foreach (var msg in msgList)
                    info += msg.ToString() + Environment.NewLine + Environment.NewLine;
            }
            catch (Exception e)
            {
                info += e.ToString();
            }

            return info;
        }

        string ProcessYamlStream(string file, bool cdKey)
        {
            string info = file + Environment.NewLine;
            try
            {
                var data = File.ReadAllText(file);
                var plist = data.Split(new[] { "peer" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var pd in plist)
                {
                    if (pd[0] == '#')
                        continue;
                    var dir = pd[0] == '0' ? "GAM " : "SRV ";
                    var b64Data = pd.Split('|')[1].Split('#')[0].Trim().Replace("\n", "");
                    var peerData = Convert.FromBase64String(b64Data);
                    try
                    {
                        if (cdKey)
                        {
                            var msg = new CDKeyMessage(peerData);
                            info += dir + msg.ToString() + Environment.NewLine;
                        }
                        else
                        {
                            var msgList = S5GameServices.Message.ParseIncoming(peerData);

                            foreach (var msg in msgList)
                                info += dir + msg.ToString() + Environment.NewLine;
                        }
                    }
                    catch (Exception ex) { info += dir + "!! EXECPTION: " + ex.Message + Environment.NewLine; }
                }
            }
            catch (Exception e)
            {
                info += e.ToString();
            }

            return info;
        }
    }
}
