using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using SamSeifert.Utilities; using SamSeifert.Utilities.Maths;
using SamSeifert.Utilities.Files.Json;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using SamSeifert.Utilities.Extensions;
using SamSeifert.Utilities.Maths;

namespace SamSeifert.Velodyne
{
    /// <summary>
    /// Velodyne's spit back data constantly.  If you'd like to manually handle each parsed packet, use this class (static Listen method).  If you only want to handle data 
    /// every time the sensor makes a complete revolution, use the VLP_16_Framer Class (static Listen method).
    /// </summary>
    public static class VLP_16
    {
        public delegate void PacketRecieved(Packet p, IPEndPoint velodyne_ip);

        /// <summary>
        /// This method won't return untill listener is done (canceled manually, or error).
        /// It returns the raw data packets sent by the VLP-16
        /// Throws a socket exception only on initialization.  Once everything is up and running exceptions are handled internally.
        /// </summary>
        /// <param name="endpoint">Where to listen for data</param>
        /// <param name="packet_recieved_callback">New data gets parsed, sent to this method.</param>
        /// <param name="should_cancel_callback">Hearbeat function, called at 1 Hz, can stop the velodyne listener</param>
        public static void Listen(
            IPEndPoint endpoint,
            PacketRecieved packet_recieved_callback = null,
            ShouldCancel should_cancel_callback = null)
        {
            UdpClient cl = null;
            try
            {
                cl = new UdpClient(new IPEndPoint(IPAddress.Any, endpoint.Port));
                cl.Client.ReceiveTimeout = 250; // .25 Seconds
                cl.Client.ReceiveBufferSize = 4096;

                Listen_(cl, packet_recieved_callback, should_cancel_callback); // This will only end when canceled
            }
            finally
            {
                if (cl != null)
                {
                    cl.Dispose();
                }
            }
        }

        private static unsafe void Listen_(
            UdpClient cl,
            PacketRecieved packet_recieved_callback,
            ShouldCancel should_cancel_callback)
        {
            DateTime start = DateTime.Now;
            int elapsed_seconds = 0;

            int corrects = 0;
            int incorrects = 0;
            int timeouts = 0;
            int socketerrors = 0;

            while (true)
            {
                try
                {
                    IPEndPoint iep = null;
                    var data = cl.Receive(ref iep);

                    if (data.Length == Packet.Raw._Size)
                    {
                        Packet p;
                        bool valid;

                        fixed (byte* b = data)
                            p = new Packet(b, out valid);

                        if (valid)
                        {
                            corrects++;
                            packet_recieved_callback?.Invoke(p, iep);
                        }
                        else
                        {
                            incorrects--;
                        }
                    }
                    else
                    {
                        incorrects++;
                    }
                }
                catch (TimeoutException)
                {
                    timeouts++;
                }
                catch (SocketException)
                {
                    socketerrors++;
                }
                catch (Exception e)
                {
                    Logger.WriteException(typeof(VLP_16), "Listen", e);
                    incorrects++;
                }

                var now = DateTime.Now;
                if ((now - start).TotalSeconds > elapsed_seconds)
                {
                    elapsed_seconds++;

                    var st = new UpdateArgs();

                    st.Timeouts = timeouts;
                    st.PacketsReceivedCorrectly = corrects;
                    st.PacketsReceivedIncorrectly = incorrects;
                    st.SocketErrors = socketerrors;

                    timeouts = 0;
                    corrects = 0;
                    incorrects = 0;
                    socketerrors = 0;

                    if (should_cancel_callback != null)
                        if (should_cancel_callback(st))
                            return;
                }
            }
        }

        public const int _Lasers = 16;
        public static readonly float[] LaserPitchSin; // Do this map once and hopefully save time!
        public static readonly float[] LaserPitchCos;
        public static readonly float[] LaserPitch = new float[_Lasers] // Taken from datasheet
        {
            -15,
            1,
            -13,
            3,
            -11,
            5,
            -9,
            7,
            -7,
            9,
            -5,
            11,
            -3,
            13,
            -1,
            15                
        };

