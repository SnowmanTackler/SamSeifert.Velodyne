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

namespace SamSeifert.Velodyne
{
    internal partial class Form_VLP_16 : Form
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form_VLP_16());
        }

        public Form_VLP_16()
        {
            InitializeComponent();
        }

        private unsafe void Form1_Load(object sender, EventArgs e)
        {
            Console.WriteLine("VLP_16.Packet: " + VLP_16.Packet.Raw._Size + ", " + sizeof(VLP_16.Packet.Raw));
            Console.WriteLine("VLP_16.VerticalBlockPair: " + VLP_16.VerticalBlockPair.Raw._Size + ", " + sizeof(VLP_16.VerticalBlockPair.Raw));
            Console.WriteLine("VLP_16.SinglePoint: " + VLP_16.SinglePoint.Raw._Size + ", " + sizeof(VLP_16.SinglePoint.Raw));

            this.backgroundWorker1.RunWorkerAsync();
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            var end = new IPEndPoint(IPAddress.Parse("192.168.1.1"), 2368);

            try
            {
                // new VLP_16_Framer(end, null, this.ShouldStopAsync);
                new VLP_16(end, null, this.ShouldStopAsync);
            }
            catch (Exception initalization_exception)
            {
                Console.WriteLine("Initialization Error: " + initalization_exception.ToString());
            }
        }

        private bool ShouldStopAsync(UpdateArgs ua)
        {
            Console.WriteLine(
                ua.PacketsReceivedCorrectly + " " + 
                ua.PacketsReceivedIncorrectly + " " + 
                ua.SocketErrors + " " +
                ua.Timeouts);

            return false;
        }
    }
}
