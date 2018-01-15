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
    public class VLP_16_Framer
    {
        private IPEndPoint _Sender;

        private float _LastAzimuth;
        private float _LastMeasuredAzimuth;
        private float _AngularVelocity = 0;

        private Action<Frame> _FramePop;

        /// <summary>
        /// Azimuth, Data
        /// </summary>
        private List<Tuple<float, VLP_16.SinglePoint[]>> _List = new List<Tuple<float, VLP_16.SinglePoint[]>>();

        /// <summary>
        /// Throws a socket exception only on initialization.  Once everything is up and running exceptions are handled internally.
        /// </summary>
        /// <param name="d"></param>
        /// <param name="initialization_exception"></param>
        /// <param name="frame_pop"></param>
        /// <param name="should_cancel_async">Called at 1 Hz</param>
        public VLP_16_Framer(
            IPEndPoint d,
            Action<Frame> frame_pop = null,
            VLP_16.ShouldCancel should_cancel_async = null)
        {
            this._FramePop = frame_pop;

            new VLP_16(
                d,
                this.RecievePacket,
                should_cancel_async);
        }

        private void RecieveBlocks(VLP_16.SinglePoint[] point16)
        {            
            this.RecieveBlocks(
                point16, 
                this._LastMeasuredAzimuth + this._AngularVelocity * 0.5f, // Assume half way in between others!
                true);
        }

        private void RecieveBlocks(VLP_16.SinglePoint[] point16, float azimuth, bool interpolated = false)
        {
            // azimuth can vary from 0 to a little more than 360 (in the case of interpolated points)

            if (!interpolated)
            {
                float change = azimuth - this._LastMeasuredAzimuth;
                if (change < 0) change += 360;
                this._AngularVelocity = 0.9f * this._AngularVelocity + 0.1f * change;
                this._LastMeasuredAzimuth = azimuth;
            }

            // Split frames directly behind front
            if ((this._LastAzimuth <= 180) && (azimuth > 180)) // Flip
            {
                this.CompletedRevolution();
            }

            this._List.Add(new Tuple<float, VLP_16.SinglePoint[]>(azimuth, point16));

            this._LastAzimuth = azimuth;
        }

        private void CompletedRevolution()
        {
            if (this._FramePop != null)
            {
                const int lasers = 16;

                var frame = new Frame(lasers, this._List.Count);

                for (int index = 0; index < this._List.Count; index++)
                {
                    var tup = this._List[index];

                    float angle_radians = SamSeifert.Utilities.UnitConverter.DegreesToRadians(tup.Item1);
                    float sin = (float)Math.Sin(angle_radians);
                    float cos = (float)Math.Cos(angle_radians);

                    for (int l = 0; l < lasers; l++)
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

                this._FramePop(frame);
            }
            this._List.Clear(); // Keep Internal Length

        }

        private void RecievePacket(VLP_16.Packet pack, IPEndPoint sender)
        {
            if (this._Sender == null) this._Sender = sender;
            else if (this._Sender.Equals(sender))
            {
                foreach (var blck in pack._Blocks)
                {
                    this.RecieveBlocks(blck._ChannelData.SubArray(0, 16), blck._Azimuth);
                    this.RecieveBlocks(blck._ChannelData.SubArray(0, 16)); // interpolate azimuth
                }
            }
            else
            {
                this._Sender = sender;
                throw new Exception("Multiple sender IPEndPoint's");
            }
        }
    }
}