//------------------------------------------------------------
// Microsoft® XNA Game Studio Creator's Guide, Second Edition
// by Stephen Cawood and Pat McGee 
// Copyright (c) McGraw-Hill/Osborne. All rights reserved.
// https://www.mhprofessional.com/product.php?isbn=0071614060
//------------------------------------------------------------
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
namespace CameraViewer
{
    public class Camera 
    {
        public  Vector3 position, view, up;
        public  Matrix  projectionMatrix, viewMatrix;
        private float   timeLapse = 0.0f;

        /// <summary>
        /// Default constructor for camera object. Set up camera vectors.
        /// </summary>
        public          Camera()
        {
            position              = new Vector3(0.0f, 0.9f, 0.0f);
            view                  = new Vector3(0.0f, 0.9f, -0.5f);
            up                    = new Vector3(0.0f, 1.0f, 0.0f);
        }

        /// <summary>
        /// Sets time lapse between frames
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public void SetFrameInterval(GameTime gameTime)
        {
            timeLapse  = (float)gameTime.ElapsedGameTime.Milliseconds;
        }

        /// <summary>
        /// Define how objects are placed in our world relative to camera direction.
        /// </summary>
        /// <param name="viewChange">Changes to view in terms of X and Z.</param>
        public void SetView(Vector2 viewChange)
        {
            ChangeView(viewChange.X, viewChange.Y);
            viewMatrix = Matrix.CreateLookAt(position, view, up);
        }

        /// <summary>
        /// Define cone of visibility.  Set fov, aspect ratio, near clip / far clip
        /// </summary>
        /// <param name="windowWidth">Viewport width.</param>
        /// <param name="windowHeight">Viewport height.</param>
        public void SetProjection(int windowWidth, int windowHeight)
        { 
            // parameters are field of view, width/height, near clip, far clip
            projectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.Pi/4.0f,
                               (float)windowWidth/(float)windowHeight, 0.005f, 1000.0f);
        }

        /// <summary>
        /// Move camera and view position according to gamer's move and strafe events.
        /// </summary>
        /// <param name="direction">Direction taken from 'forward' vector for 'move' 
        ///                         and 'right' vector for 'strafe'.</param>
        /// <param name="speed">Rate of change to view and position.</param>
        public void UpdatePositionAndView(Vector3 direction, float speed)
        {
            speed      *= (float)timeLapse; // scale rate of change
            direction  *= speed;

            position.X += direction.X;      // adjust position 
            position.Z += direction.Z;

            view.X     += direction.X;      // adjust view change
            view.Z     += direction.Z;
        }

        /// <summary>
        /// Move camera position and view position forwards and backwards.
        /// </summary>
        /// <param name="speed">Rate of change in movmement.
        ///                     Positive is forwards and negative is backwards.</param>
        public void Move(float speed)
        {
            Vector3     look        = new Vector3(0.0f, 0.0f, 0.0f);
            Vector3     unitLook;
 
            // scale rate of change in movement
            const float SCALE       = 0.005f;
            speed                  *= SCALE;

            // update forward direction
            look.Y                  = view.Y;
            look.X                  = view.X - position.X;
            look.Z                  = view.Z - position.Z;
            
            // get new camera direction vector 
            unitLook = Vector3.Normalize(look);

            // update camera position and view position
            UpdatePositionAndView(unitLook, speed);
        }

        /// <summary>
        /// Sets rotation amount about an axis.
        /// </summary>
        /// <param name="degrees">Amount of rotation.</param>
        /// <param name="direction">Axis where rotation occurs.</param>
        private Vector4 RotationQuaternion(float degrees, Vector3 direction)
        {
            Vector4 unitAxis = Vector4.Zero;
            Vector4 axis     = new Vector4(direction, 0.0f);
            
            // only normalize if necessary
            if ((axis.X != 0 && axis.X != 1) || (axis.Y != 0 && axis.Y != 1) 
            ||  (axis.Z != 0 && axis.Z != 1)){
                    unitAxis = Vector4.Normalize(axis);
            }

            float angle = degrees * MathHelper.Pi/180.0f;
            float sin   = (float)Math.Sin(angle/2.0f);

            // create the quaternion.
            Vector4 quaternion = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
            quaternion.X       = axis.X * sin;
            quaternion.Y       = axis.Y * sin;
            quaternion.Z       = axis.Z * sin;
            quaternion.W       = (float)Math.Cos(angle/2.0f);

            return Vector4.Normalize(quaternion);
        }

