using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SamSeifert.Velodyne
{
    /// <summary>
    /// Represents all data collected on a single rotation of the laser column.  
    /// </summary>
    public class Frame
    {
        /// <summary>
        /// This is in AAVS Sensor Frame.  X is Forward, Y is Left, Z is up.
        /// </summary>
        public readonly float[,,] _Normals;
        /// <summary>
        /// Meters
        /// </summary>
        public readonly float[,] _Distances;
        public readonly byte[,] _Reflectiveness;
        public readonly int _Lasers;
        public readonly int _Length;

        public Frame(int lasers, int length)
        {
            this._Normals = new float[lasers, length, 3];
            this._Distances = new float[lasers, length];
            this._Reflectiveness = new byte[lasers, length];
            this._Lasers = lasers;
            this._Length = length;
        }

        internal void Fill(
            int laser_index,
            int length_index,
            float nx,
            float ny,
            float nz,
            float distance,
            byte reflectiveness)
        {
            this._Normals[laser_index, length_index, 0] = nx;
            this._Normals[laser_index, length_index, 1] = ny;
            this._Normals[laser_index, length_index, 2] = nz;
            this._Distances[laser_index, length_index] = distance;
            this._Reflectiveness[laser_index, length_index] = reflectiveness;
        }
    }
}
