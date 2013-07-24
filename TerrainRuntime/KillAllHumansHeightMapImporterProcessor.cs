using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

namespace KillAllHumansHeightMapImporterTerrainRuntime
{
    public class Terrain
    {
        // these variables store values that are accessible in game class
        public byte[] height;
        public Vector3[] position;
        public Vector3[] normal;
        public int NUM_ROWS, NUM_COLS;
        public float worldWidth, worldHeight, heightScale;
        public float cellWidth, cellHeight;

        // return position data to game class
        public float PositionX(int index) { return position[index].X; }
        public float PositionY(int index) { return position[index].Y; }
        public float PositionZ(int index) { return position[index].Z; }

        // return normal data to game class
        public float NormalX(int index) { return normal[index].X; }
        public float NormalY(int index) { return normal[index].Y; }
        public float NormalZ(int index) { return normal[index].Z; }

        internal Terrain(ContentReader cr)
        {
            NUM_ROWS = cr.ReadInt32();
            NUM_COLS = cr.ReadInt32();
            worldWidth = cr.ReadSingle();
            worldHeight = cr.ReadSingle();
            heightScale = cr.ReadSingle();
            cellWidth = cr.ReadSingle();
            cellHeight = cr.ReadSingle();

            // declare position and normal vector arrays
            position = new Vector3[NUM_ROWS * NUM_COLS];
            normal = new Vector3[NUM_ROWS * NUM_COLS];

            // read in position and normal data to generate height map
            for (int row = 0; row < NUM_ROWS; row++)
            {
                for (int col = 0; col < NUM_COLS; col++)
                {
                    position[col + row * NUM_COLS] = cr.ReadVector3();
                    normal[col + row * NUM_COLS] = cr.ReadVector3();
                }
            }
        }
    }

    // loads terrain from an XNB file.
    public class TerrainReader : ContentTypeReader<Terrain>
    {
        protected override Terrain Read(ContentReader input, Terrain existingInstance)
        {
            return new Terrain(input);
        }
    }
}