        static VLP_16()
        {
            int lens = LaserPitch.Length;
            LaserPitchCos = new float[lens];
            LaserPitchSin = new float[lens];
            for (int i = 0; i < lens; i++)
            {
                float angle_radians = UnitConverter.DegreesToRadians(LaserPitch[i]);
                LaserPitchCos[i] = (float)Math.Cos(angle_radians);
                LaserPitchSin[i] = (float)Math.Sin(angle_radians);
            }
        }

        #region Data Structures Taken From Data Sheet
        public class Packet : JsonPackable
        {
            public VerticalBlockPair[] _Blocks { get; private set; }
            public uint _Time { get; private set; }
            public VelodyneModel _VelodyneModel { get; private set; }
            public ReturnType _ReturnType { get; private set; }

            public JsonDict Pack()
            {
                var of_the_jedi = new JsonDict();

                of_the_jedi["Time"] = this._Time;
                of_the_jedi["VelodyneModel"] = (int)this._VelodyneModel;
                of_the_jedi["ReturnType"] = (int)this._ReturnType;
                of_the_jedi["Blocks"] = Array.ConvertAll(this._Blocks, item => (object)item.Pack());

                return of_the_jedi;
            }

            public void Unpack(JsonDict dict)
            {
                // All numeric entities will be doubles
                // Round and cast them
                this._Time = (uint)Math.Round((double)dict["Time"]);
                this._VelodyneModel = (VelodyneModel)(int)Math.Round((double)dict["VelodyneModel"]);
                this._ReturnType = (ReturnType)(int)Math.Round((double)dict["ReturnType"]);
                this._Blocks = Array.ConvertAll(dict["Blocks"] as object[], item => new VerticalBlockPair(item as JsonDict));
            }

            public Packet(JsonDict d)
            {
                this.Unpack(d);
            }

            internal unsafe Packet(Byte* b, out bool valid)
            {
                Raw r = *(Raw*)b;

                this._Time = 0;
                for (int i = 4; i > 0; i--)
                    this._Time = (this._Time << 8) | r._TimeStamp[i - 1];

                this._ReturnType = Parse.ReturnType_(r._Factory[0]);
                this._VelodyneModel = Parse.VelodyneModel_(r._Factory[1]);

                valid = true;

                this._Blocks = new VerticalBlockPair[Raw._VerticalBlockPairsPerPacket];

                for (int i = 0, byte_index = 0;
                    i < Raw._VerticalBlockPairsPerPacket;
                    i++, byte_index += VerticalBlockPair.Raw._Size)
                {
                    bool bb;
                    this._Blocks[i] = new VerticalBlockPair(&r._DataBlocks[byte_index], out bb);
                    valid &= bb;
                }
            }

            [StructLayout(LayoutKind.Explicit, Pack = 1)]
            internal unsafe struct Raw
            {
                /// <summary>
                /// Size of the structure in bytes
                /// </summary>
                public const int _Size = _FactoryE;
                public const int _VerticalBlockPairsPerPacket = 12;

                // private const int _HeaderE = 42; // UDP Will Filter This Heading OUT
                private const int _DataBlockE = VerticalBlockPair.Raw._Size * _VerticalBlockPairsPerPacket; // index after data block E(nd)
                private const int _TimeStampE = 4 + _DataBlockE; // index after time stamp E(nd)
                private const int _FactoryE = 2 + _TimeStampE; // index after factory E(nd)

                // [FieldOffset(0)]
                // public fixed byte _Header[42]; // UDP Will Filter This Heading OUT

                [FieldOffset(0)]
                public fixed byte _DataBlocks[VerticalBlockPair.Raw._Size * _VerticalBlockPairsPerPacket];

                [FieldOffset(_DataBlockE)]
                public fixed byte _TimeStamp[4];

                [FieldOffset(_TimeStampE)]
                public fixed byte _Factory[2];
            };
        }

