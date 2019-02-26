using SamSeifert.Utilities; using SamSeifert.Utilities.Maths;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SamSeifert.Velodyne
{
    /// <summary>
    /// Return true if the thread should shut down (stop listenting).  Return false to continue listening.
    /// </summary>
    /// <param name="a"></param>
    /// <returns></returns>
    public delegate bool ShouldCancel(UpdateArgs a);

    public struct UpdateArgs
    {
        public int PacketsReceivedCorrectly;
        public int PacketsReceivedIncorrectly;
        public int Timeouts;
        public int SocketErrors;

        public override string ToString()
        {
            return
                "Packets Recieved Correctly: " + this.PacketsReceivedCorrectly + ", " +
                "Packets Recieved Incorrectly: " + this.PacketsReceivedIncorrectly + ", " +
                "Socket Errors: " + this.SocketErrors + ", " +
                "Timeouts: " + this.Timeouts;
        }
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

    public static class Parse
    {
        private static HashSet<int> _BadReturnTypes = new HashSet<int>();
        private static HashSet<int> _BadLidarTypes = new HashSet<int>();

        public static ReturnType ReturnType_(int rt)
        {
            switch (rt)
            {
                case 0x37:
                    return ReturnType.Strongest;
                case 0x38:
                    return ReturnType.Last;
                case 0x39:
                    return ReturnType.Dual;
                default:
                    lock (_BadReturnTypes)
                        if (!_BadReturnTypes.Contains(rt))
                        {
                            _BadReturnTypes.Add(rt);
                            Logger.WriteWarning(typeof(ReturnType), "Unrecognized ReturnType: " + rt);
                        }
                    return ReturnType.NAN;
            }
        }

        public static VelodyneModel VelodyneModel_(int lt)
        {
            switch (lt)
            {
                case 0x21:
                    return VelodyneModel.HDL_32E;
                case 0x22:
                    return VelodyneModel.VLP_16;
                default:
                    lock (_BadReturnTypes)
                        if (!_BadLidarTypes.Contains(lt))
                        {
                            _BadLidarTypes.Add(lt);
                            Logger.WriteWarning(typeof(VelodyneModel), "Unrecognized VelodyneModel: " + lt);
                        }
                    return VelodyneModel.NAN;
            }
        }
    }
}
