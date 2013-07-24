using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MGHGame
{
    public struct SphereData
    {
        public VertexPositionColor[] vertices;
        public BoundingSphere boundingSphere;
    }

    public class Sphere
    {
        const int SLICES = 10;
        const int STACKS = 10;
        public List<SphereData> sphere = new List<SphereData>();

        private int numPrimitives = 0;
        public int PrimitiveCount { get { return numPrimitives; } }

        public PrimitiveType primitiveType = PrimitiveType.LineStrip;
        private Color vertexColor = Color.White;

        private bool show;
        public bool Show { get { return show; } }

        public Sphere(bool showSphere)
        {
            show = showSphere;
        }

        public void AddSphere(float radius, Vector3 position, Color color)
        {
            SphereData sphereData = new SphereData();
            sphereData.boundingSphere.Center = position;
            sphereData.boundingSphere.Radius = radius;

            if (show)
            {
                numPrimitives = SLICES * STACKS * 2 - 1;
                SphereVertices sphereVertices = new SphereVertices(color, numPrimitives, position);
                sphereData.vertices = sphereVertices.InitializeSphere(SLICES, STACKS, radius);
            }
            sphere.Add(sphereData);
        }

        public enum Group
        {
            leftWing, rightWing, tail, head,
            plane, bigPlane,
            // wall spheres divided into 4 groups
            wallBackRight, wallBackLeft, wallFrontLeft, wallFrontRight,
            // wall spheres grouped according to location within 4 world quadrants
            worldBackRight, worldBackLeft, worldFrontLeft, worldFrontRight
        }

        const int TOTAL_SPHERE_GROUPS = 14;
        Sphere[] sphereGroup = new Sphere[TOTAL_SPHERE_GROUPS];
        const bool SHOW = true; // set to false during release
    }
}