        public struct VerticalBlockPair : JsonPackable
        {
            public float _Azimuth { get; private set; }
            public SinglePoint[] _ChannelData { get; private set; }

            internal unsafe VerticalBlockPair(Byte* b, out bool valid)
            {
                Raw r = *(Raw*)b;

                valid = (r._Header[0] == 0xFF) && (r._Header[1] == 0xEE);

                this._Azimuth = ((r._Azimuth[1] << 8) | (r._Azimuth[0])) / 100.0f;

                this._ChannelData = new SinglePoint[Raw._SinglePointsPerVerticalBlock];

                for (int i = 0; i < Raw._SinglePointsPerVerticalBlock; i++)
                    this._ChannelData[i] = new SinglePoint(&r._ChannelData[i * SinglePoint.Raw._Size]);
            }

            internal VerticalBlockPair(JsonDict dict)
            {
                this._Azimuth = 0;
                this._ChannelData = null;
                this.Unpack(dict);
            }

            public JsonDict Pack()
            {
                var of_the_jedi = new JsonDict();
                of_the_jedi["Azimuth"] = this._Azimuth;
                of_the_jedi["Length"] = this._ChannelData.Length;
                of_the_jedi["Distances"] = Array.ConvertAll(this._ChannelData, item => (object)item._DistanceMeters);
                of_the_jedi["Reflectivities"] = Array.ConvertAll(this._ChannelData, item => (object)item._Reflectivity);
                return of_the_jedi;
            }

            public void Unpack(JsonDict dict)
            {
                this._Azimuth = dict.asFloat("Azimuth");
                int lens = dict.asInt("Length");
                var distances = dict["Distances"] as object[];
                var reflectivities = dict["Reflectivities"] as object[];
                this._ChannelData = new SinglePoint[lens];
                this._ChannelData.Fill(i => new SinglePoint(
                        (float)(double)distances[i],
                        (byte)Math.Round((double)reflectivities[i])));
            }

            [StructLayout(LayoutKind.Explicit, Pack = 1)]
            internal unsafe struct Raw
            {
                /// <summary>
                /// Size of the structure in bytes
                /// </summary>
                public const int _Size = _ChannelDataE;
                public const int _SinglePointsPerVerticalBlock = 32;

                private const int _HeaderE = 2; // index after header E(nd)
                private const int _AzimuthE = 2 + _HeaderE; // index after azimuth E(nd)
                private const int _ChannelDataE = SinglePoint.Raw._Size * _SinglePointsPerVerticalBlock + _AzimuthE; // index after channel data E(nd)

                [FieldOffset(0)]
                public fixed byte _Header[2];

                [FieldOffset(_HeaderE)]
                public fixed byte _Azimuth[2];

                [FieldOffset(_AzimuthE)]
                public fixed byte _ChannelData[SinglePoint.Raw._Size * _SinglePointsPerVerticalBlock];
            }
        }

        [Serializable]
        public struct SinglePoint
        {
            public readonly float _DistanceMeters;
            public readonly byte _Reflectivity;

            internal unsafe SinglePoint(Byte* b)
            {
                Raw r = *(Raw*)b;

                // Return Azimuth
                this._DistanceMeters = ((r._Distance[1] << 8) | (r._Distance[0])) * 0.002f; // 2 MM increments
                this._Reflectivity = r._Reflectivity;
            }

            internal SinglePoint(float distance_meters, byte reflectivity)
            {
                this._DistanceMeters = distance_meters;
                this._Reflectivity = reflectivity;
            }            

            [StructLayout(LayoutKind.Explicit, Pack = 1)]
            internal unsafe struct Raw
            {
                /// <summary>
                /// Size of the structure in bytes
                /// </summary>
                public const int _Size = 3;

                [FieldOffset(0)]
                public fixed byte _Distance[2];

                [FieldOffset(2)]
                public byte _Reflectivity;
            };
        }
        #endregion
    }
}