        /// <summary>
        /// Finalizes change to the camera view.
        /// </summary>
        /// <param name="rotationAmount">Amount of rotation.</param>
        /// <param name="direction">Axis where rotation occurs.</param>
        private void UpdateView(float rotationAmount, Vector3 direction)
        { 
            // local rotation quaternion
            Vector4 Q = RotationQuaternion(rotationAmount, direction);
            Vector4 look = Vector4.Zero;
            look.X = view.X - position.X;
            look.Y = view.Y - position.Y;
            look.Z = view.Z - position.Z;

            // rotation quaternion*look
            Vector4 Qp;
            Qp.X = Q.W*look.X + Q.X*look.W + Q.Y*look.Z - Q.Z*look.Y;
            Qp.Y = Q.W*look.Y - Q.X*look.Z + Q.Y*look.W + Q.Z*look.X;
            Qp.Z = Q.W*look.Z + Q.X*look.Y - Q.Y*look.X + Q.Z*look.W;
            Qp.W = Q.W*look.W - Q.X*look.X - Q.Y*look.Y - Q.Z*look.Z;
        
            // conjugate is made by negating quaternion x, y, and z
            Vector4 conj = new Vector4(-Q.X, -Q.Y, -Q.Z, Q.W);
        
            // updated look vector
            Vector4 Qlook;
            Qlook.X = Qp.W*conj.X + Qp.X*conj.W + Qp.Y*conj.Z - Qp.Z*conj.Y;
            Qlook.Y = Qp.W*conj.Y - Qp.X*conj.Z + Qp.Y*conj.W + Qp.Z*conj.X;
            Qlook.Z = Qp.W*conj.Z + Qp.X*conj.Y - Qp.Y*conj.X + Qp.Z*conj.W;
            Qlook.W = Qp.W*conj.W - Qp.X*conj.X - Qp.Y*conj.Y - Qp.Z*conj.Z;

            // cap view at ground and sky
            if (Qlook.Y > -0.49f && Qlook.Y < 0.49f)
            {
                // updated view equals position plus the quaternion
                view.X = position.X + Qlook.X;
                view.Y = position.Y + Qlook.Y;
                view.Z = position.Z + Qlook.Z;
            }
        }


        /// <summary>
        /// Adjusts view according to movement of mouse or right thumbstick.
        /// </summary>
        /// <param name="X">Deviation from X in 2D window sets camera rotation about Y axis.</param>
        /// <param name="Y">Deviation from Y in 2D window sets camera rotation about X axis.</param>
        public void ChangeView(float X, float Y)
        { 
            // exit if no change to view
            if (X == 0 && Y == 0)
                return;

            float       rotationX, rotationY;
            const float SCALEX = 50.0f; const float SCALEY = 2000.0f;
            Vector3     look   = view - position;
            
            // tilt camera up and down
            Vector3 right     = Vector3.Cross(look, up);
                    rotationX = Y/SCALEX;
            UpdateView(rotationX, Vector3.Normalize(right));
            
            // swivel camera left and right
            rotationY = X * timeLapse / SCALEY;
            UpdateView(-rotationY, up);
        }

        /// <summary>
        /// Move camera position and view sideways.
        /// </summary>
        /// <param name="speed">Rate of change in direction.  
        ///                     Negative speed is left.  Positive speed is right.</param>
        public void Strafe(float speed)
        {
            Vector3 look = Vector3.Zero;             
            Vector3 unitLook;
            
            const float SCALE = 0.005f;              // set rate of sideways movement
            speed            *= SCALE;
            
            look.Y            = view.Y;
            look.X            = view.X - position.X;
            look.Z            = view.Z - position.Z;

            unitLook          = Vector3.Normalize(look);
            
            // update camera position and view with right vector direction
            Vector3 right     = Vector3.Cross(unitLook, up);
            UpdatePositionAndView(right, speed);
        }
    }
}