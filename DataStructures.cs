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

    /// <summary>
    /// Changing these numbers will destroy old log files.
    /// </summary>
    public enum ReturnType
    {
        NAN = 0,
        Strongest = 1,
        Last = 2,
        Dual = 3,
    }

    /// <summary>
    /// Changing these numbers will destroy old log files.
    /// </summary>
    public enum VelodyneModel
    {
        NAN = 0,
        VLP_16 = 16,
        HDL_32E = 32,
    }
}
