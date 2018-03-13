using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using SamSeifert.Utilities;
using SamSeifert.Utilities.Extensions;

namespace SamSeifert.Velodyne
{
    /// <summary>
    /// Velodyne's spit back data constantly.  If you'd like to manually handle each parsed packet, use the VLP_16 Class (static Listen Method).  If you only want to handle data 
    /// every time the sensor makes a complete revolution, use this class (static Listen method).
    /// </summary>
    public class VLP_16_Framer
    {
        public delegate void FrameRecieved(Frame f, IPEndPoint velodyne_ip);

        private IPEndPoint _VelodynesIP = null;

        private float _LastAzimuth;
        private float _LastMeasuredAzimuth;
        /// <summary>
        /// Maintain an estimate of the sensor angular velocity to interpolate data that doesn't have a measured azimuth (every other point)
        /// </summary>
        private float _AngularVelocity = 0;

        /// <summary>
        /// Azimuth, Data
        /// </summary>
        private readonly List<Tuple<float, VLP_16.SinglePoint[]>> _List = new List<Tuple<float, VLP_16.SinglePoint[]>>();

        private FrameRecieved _NewFrameCallback;

        /// <summary>
        /// Throws a socket exception only on initialization.  Once everything is up and running exceptions are handled internally.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="new_frame_callback"></param>
        /// <param name="should_cancel_callback">Called at 1 Hz</param>
        public static void Listen(
            IPEndPoint endpoint,
            FrameRecieved new_frame_callback = null,
            ShouldCancel should_cancel_callback = null)
        {
            var framer = new VLP_16_Framer(new_frame_callback);
            VLP_16.Listen(
                endpoint,
                framer.RecievePacket,
                should_cancel_callback);
        }

        private VLP_16_Framer(FrameRecieved new_frame_callback)
        {
            this._NewFrameCallback = new_frame_callback;
        }

        private void RecievePacket(VLP_16.Packet pack, IPEndPoint velodyne_ip)
        {
            if (this._VelodynesIP == null) this._VelodynesIP = velodyne_ip;
            else if (this._VelodynesIP.Equals(velodyne_ip))
            {
                foreach (var blck in pack._Blocks)
                {
                    this.RecieveBlocks(velodyne_ip, blck._ChannelData.SubArray(0, 16), blck._Azimuth);
                    this.RecieveBlocks(velodyne_ip, blck._ChannelData.SubArray(16, 16)); // interpolate azimuth
                }
            }
            else
            {
                this._VelodynesIP = velodyne_ip;
                throw new Exception("Multiple sender IPEndPoint's " + this._VelodynesIP.ToString() + " " + velodyne_ip.ToString());
            }
        }

        private void RecieveBlocks(IPEndPoint sender, VLP_16.SinglePoint[] point16)
        {            
            this.RecieveBlocks(
                sender,
                point16, 
                this._LastMeasuredAzimuth + this._AngularVelocity * 0.5f, // Assume half way in between others!
                true);
        }

        private void RecieveBlocks(IPEndPoint sender, VLP_16.SinglePoint[] point16, float azimuth, bool interpolated = false)
        {
            // azimuth can vary from 0 to a little more than 360 (in the case of interpolated points)

            if (!interpolated)
            {
                float change = azimuth - this._LastMeasuredAzimuth;
                if (change < 0) change += 360;
                this._AngularVelocity = 0.9f * this._AngularVelocity + 0.1f * change; // Low pass filter velocity
                this._LastMeasuredAzimuth = azimuth;
            }

            // Split frames directly behind front of velodyne sensor
            if ((this._LastAzimuth <= 180) && (azimuth > 180)) // Flip
                this.CompletedRevolution(sender);

            this._List.Add(new Tuple<float, VLP_16.SinglePoint[]>(azimuth, point16));
            this._LastAzimuth = azimuth;
        }

        private void CompletedRevolution(IPEndPoint ip)
        {
            if (this._NewFrameCallback != null)
            {
                var frame = new Frame(VLP_16._Lasers, this._List.Count);

                for (int index = 0; index < this._List.Count; index++)
                {
                    var tup = this._List[index];

                    float angle_radians = UnitConverter.DegreesToRadians(tup.Item1);
                    float sin = (float)Math.Sin(angle_radians);
                    float cos = (float)Math.Cos(angle_radians);

                    for (int l = 0; l < VLP_16._Lasers; l++)
                    {
                        VLP_16.SinglePoint rd = tup.Item2[l];

                        float nz = VLP_16.LaserPitchSin[l];
                        float nxy = VLP_16.LaserPitchCos[l];

                        float nx = cos * nxy;
                        float ny = -sin * nxy;

                        frame.Fill(
                            l, // Laser Index 
                            index, // Width Index
                            nx, // Normal X
                            ny, // Normal Y
                            nz, // Normal Z
                            rd._DistanceMeters,
                            rd._Reflectivity
                            );
                    }
                }

                this._NewFrameCallback(frame, ip);
            }
            this._List.Clear();
        }
    }
}