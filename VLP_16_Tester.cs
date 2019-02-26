using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Net;
using SamSeifert.Utilities;
using SamSeifert.Utilities.Extensions;

namespace SamSeifert.Velodyne
{
    internal partial class VLP_16_Tester : Form
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new VLP_16_Tester());
        }

        public VLP_16_Tester()
        {
            InitializeComponent();
        }

        private unsafe void Form1_Load(object sender, EventArgs e)
        {
            if (!ModifierKeys.HasFlag(Keys.Control)) // Set to default position!
                this.LoadFormState();

            this.textBox1.UseAsLogger(() => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));

            /*
            Logger.WriteLine("VLP_16.Packet: " + VLP_16.Packet.Raw._Size + ", " + sizeof(VLP_16.Packet.Raw));
            Logger.WriteLine("VLP_16.VerticalBlockPair: " + VLP_16.VerticalBlockPair.Raw._Size + ", " + sizeof(VLP_16.VerticalBlockPair.Raw));
            Logger.WriteLine("VLP_16.SinglePoint: " + VLP_16.SinglePoint.Raw._Size + ", " + sizeof(VLP_16.SinglePoint.Raw));
            */

            this.backgroundWorker1.RunWorkerAsync();
        }

        private void VLP_16_Tester_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.SaveFormState();
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            var end = new IPEndPoint(IPAddress.Parse("192.167.1.2"), 2368);

            Logger.WriteLine();
            Logger.WriteLine("Listening for VLP_16 on " + end.ToString());
            Logger.WriteLine();

            try
            {
                // VLP_16 will callback with live data ASAP, VLP_16_Framer will callback with data once per laser column revoltuion
                if (true)  VLP_16.Listen(end, this.PacketRecievedAsync, this.ShouldStopAsync);
                else VLP_16_Framer.Listen(end, this.FrameRecievedAsync, this.ShouldStopAsync);
            }
            catch (Exception initalization_exception)
            {
                Logger.WriteException(this, "Initialization", initalization_exception);
            }
        }



        private void PacketRecievedAsync(VLP_16.Packet p, IPEndPoint velodyne_ip)
        {

        }

        private void FrameRecievedAsync(Frame f, IPEndPoint velodyne_ip)
        {
            Logger.WriteLine("New Frame From:" + velodyne_ip.ToString());
        }

        private bool ShouldStopAsync(UpdateArgs ua)
        {
            Logger.WriteLine(ua.ToString());
            return false;
        }
    }
}
