using SamSeifert.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SamSeifert.Velodyne
{
    public struct UpdateArgs
    {
        public int PacketsReceivedCorrectly;
        public int PacketsReceivedIncorrectly;
        public int Timeouts;
        public int SocketErrors;
    }

    public enum ReturnType
    {
        Strongest,
        Last,
        Dual,
        NAN,
    }

    public enum LidarType
    {
        HDL_32E,
        VLP_16,
        NAN,
    }
}
