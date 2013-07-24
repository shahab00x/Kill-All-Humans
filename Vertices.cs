using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MGHGame
{
    class Vertices
    {

    }

    public class SphereVertices
    {
        private Color color;
        private int numPrimitives;
        private Vector3 offset = Vector3.Zero;

        public SphereVertices(Color vertexColor, int totalPrimitives, Vector3 position)
        {
            offset = position;
            color = vertexColor;
            numPrimitives = totalPrimitives;
        }

        public VertexPositionColor[] InitializeSphere(int numSlices, int numStacks, float radius)
        {
            Vector3[] position = new Vector3[(numSlices + 1) * (numStacks + 1)];

            float angleX, angleY;
            float rowHeight = MathHelper.Pi / numStacks;
            float colWidth = MathHelper.TwoPi / numSlices;
            float X, Y, Z, W;

            // generate horizontal rows (stacks in sphere)
            for (int stacks = 0; stacks <= numStacks; stacks++)
            {
                angleX = MathHelper.PiOver2 - stacks * rowHeight;
                Y = radius * (float)Math.Sin(angleX);
                W = -radius * (float)Math.Cos(angleX);

                // generate vertical columns (slices in sphere)
                for (int slices = 0; slices <= numSlices; slices++)
                {
                    angleY = slices * colWidth;
                    X = W * (float)Math.Sin(angleY);
                    Z = W * (float)Math.Cos(angleY);

                    // position sphere vertices at offset from origin
                    position[stacks * numSlices + slices] = new Vector3(X + offset.X, Y + offset.Y, Z + offset.Z);
                }
            }

            int i = -1;

            VertexPositionColor[] vertices = new VertexPositionColor[2 * numSlices * numStacks];
            // index vertices to draw sphere
            for (int stacks = 0; stacks < numStacks; stacks++)
            {
                for (int slices = 0; slices < numSlices; slices++)
                {
                    vertices[++i] = new VertexPositionColor(position[stacks * numSlices + slices], color);
                    vertices[++i] = new VertexPositionColor(position[(stacks + 1) * numSlices + slices],
                        color);
                }
            }
            return vertices;
        }
    }
}
