//------------------------------------------------------------
// Microsoft® XNA Game Studio Creator's Guide, Second Edition
// by Stephen Cawood and Pat McGee 
// Copyright (c) McGraw-Hill/Osborne. All rights reserved.
// https://www.mhprofessional.com/product.php?isbn=0071614060
//------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;
using CameraViewer;
using Projectiles;
using KillAllHumansHeightMapImporterTerrainRuntime;

namespace MGHGame
{
    /// <summary>
    /// This is the driving class for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        //------------------------------------------------------------
        // C L A S S   L E V E L   D E C L A R A T I O N S
        //------------------------------------------------------------
        // constant definitions
        public const float BOUNDARY = 16f;

        // accesses drawing methods and properties
        GraphicsDeviceManager graphics;

        // handle mouse on the PC
#if !XBOX
        MouseState mouse;
#endif

        // for loading and drawing 2D images on the game window
        SpriteBatch                 spriteBatch;

        // load and access PositionColor.fx shader
        public Effect              positionColorEffect;    // shader object
        public EffectParameter     positionColorEffectWVP; // to set display matrix for window

        // load and access Texture.fx shader
        private Effect              textureEffect;          // shader object                 
        private EffectParameter     textureEffectWVP;       // cumulative matrix w*v*p 
        private EffectParameter     textureEffectImage;     // texture parameter

        // camera 
        public Camera              cam = new Camera();

        // vertex types and buffers
        private VertexDeclaration   positionColor;
        private VertexDeclaration   positionColorTexture;

        // ground vertices and texture
        VertexPositionColorTexture[] 
            groundVertices = new    VertexPositionColorTexture[4];
        private Texture2D           grassTexture;
        private Texture2D blood;

        private SoundEffect headshot;
        private SoundEffect[] revenge;
        SoundEffectInstance sfi;

        String highscore_text;

        /// <summary>
        /// Initializes:    -GraphicsDeviceManager object for drawing 
        ///                 -ContentManager object for loading media
        /// </summary>

        Texture2D frontTexture, backTexture, groundTexture,
            leftTexture, rightTexture, skyTexture;

        private const float EDGE = BOUNDARY * 2f;
        private VertexPositionColorTexture[] skyVertices = new VertexPositionColorTexture[4];

        private Texture2D spriteTexture;
        int frameNum = 1;
        private double intervalTime = 0; // time in current interval
        private double previousIntervalTime = 0; // interval time at last frame

        Model baseModel; Model fanModel;
        Matrix[] baseMatrix; Matrix[] fanMatrix;

        const int WINDMILL_BASE = 0; const int WINDMILL_FAN = 1;

        private float fanRotation = 0.0f; // stores rotation of windmill fan

        private Vector3[] bezierA = new Vector3[4];  // route 1
        private Vector3[] lineA = new Vector3[2];   // route 2
        private Vector3[] bezierB = new Vector3[4]; // route 3
        private Vector3[] lineB = new Vector3[2]; // route 4
        private Vector3[] bezierC = new Vector3[4]; // route 5
        private Vector3[] lineC = new Vector3[2]; // route 6

        private float[] keyFrameTime = new float[6];
        private float tripTime = 0.0f;
        private const float TOTAL_TRIP_TIME = 17.8f;
        private const int NUM_KEYFRAMES = 6;

        Vector3 currentPosition, previousPosition;
        float Yrotation;

        Model jetModel;
        Matrix[] jetMatrix;

        public float BASE_HEIGHT = 0.6f; // start height for models
        public Gun gun;
   
        public Model humanModel; public Model humanSpheres;
        public Model crateModel; public Model crateSpheres;
        
        const int NUM_BALLOONS = 20;
        int NUM_ENEMIES = 100;
        bool deadman_walking = false;

#if !XBOX 
        MouseState mouseCurrent, mousePrevious;
#endif
        GamePadState gamepad, gamepadPrevious;


        public enum Group
        {
            plane, bigPlane,
            // wall spheres divided into 4 groups
            wallBackRight, wallBackLeft, wallFrontLeft, wallFrontRight,
            // wall spheres grouped according to location within 4 world quadrants
            worldBackRight, worldBackLeft, worldFrontLeft, worldFrontRight
            // rocket sphere
            ,rocketSpheres
        }

        const int TOTAL_SPHERE_GROUPS = 11;
        public Sphere[] sphereGroup = new Sphere[TOTAL_SPHERE_GROUPS];

        bool reverse = false;
        private SpriteFont spriteFont;

        VertexPositionNormalTexture[] terrainVertices;

        SoundEffect footstep1;
        SoundEffectInstance footstepInstance1;

        // You can change this part
        public bool SHOW = false; // show bounding spheres

        int HP = 100;
        
        const int HIT_SCORE = 1;
        const int GETTING_HIT_SCORE = 15;
        int enemies_in_a_wave = 1;
        float enemy_radius = 10f;
        ///////////


        int score = 0, total_score = 0;
        List<int> high_scores = new List<int>();
        Enemy enemies;
        /// <summary>
        /// //////////////////////
        /// </summary>
        SpriteFont font;
        public String GameOver = "NO";

        /// <summary>
        /// ///////////////
        /// 
        /// </summary>

        bool positiveDirection = true;

        public Random rnd;
        public powerup powerups;

        // The number of power ups the player has received
        int bullet_powerup = 0; int gun_powerup = 0;


        public bool game_has_started = false;
        SoundEffect ambience, scary_horror, menu_music, haha;
        SoundEffectInstance scary_horror_instance, ambienceInstance;
        Song game_music, win, lose, menu;
        System.Diagnostics.Stopwatch time;


        public Vector3 ProjectedXZ(Vector3 position, Vector3 speed,
                                   float directionScalar)
        {
            // only consider change in X and Z when projecting position
            // in neighboring cell.
            Vector3 velocity = new Vector3(speed.X, 0.0f, speed.Z);
            velocity = Vector3.Normalize(velocity);
            float changeX = directionScalar * terrain.cellWidth * velocity.X;
            float changeZ = directionScalar * terrain.cellHeight * velocity.Z;

            return new Vector3(position.X + changeX, 0.0f, position.Z + changeZ);
        }


        float CellWeight(Vector3 currentPosition, Vector3 nextPosition)
        {
            Vector3 currRowColumn = RowColumn(currentPosition);
            int currRow = (int)currRowColumn.Z;
            int currCol = (int)currRowColumn.X;
            Vector3 nextRowColumn = RowColumn(nextPosition);
            int nextRow = (int)nextRowColumn.Z;
            int nextCol = (int)nextRowColumn.X;

            // find row and column between current cell and neighbor cell
            int rowBorder, colBorder;
            if (currRow < nextRow)
                rowBorder = currRow + 1;
            else
                rowBorder = currRow;

            if (currCol < nextCol)              // next cell at right of current cell
                colBorder = currCol + 1;
            else
                colBorder = currCol;            // next cell at left of current cell
            Vector3 intersect = Vector3.Zero;   // margins between current
            // and next cell

            intersect.X = -BOUNDARY + colBorder * terrain.cellWidth;
            intersect.Z = -BOUNDARY + rowBorder * terrain.cellHeight;
            currentPosition.Y
                          = 0.0f;               // not concerned about height
            // find distance between current position and cell border
            Vector3 difference = intersect - currentPosition;
            float lengthToBorder = difference.Length();

            // find distance to projected location in neighboring cell
            difference = nextPosition - currentPosition;
            float lengthToNewCell = difference.Length();
            if (lengthToNewCell == 0)              // prevent divide by zero
                return 0.0f;

            // weighted distance in current cell relative to the entire
            // distance to projected position
            return lengthToBorder / lengthToNewCell;
        }

        Vector3 CellNormal(int row, int col)
        {
            HandleOffHeightMap(ref row, ref col);

            return new Vector3(terrain.NormalX(col + row * terrain.NUM_COLS),
                               terrain.NormalY(col + row * terrain.NUM_COLS),
                               terrain.NormalZ(col + row * terrain.NUM_COLS));
        }

        Vector3 Normal(Vector3 position)
        {
            // coordinates for top left of cell
            Vector3 cellPosition = RowColumn(position);
            int row = (int)cellPosition.Z;
            int col = (int)cellPosition.X;
            // distance from top left of cell
            float distanceFromLeft = position.X % terrain.cellWidth;
            float distanceFromTop = position.Z % terrain.cellHeight;

            // use lerp to interpolate normal at point within cell
            Vector3 topNormal = Vector3.Lerp(CellNormal(row, col),
                                                   CellNormal(row, col + 1),
                                                   distanceFromLeft);
            Vector3 bottomNormal = Vector3.Lerp(CellNormal(row + 1, col),
                                                   CellNormal(row + 1, col + 1),
                                                   distanceFromLeft);
            Vector3 normal = Vector3.Lerp(topNormal,
                                                   bottomNormal,
                                                   distanceFromTop);
            normal.Normalize(); // convert to unit vector for consistency
            if (normal == null || normal == Vector3.Zero)
                return Vector3.Up; 
            return normal;
        }

        Vector3 NormalWeight(Vector3 position, Vector3 speed,
                             float numCells, float directionScalar)
        {
            float weight = 0.0f;
            float startWeight = 0.0f;
            float totalSteps = (float)numCells;
            Vector3 nextPosition;
            Vector3 cumulativeNormal = Vector3.Zero;

            for (int i = 0; i <= numCells; i++)
            {
                // get position in next cell
                if (speed == Vector3.Zero)
                {
                    speed = cam.view - cam.position;
                }
                nextPosition = ProjectedXZ(position, speed, directionScalar);
                if (i == 0)
                {
                    // current cell
                    startWeight = CellWeight(position, nextPosition);
                    weight = startWeight / totalSteps;
                }
                else if (i == numCells) // end cell
                    weight = (1.0f - startWeight) / totalSteps;
                else                    // all cells in between
                    weight = 1.0f / totalSteps;

                cumulativeNormal += weight * Normal(position);
                position = nextPosition;
            }
            cumulativeNormal.Normalize();
            return cumulativeNormal;
        }

        Vector3 ProjectedUp(Vector3 position, Vector3 speed, int numCells)
        {
            Vector3 frontAverage, backAverage, projectedUp;

            // total steps must be 0 or more. 0 steps means no shock absorption.
            if (numCells <= 0)
                return Normal(position);

            // weighted average of normals ahead and behind enable smoother ride.
            else
            {
                frontAverage = NormalWeight(position, speed, numCells, 1.0f);
                backAverage = NormalWeight(position, speed, numCells, -1.0f);
            }
            projectedUp = (frontAverage + backAverage) / 2.0f;
            projectedUp.Normalize();
            return projectedUp;
        }


        //////////////////

        // The core of the game starts here.
        public Game1()
        {
            graphics              = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";


            string[] lines = System.IO.File.ReadAllLines(@"highscores.txt");
            foreach (string s in lines)
            {
                high_scores.Add(Int32.Parse(s));
            }
        }

        // checks to see if the position is in the boundary of the terrain. Then returns a corrected value.
        public Vector3 inbound(Vector3 v)
        {
            if (v.X > BOUNDARY-1) v.X = BOUNDARY-1;
            if (v.X < -BOUNDARY+1) v.X = -BOUNDARY+1;

            if (v.Z > BOUNDARY-1) v.Z = BOUNDARY-1;
            if (v.Z < -BOUNDARY+1) v.Z = -BOUNDARY+1;

            return v;
        }

        // returns true if more than a specific amount of time is passed
        float last_time = 0f;
        public bool more_than_one_second_has_passed(float sec)
        {
            if (last_time > time.Elapsed.Seconds) last_time = 0;
            if (time.Elapsed.Seconds -last_time > sec)
            {
                last_time = time.Elapsed.Seconds;
                return true;
            }
            return false;
        }
        public Vector3 wander(Vector3 direction, Vector3 up)
        {
            int a = rnd.Next(0, 2) == 1? 1 : -1;

            return a * Vector3.Cross(direction, up);
        }

        public void power_up(String name)
        {
            if (name == "gun")
                gun_powerup++;
            if (name == "bullet")
                bullet_powerup++;

            gun.gun_clip_limit = 13 + gun_powerup * 7;
            gun.bullets += bullet_powerup * 20;
            bullet_powerup = 0;
        }

        int enemy_numbers = 0;
        public void spawn_enemy_wave(){
            if (enemy_numbers >= NUM_ENEMIES && !deadman_walking) return;

            if (enemies.active_enemies() < enemies_in_a_wave)
            {
                for (int i = enemies.active_enemies(); i < enemies_in_a_wave; i++)
                {
                    enemies.spawn_new_enemy(enemy_radius);
                    enemy_numbers++;
                }
            }
        }

        public void stop_all_sounds()
        {
            MediaPlayer.Stop();
            scary_horror_instance.Stop();
            ambienceInstance.Stop();
            footstepInstance1.Stop();
            powerups.stop_sound();
            powerups.bullet_box.stop_sounds();
            powerups.gun_box.stop_sounds();
            
        }
        float got_hit = 0f;
        public int adjust_score_and_hp(bool hit_by_enemy)
        {
            if (hit_by_enemy)
            {
                HP -= GETTING_HIT_SCORE;
                got_hit = 1f;
                headshot.Play();
                if (sfi.State != SoundState.Playing)
                {
                    int i = rnd.Next(0, revenge.Length);
                    sfi = revenge[i].CreateInstance();
                    sfi.Play();
                }
            }
            else
            {
                score += HIT_SCORE; total_score += HIT_SCORE;
                gun.bullets += 20 * (int)(score/20f);
                score = score % 20;
            }

            if (HP <= 0)
            {
                stop_all_sounds();
                GameOver = "LOSE";
                game_has_started = false;
                if (deadman_walking)
                    high_scores.Add(total_score);
                haha.Play();
                MediaPlayer.IsRepeating = false;
                MediaPlayer.Play(lose);
                return -1; // LOSE
            }
            else if (enemy_numbers >= NUM_ENEMIES && !deadman_walking)
            {
                GameOver = "WIN";
                game_has_started = false;
                if (deadman_walking)
                    high_scores.Add(total_score);
                stop_all_sounds();
                MediaPlayer.IsRepeating = false;
                MediaPlayer.Play(win);
                return 1; // WIN
            }
            else if (HP <= 30)
            {
                MediaPlayer.Stop();
                scary_horror_instance.Play();
            }

            
            return 0;
        }

        // and it ends here
        ////////////////////////////////


        // implementing heightmap and terrain
        Vector3 cam_velocity;
        Vector3 lastNormal;
        float last_yaw = 0;
        void UpdateCameraHeight()
        {
            const float HOVER_AMOUNT = 0.75f;

            float height = CellHeight(cam.position);
            cam.view.Y += height - cam.position.Y +HOVER_AMOUNT;
            cam.position.Y += height - cam.position.Y + HOVER_AMOUNT;

            
            
        }
        // Importing the heightmap.raw
        //
        //

        Terrain terrain;

        private void HandleOffHeightMap(ref int row, ref int col)
        {
            if (row >= terrain.NUM_ROWS)
                row = terrain.NUM_ROWS - 2;
            else if (row < 0)
                row = 0;
            if (col >= terrain.NUM_COLS)
                col = terrain.NUM_COLS - 2;
            else if (col < 0)
                col = 0;
        }

        Vector3 RowColumn(Vector3 position)
        {
            // calculate X and Z
            int col = (int)((position.X + terrain.worldWidth) / terrain.cellWidth);
            int row = (int)((position.Z + terrain.worldHeight) / terrain.cellHeight);
            HandleOffHeightMap(ref row, ref col);

            return new Vector3(col, 0.0f, row);
        }

        float Height(int row, int col)
        {
            HandleOffHeightMap(ref row, ref col);
            //return terrainVertices[col + row * terrain.NUM_COLS].Position.Y;
            return terrain.position[col + row * terrain.NUM_COLS].Y;
        }

        float Height2(int row, int col)
        {
            HandleOffHeightMap(ref row, ref col);
            return terrainVertices[col + row * terrain.NUM_COLS].Position.Y;
        }

        public float CellHeight(Vector3 position)
        {
            // get top left row and column indices
            Vector3 cellPosition = RowColumn(position);
            int row = (int)cellPosition.Z;
            int col = (int)cellPosition.X;

            // distance from top left of cell
            float distanceFromLeft, distanceFromTop;
            distanceFromLeft = position.X % terrain.cellWidth;
            distanceFromTop = position.Z % terrain.cellHeight;

            // Lerp projects height relative to known dimensions
            float topHeight = MathHelper.Lerp(Height(row, col), Height(row, col + 1), distanceFromLeft);
            float bottomHeight = MathHelper.Lerp(Height(row + 1, col), Height(row + 1, col + 1), distanceFromLeft);
            
            return MathHelper.Lerp(topHeight, bottomHeight, distanceFromTop);
        }

        ///////////////////////////////////


        // for directional light
        //
        //

        private IndexBuffer indexBuffer; // reference vertices
        private VertexBuffer vertexBuffer; // vertex storage
        private VertexDeclaration positionNormalTexture;

        const int NUM_COLS = 257;
        const int NUM_ROWS = 257;

        BasicEffect basicEffect;

        private void InitializeBasicEffect()
        {
            basicEffect = new BasicEffect(graphics.GraphicsDevice);
            basicEffect.TextureEnabled = true; // needed if objects are textured
            basicEffect.LightingEnabled = true; // must be on for lighting effect
            basicEffect.SpecularPower = 5.0f; // highlights
            basicEffect.AmbientLightColor = new Vector3(1f, 1f, 1f) * 0.7f; // background light
            basicEffect.DirectionalLight0.Enabled = true;   // turn on light
            basicEffect.DirectionalLight0.DiffuseColor = new Vector3(0.2f, 0.2f, 0.2f); // rgb range 0 to 1
            basicEffect.DirectionalLight0.SpecularColor        // highlight color
                = new Vector3(0.5f, 0.5f, 0.37f);   // rgb range 0 to 1
            basicEffect.DirectionalLight0.Direction // set normalized direction
                = Vector3.Normalize(new Vector3(0.0f, -1.0f, -1.0f));
        }

        private void InitializeIndices()
        {
            short[] indices;
            indices = new short[2 * NUM_COLS]; // indices for 1 subset
            indexBuffer = new IndexBuffer(
                graphics.GraphicsDevice, // graphics device
                typeof(short),           // data type is short
                indices.Length,          // array size in bytes
                BufferUsage.WriteOnly);  // memory allocation

            // store indices for one subset of vertices
            // see Figure 11-2 for the first subset of indices
            int counter = 0;
            for (int col = 0; col < NUM_COLS; col++)
            {
                indices[counter++] = (short)col;
                indices[counter++] = (short)(col + NUM_COLS);
            }
            indexBuffer.SetData(indices);
        }

        private void InitializeVertexBuffer()
        {
            vertexBuffer = new VertexBuffer(
                graphics.GraphicsDevice,    // graphics device
                typeof(VertexPositionNormalTexture), // vertex type
                NUM_COLS * NUM_ROWS,    // element count
                BufferUsage.WriteOnly); // memory use

            // store vertices temporarily while initializing them
            VertexPositionNormalTexture[] vertex = new VertexPositionNormalTexture[NUM_ROWS * NUM_COLS];

            // set grid width and height
            float colWidth = (float)2 * BOUNDARY / (NUM_COLS - 1);
            float rowHeight = (float)2 * BOUNDARY / (NUM_ROWS - 1);

            // set position, color, and texture coordinates
            for (int row = 0; row < NUM_ROWS; row++){
                for (int col = 0; col < NUM_COLS; col++){
                    vertex[col + row * NUM_COLS].Position   // position
                        = terrain.position[col + row * NUM_COLS] ;
                    float U = (float)col / (float)(NUM_COLS - 1);   // UV
                    float V = (float)row / (float)(NUM_ROWS - 1);
                    vertex[col + row * NUM_COLS].TextureCoordinate = new Vector2(U, V);
                    vertex[col + row * NUM_COLS].Normal // normal
                        = terrain.normal[col + row * NUM_COLS];
                }
            }
            terrainVertices = vertex;

            // commit data to vertex buffer
            vertexBuffer.SetData(vertex);
        }

        private void DrawIndexedGrid(Texture2D image, Matrix myWorld)
        {
            // 1: declare matrices
            Matrix world, translate, rotationX, scale, rotationY;

            // 2: initialize matrices
            // world = myWorld;
            world = Matrix.Identity;// camWorldMatrix(cam_velocity, cam.position);
            scale = Matrix.CreateScale(2.01f, 2.01f, 2.01f);

            // Create two walls with normals that face the user
            basicEffect.Texture = image;

            // 4: set shader parameters
            basicEffect.World = world;
            basicEffect.Projection = cam.projectionMatrix;
            basicEffect.View = cam.viewMatrix;

            // avoid drawing back face for large amounts of vertices
            graphics.GraphicsDevice.RasterizerState = RasterizerState.CullClockwise;

            basicEffect.Techniques[0].Passes[0].Apply();

            // 5: draw objet - select vertex type, primitive type, index, & draw
            graphics.GraphicsDevice.SetVertexBuffer(vertexBuffer);
            graphics.GraphicsDevice.Indices = indexBuffer;

            foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                graphics.GraphicsDevice.SetVertexBuffer(vertexBuffer);

                for (int Z = 0; Z < NUM_ROWS - 1; Z++)
                {
                    graphics.GraphicsDevice.DrawIndexedPrimitives(
                        PrimitiveType.TriangleStrip,    // primitive
                        Z * NUM_COLS,               // start point in buffer for drawing
                        0,                          // minimum vertices in vertex buffer
                        NUM_COLS * NUM_ROWS,        // total vertices in buffer
                        0,                          // start point in index buffer
                        2 * (NUM_COLS - 1));        // primitive count
                }
                // end shader

                // disable culling
                graphics.GraphicsDevice.RasterizerState = RasterizerState.CullNone;

            }

        }

        private void LightingShader(PrimitiveType primitiveType)
        {

            // draw grid one row at a time
            for (int Z = 0; Z < NUM_ROWS - 1; Z++)
            {
                graphics.GraphicsDevice.DrawIndexedPrimitives(
                    PrimitiveType.TriangleStrip,    // primitive
                    Z * NUM_COLS,               // start point in buffer for drawing
                    0,                          // minimum vertices in vertex buffer
                    NUM_COLS * NUM_ROWS,        // total vertices in buffer
                    0,                          // start point in index buffer
                    2 * (NUM_COLS - 1));        // primitive count
            }
            // end shader

            // disable culling
            graphics.GraphicsDevice.RasterizerState = RasterizerState.CullNone;
        }
        //////////////////////////////////////////////////


        // collision detection
        public bool Collision(BoundingSphere A, BoundingSphere B)
        {
            if (A.Intersects(B))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool planeCollision(Camera camera, int rocketGroup, int planeGroup, int rocketNumber)
        {
            //check selected car and selected wall spheres for collisions
            for (int i = 0; i < sphereGroup[planeGroup].sphere.Count; i++)
            {
                for (int j = 0; j < sphereGroup[rocketGroup].sphere.Count; j++)
                {
                    Matrix transfrom = ScaleSpheres() * TransformPlane(camera);

                    //generate temp bounding sphere with transformed sphere
                    BoundingSphere tempPlaneSphere =
                        sphereGroup[planeGroup].sphere[i].boundingSphere.Transform(transfrom);

                    tempPlaneSphere.Radius
                        = SphereScalar() * sphereGroup[planeGroup].sphere[i].boundingSphere.Radius;

                    transfrom = ScaleSpheres() * gun.transformRocket(rocketNumber);
                    BoundingSphere tempRocketSphere = sphereGroup[rocketGroup].sphere[i].boundingSphere.Transform(transfrom);
                    tempRocketSphere.Radius = SphereScalar() * sphereGroup[rocketGroup].sphere[i].boundingSphere.Radius;

                    if (Collision(tempRocketSphere, tempPlaneSphere))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        // Chapter 19 methods:
        void InitializeModels()
        {
            // load models
            humanModel = Content.Load<Model>("Models\\fem1");
            humanSpheres = Content.Load<Model>("Models\\fem1_sphere");

            crateModel = Content.Load<Model>("Models\\crate");
            crateSpheres= Content.Load<Model>("Models\\crate_spheres");
        }

        void WallSphere(Group group, float X, float Z, float radius)
        {
            Color color = Color.Yellow;
            Vector3 position = new Vector3(X, 0.0f, Z);
            position.Y = CellHeight(position);
            sphereGroup[(int)group].AddSphere(radius, position, color);
        }

        void InitializeWallSpheres()
        {
            // initialize vertices around border of world
            sphereGroup[(int)Group.wallBackRight] = new Sphere(SHOW);
            sphereGroup[(int)Group.wallBackLeft] = new Sphere(SHOW);
            sphereGroup[(int)Group.wallFrontRight] = new Sphere(SHOW);
            sphereGroup[(int)Group.wallFrontLeft] = new Sphere(SHOW);

            Vector3 wall;
            const float RADIUS = 0.3f;
            const float SPHERE_INCREMENT = 2.0f;

            // 1st qtr 
            wall = new Vector3(BOUNDARY, 0.0f, -BOUNDARY);

            // small spheres right and left - back half of world
            while (wall.Z < 0.0f)
            {
                WallSphere(Group.wallBackRight, wall.X, wall.Z, RADIUS);
                WallSphere(Group.wallBackLeft, -wall.X, wall.Z, RADIUS);
                wall.Z += SPHERE_INCREMENT * RADIUS;
            }

            // small spheres right and left - front half of world
            while (wall.Z < BOUNDARY)
            {
                WallSphere(Group.wallFrontRight, wall.X, wall.Z, RADIUS);
                WallSphere(Group.wallFrontLeft, -wall.X, wall.Z, RADIUS);
                wall.Z += SPHERE_INCREMENT * RADIUS;
            }

            // small spheres right side of world - back and front
            wall = new Vector3(BOUNDARY, 0.0f, -BOUNDARY);
            while (wall.X > 0.0f)
            {
                WallSphere(Group.wallBackRight, wall.X, wall.Z, RADIUS);
                WallSphere(Group.wallFrontRight, wall.X, -wall.Z, RADIUS);
                wall.X -= SPHERE_INCREMENT * RADIUS;
            }

            // small spheres left side of world - top and bottom
            while (wall.X > -BOUNDARY)
            {
                WallSphere(Group.wallBackLeft, wall.X, wall.Z, RADIUS);
                WallSphere(Group.wallBackLeft, wall.X, -wall.Z, RADIUS);
                wall.X -= SPHERE_INCREMENT * RADIUS;
            }
            // separate world into 4 quadrants
            sphereGroup[(int)Group.worldBackRight] = new Sphere(SHOW);
            sphereGroup[(int)Group.worldBackLeft] = new Sphere(SHOW);
            sphereGroup[(int)Group.worldFrontLeft] = new Sphere(SHOW);
            sphereGroup[(int)Group.worldFrontRight] = new Sphere(SHOW);

            // large spheres - each one surrounds 1 quarter of world
            float radius = BOUNDARY;
            WallSphere(Group.worldBackRight, BOUNDARY, -BOUNDARY, radius);
            WallSphere(Group.worldBackLeft, -BOUNDARY, -BOUNDARY, radius);
            WallSphere(Group.worldFrontLeft, -BOUNDARY, BOUNDARY, radius);
            WallSphere(Group.worldFrontRight, BOUNDARY, BOUNDARY, radius);
        }

        void ExtractBoundingSphere(Model tempModel, Color color, int groupNum)
        {
            // set up model temporarily 
            Matrix[] tempMatrix = new Matrix[tempModel.Bones.Count];
            tempModel.CopyAbsoluteBoneTransformsTo(tempMatrix);

            // generate new sphere group
            BoundingSphere sphere = new BoundingSphere();
            sphereGroup[groupNum] = new Sphere(SHOW);

            // store radius, position, and color information for each sphere
            foreach (ModelMesh mesh in tempModel.Meshes)
            {
                sphere = mesh.BoundingSphere;
                Vector3 newCenter = sphere.Center;
                Matrix transformationMatrix = ScaleModel();
                sphereGroup[groupNum].AddSphere(sphere.Radius, sphere.Center, color);
            }
        }

        Matrix ScaleModel()
        {
            const float SCALAR = 0.002f;
            return Matrix.CreateScale(SCALAR, SCALAR, SCALAR);
        }

        public float SphereScalar()
        {
            return 0.1f;
        }

        Matrix ScaleSpheres()
        {
            // spheres created with different modeling tool so scaled differently
            return Matrix.CreateScale(SphereScalar(), SphereScalar(), SphereScalar());
        }

        void InitializeModelSpheres()
        {
            // load big plane sphere
            Model sphereModel = Content.Load<Model>("Models\\bigPlaneSphere");
            ExtractBoundingSphere(sphereModel, Color.Blue, (int)Group.bigPlane);

            sphereModel = Content.Load<Model>("Models\\planeSpheres");
            ExtractBoundingSphere(sphereModel, Color.White, (int)Group.plane);

            sphereModel = Content.Load<Model>("Models\\bulletSphere");
            ExtractBoundingSphere(sphereModel, Color.Red, (int)Group.rocketSpheres);
        }

        private void UpdateKeyframeAnimation(GameTime gameTime)
        {
            // update total trip time, use modulus to prevent variable overflow
            tripTime += (gameTime.ElapsedGameTime.Milliseconds / 1000.0f);
            tripTime = tripTime % TOTAL_TRIP_TIME;

            // get the current route number from a total of four routes
            int routeNum = KeyFrameNumber(reverse);

            // sum times for preceding keyframes
            float keyFrameStartTime = 0.0f;
            for (int i = 0; i < routeNum; i++)
                keyFrameStartTime += keyFrameTime[i];

            // calculate time spent during current route
            float timeBetweenKeys = tripTime - keyFrameStartTime;

            // calculate percentage of current route completed
            float fraction = timeBetweenKeys / keyFrameTime[routeNum];

            // get current X, Y, Z of object being animated
            // find point on line or curve by passing in % completed
            switch (routeNum)
            {
                case 0: // first curve
                    currentPosition = GetPositionOnCurve(bezierA, fraction); break;
                case 1: // first line
                    currentPosition = GetPositionOnLine(lineA, fraction); break;
                case 2: // 2nd curve
                    currentPosition = GetPositionOnCurve(bezierB, fraction); break;
                case 3:
                    currentPosition = GetPositionOnLine(lineB, fraction); break;
                case 4:
                    currentPosition = GetPositionOnCurve(bezierC, fraction); break;
                case 5:
                    currentPosition = GetPositionOnLine(lineC, fraction); break;
            }

            // get rotation angle about Y based on change in X and Z speed
            Vector3 speed = currentPosition - previousPosition;
            previousPosition = currentPosition;
            Yrotation = (float)Math.Atan2((float)speed.X, (float)speed.Z);
        }

        private Vector3 GetPositionOnCurve(Vector3[] bezier, float fraction)
        {
            // returns absolute position on curve based on relative
            return // position on curve (relative position ranges from 0% to 100%)
                bezier[0] * (1.0f - fraction) * (1.0f - fraction) * (1.0f - fraction) +
                bezier[1] * 3.0f * fraction * (1.0f - fraction) * (1.0f - fraction) +
                bezier[2] * 3.0f * fraction * fraction * (1.0f - fraction) +
                bezier[3] * fraction * fraction * fraction;
            
        }

        private Vector3 GetPositionOnLine(Vector3[] line, float fraction)
        {
            // returns absolute position on line based on relative position
            // on curve (relative position ranges from 0% to 100%)
            Vector3 lineAtOrigin = line[1] - line[0];
            return line[0] + fraction * lineAtOrigin;
        }

        private int KeyFrameNumber(bool reverse = false)
        {
            //if (reverse)
            //{
            //    reverse = false;
            //    return 5 - KeyFrameNumber();
            //}
            float timeLapsed = 0.0f;
            // retrieve current leg of trip
            for (int i = 0; i < NUM_KEYFRAMES; i++)
            {
                if (timeLapsed > tripTime)
                    return i - 1;
                else
                    timeLapsed += keyFrameTime[i];
            }
            return 5; // special case for last route
        }

        Rectangle TitleSafeRegion(Texture2D texture, int numFrames)
        {
            int windowWidth = Window.ClientBounds.Width;
            int windowHeight = Window.ClientBounds.Height;

            // some televisions only show 80% of the window
            const float UNSAFEAREA = .2f;
            const float MARGIN = UNSAFEAREA / 2.0f;

            // return bounding margins
            int top, left, height, width;
            left = (int)(windowWidth * MARGIN);
            top = (int)(windowHeight * MARGIN);

            width = (int)((1.0f - UNSAFEAREA) * windowWidth - texture.Width);
            height = (int)((1.0f - UNSAFEAREA) * windowHeight - texture.Height / numFrames);

            return new Rectangle(left, top, width, height);
        }

        Rectangle TitleSafeRegion(string outputString, SpriteFont font)
        {
            Vector2 stringDimensions = font.MeasureString(outputString);
            float width = stringDimensions.X; // string pixel width
            float height = stringDimensions.Y; // font pixel height

            // some televisions only show 80% of the window
            const float UNSAFEAREA = 0.2f;
            Vector2 topLeft = new Vector2();
            topLeft.X = graphics.GraphicsDevice.Viewport.Width * UNSAFEAREA / 2.0f;
            topLeft.Y = graphics.GraphicsDevice.Viewport.Height * UNSAFEAREA / 2.0f;

            return new Rectangle(
                (int)topLeft.X,
                (int)topLeft.Y,
                (int)((1.0f - UNSAFEAREA) * (float)Window.ClientBounds.Width - width),
                (int)((1.0f - UNSAFEAREA) * (float)Window.ClientBounds.Height - height));
        }

        private void DisplayStats(string outputString)
        {
            Rectangle safeArea;

            // start drawing font sprites
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);

            Vector3 position = RowColumn(cam.position);
            int row = (int)position.Z;
            int col = (int)position.X;
            float height = terrain.position[col + terrain.NUM_ROWS * row].Y;

            // show cell height and width
            //outputString = "Cell Height=" + height + "\nZ = " + row + "\nX = " + col;
            

            safeArea = TitleSafeRegion(outputString, spriteFont);
            if (game_has_started)
                spriteBatch.DrawString(spriteFont, outputString, new Vector2(safeArea.Left, safeArea.Top), Color.Yellow);
            else
                spriteBatch.DrawString(spriteFont, outputString, new Vector2(safeArea.Center.X, safeArea.Center.Y), Color.Yellow);
            // stop drawing - and 3D settings are restored if SaveState used 
            
            spriteBatch.End();
        }

        bool Timer(GameTime gameTime)
        {
            bool resetInterval = false;

            // add time lapse between frames and keep value between 0 & 500 ms
            intervalTime += (double)gameTime.ElapsedGameTime.Milliseconds;
            intervalTime = intervalTime % 500;

            // intervalTime has been reset so a new interval has started
            if (intervalTime < previousIntervalTime)
                resetInterval = true;

            previousIntervalTime = intervalTime;
            return resetInterval;
        }

        /// <summary>
        /// This method is called when the program begins to set game application
        /// properties such as status bar title and draw mode.  It initializes the  
        /// camera viewer projection, vertex types, and shaders.
        /// </summary>
        private void InitializeSkybox()
        {
            Vector3 pos = Vector3.Zero;
            Vector2 uv = Vector2.Zero;
            Color color = Color.White;

            const float MAX = 0.997f;   // offset to remove white seam at image edge
            const float MIN = 0.003f;   // offset to remove white seam at image edge

            // set position, image, and color data for each vertex in rectangle
            pos.X = +EDGE; pos.Y = -EDGE; uv.X = MIN; uv.Y = MAX;   // Bottom R
            skyVertices[0] = new VertexPositionColorTexture(pos, color, uv);

            pos.X = +EDGE; pos.Y = +EDGE; uv.X = MIN; uv.Y = MIN;   // Top R
            skyVertices[1] = new VertexPositionColorTexture(pos, color, uv);

            pos.X = -EDGE; pos.Y = -EDGE; uv.X = MAX; uv.Y = MAX; // Bottom L
            skyVertices[2] = new VertexPositionColorTexture(pos, color, uv);

            pos.X = -EDGE; pos.Y = +EDGE; uv.X = MAX; uv.Y = MIN; // Top L
            skyVertices[3] = new VertexPositionColorTexture(pos, color, uv);
        }

        private void InitializeTimeLine()
        {
            keyFrameTime[0] = 4.8f; // time to complete route 1
            keyFrameTime[1] = 0.8f; // time to complete route 2
            keyFrameTime[2] = 4.8f; // time to complete route 3
            keyFrameTime[3] = 0.8f; // time to complete route 4
            keyFrameTime[4] = 4.8f;
            keyFrameTime[5] = 1.8f;
        }

        private void swap(Vector3 a, Vector3 b)
        {
            Vector3 temp = a;
            a = b;
            b = temp;
        }
        private void InitializeRoutes()
        {
            // length of world quadrant
            const float END = BOUNDARY/2f;
            //// 1st Bezier curve control points (1st route)
            const float SCALE = 12f;
            if (!reverse)
            {
                bezierA[0] = new Vector3(-END - 5.0f, 5.4f, END + 5.0f); bezierA[0].Y = CellHeight(bezierA[0]) *SCALE;  // start
                bezierA[1] = new Vector3(-END - 5.0f, 2.4f, 3.0f * END); bezierA[1].Y = CellHeight(bezierA[1]) * SCALE; // ctrl 1
                bezierA[2] = new Vector3(END + 5.0f, 4.4f, 3.0f * END); bezierA[2].Y = CellHeight(bezierA[2]) * SCALE;// ctrl 2
                bezierA[3] = new Vector3(END + 5.0f, 0.4f, 5.0f); bezierA[3].Y = CellHeight(bezierA[3]) * SCALE;// end

                lineA[0] = new Vector3(END + 5.0f, 0.4f, 5.0f); lineA[0].Y = CellHeight(lineA[0]) * SCALE;
                lineA[1] = new Vector3(END + 5.0f, 0.4f, -5.0f); lineA[1].Y = CellHeight(lineA[1]) * SCALE;

                bezierB[0] = new Vector3(END + 5.0f, 0.4f, -5.0f); bezierB[0].Y = CellHeight(bezierB[0]) * SCALE;// start
                bezierB[1] = new Vector3(END + 5.0f, 2.4f, -3.0f * END); bezierB[1].Y = CellHeight(bezierB[1]) * SCALE;// ctrl 1
                bezierB[2] = new Vector3(-END - 1f, 2.4f, 3.0f * END); bezierB[2].Y = CellHeight(bezierB[2]) * SCALE;// ctrl 2
                bezierB[3] = new Vector3(-END - 1f, 2.0f, END + 5.0f); bezierB[3].Y = CellHeight(bezierB[3]) * SCALE;// end

                lineB[0] = new Vector3(-END - 1f, 2f, END + 5.0f); lineB[0].Y = CellHeight(lineB[0]) * SCALE;
                lineB[1] = new Vector3(-END - 0.2f, 3f, END + 3.0f); lineB[1].Y = CellHeight(lineB[1]) * SCALE;

                bezierC[0] = new Vector3(-END - 0.2f, 3f, END + 3.0f); bezierC[0].Y = CellHeight(bezierC[0]) * SCALE;  // start
                bezierC[1] = new Vector3(+END + 0.2f, 3f, 3.0f * END); bezierC[1].Y = CellHeight(bezierC[1]) * SCALE; // ctrl 1
                bezierC[2] = new Vector3(+END + 5.0f, 5.4f, -3.0f * END); bezierC[2].Y = CellHeight(bezierC[2]) * SCALE;// ctrl 2
                bezierC[3] = new Vector3(-END - 5.0f, 4.4f, -END - 5.0f); bezierC[3].Y = CellHeight(bezierC[3]) * SCALE;// end

                lineC[0] = new Vector3(-END - 5.0f, 4.4f, -END - 5.0f); lineC[0].Y = CellHeight(lineC[0]) * SCALE;
                lineC[1] = new Vector3(-END - 5.0f, 5.4f, END + 5.0f); lineC[1].Y = CellHeight(lineC[1]) * SCALE;
            }

            if (reverse)
            {
                bezierC[3] = new Vector3(-END - 5.0f, 5.4f, END + 5.0f);   // start
                bezierC[2] = new Vector3(-END - 5.0f, 2.4f, 3.0f * END); // ctrl 1
                bezierC[1] = new Vector3(END + 5.0f, 4.4f, 3.0f * END); // ctrl 2
                bezierC[0] = new Vector3(END + 5.0f, 0.4f, 5.0f); // end

                lineB[1] = new Vector3(END + 5.0f, 0.4f, 5.0f);
                lineB[0] = new Vector3(END + 5.0f, 0.4f, -5.0f);

                bezierB[3] = new Vector3(END + 5.0f, 0.4f, -5.0f);   // start
                bezierB[2] = new Vector3(END + 5.0f, 2.4f, -3.0f * END); // ctrl 1
                bezierB[1] = new Vector3(-END - 1f, 2.4f, 3.0f * END); // ctrl 2
                bezierB[0] = new Vector3(-END - 1f, 2.0f, END + 5.0f); // end

                lineA[1] = new Vector3(-END - 1f, 2f, END + 5.0f);
                lineA[0] = new Vector3(-END - 0.2f, 3f, END + 3.0f);

                bezierA[3] = new Vector3(-END - 0.2f, 3f, END + 3.0f);   // start
                bezierA[2] = new Vector3(+END + 0.2f, 3f, 3.0f * END); // ctrl 1
                bezierA[1] = new Vector3(+END + 5.0f, 5.4f, -3.0f * END); // ctrl 2
                bezierA[0] = new Vector3(-END - 5.0f, 4.4f, -END - 5.0f); // end

                lineC[1] = new Vector3(-END - 5.0f, 4.4f, -END - 5.0f);
                lineC[0] = new Vector3(-END - 5.0f, 5.4f, END + 5.0f);

            }
        }

        private void InitializeBaseCode()
        {
            // set status bar in PC Window (there is none for the Xbox 360)
            Window.Title = "Microsoft® XNA Game Studio Creator's Guide, Second Edition";

            // see both sides of objects drawn
            //graphics.GraphicsDevice.RenderState.CullMode = CullMode.None;
            GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
            
            // set camera matrix
            cam.SetProjection(Window.ClientBounds.Width, 
                              Window.ClientBounds.Height);

            // initialize vertex types
            positionColor = new VertexDeclaration(VertexPositionTexture.VertexDeclaration.GetVertexElements());
            positionColorTexture = new VertexDeclaration(VertexPositionTexture.VertexDeclaration.GetVertexElements());

            // load PositionColor.fx and set global params
            positionColorEffect     = Content.Load<Effect>("Shaders\\PositionColor");
            positionColorEffectWVP  = positionColorEffect.Parameters["wvpMatrix"];

            // load Texture.fx and set global params
            textureEffect           = Content.Load<Effect>("Shaders\\Texture");
            textureEffectWVP        = textureEffect.Parameters["wvpMatrix"];
            textureEffectImage      = textureEffect.Parameters["textureImage"];

            positionNormalTexture = new VertexDeclaration(VertexPositionNormalTexture.VertexDeclaration.GetVertexElements());
        }

        /// <summary>
        /// Set vertices for rectangular surface that is drawn using a triangle strip.
        /// </summary>
        private void InitializeGround(){
            const float BORDER = BOUNDARY;
            Vector2     uv     = new Vector2(0.0f, 0.0f);
            Vector3     pos    = new Vector3(0.0f, 0.0f, 0.0f);
            Color       color  = Color.White;

            // top left
            uv.X= 0.0f; uv.Y= 0.0f;     pos.X=-BORDER; pos.Y=0.0f; pos.Z=-BORDER;
            groundVertices[0]  = new VertexPositionColorTexture(pos, color, uv);
            
            // bottom left
            uv.X= 0.0f; uv.Y=1.0f;     pos.X=-BORDER; pos.Y=0.0f; pos.Z= BORDER;
            groundVertices[1]  = new VertexPositionColorTexture(pos, color, uv);
            
            // top right
            uv.X=1.0f; uv.Y= 0.0f;     pos.X= BORDER; pos.Y=0.0f; pos.Z=-BORDER;
            groundVertices[2]  = new VertexPositionColorTexture(pos, color, uv);
            
            // bottom right
            uv.X=1.0f; uv.Y=1.0f;     pos.X= BORDER; pos.Y=0.0f; pos.Z= BORDER;
            groundVertices[3]  = new VertexPositionColorTexture(pos, color, uv);
        }

        /// <summary>
        /// Executes set-up routines when program begins. 
        /// </summary>
        protected override void Initialize()
        {
            enemies_in_a_wave = 1;

            revenge = new SoundEffect[4];
            revenge[0] = Content.Load<SoundEffect>("Sounds\\Revenge\\townassault_rcs_sc09_02_t1E");
            revenge[1] = Content.Load<SoundEffect>("Sounds\\Revenge\\townassault_rcs_sc09_03_t1E");
            revenge[2] = Content.Load<SoundEffect>("Sounds\\Revenge\\townassault_rcs_sc09_05_t2E");
            revenge[3] = Content.Load<SoundEffect>("Sounds\\Revenge\\townassault_rcs_sc10_02_t3E");
            sfi = revenge[0].CreateInstance();

            time = new System.Diagnostics.Stopwatch(); time.Restart();
            cam_velocity = Vector3.Zero;
            rnd = new Random();
            blood = Content.Load<Texture2D>("Images\\blood");
            headshot = Content.Load<SoundEffect>("Sounds\\headshot2");
            terrain = Content.Load<Terrain>("Images\\heightMap");

            for (int i = 0; i < terrain.position.Length; i++)
                terrain.position[i] *= 1;

            Random rand = new Random();
            InitializeModels();

            gun = new Gun(this); gun.Initialize();

          
            Vector3 color = new Vector3( (float) rand.NextDouble() , (float) rand.NextDouble(), (float) rand.NextDouble() );
            
            enemies = new Enemy(this, cam.position);
            
            InitializeBaseCode();
            InitializeGround();
            InitializeSkybox();
            InitializeRoutes();
            InitializeTimeLine();
            InitializeWallSpheres();
            InitializeModelSpheres();

            InitializeIndices();
            InitializeVertexBuffer();
            InitializeBasicEffect();

            footstep1 = Content.Load<SoundEffect>("Sounds\\pl_step");
            footstepInstance1 = footstep1.CreateInstance();
            footstepInstance1.IsLooped = true;
            
            win = Content.Load<Song>("Sounds\\I feel good");
            lose = Content.Load<Song>("Sounds\\patmat");
            menu = Content.Load<Song>("Sounds\\menu");

            ambience = Content.Load<SoundEffect>("Sounds\\ambience");
            scary_horror = Content.Load<SoundEffect>("Sounds\\Scary-Horror");
            menu_music = Content.Load<SoundEffect>("Sounds\\Futurama");
            game_music = Content.Load<Song>("Sounds\\gamestartup");
            scary_horror_instance = scary_horror.CreateInstance();
            scary_horror_instance.IsLooped = true;
            MediaPlayer.IsRepeating = true; MediaPlayer.Volume = .1f;
            ambienceInstance = ambience.CreateInstance(); ambienceInstance.IsLooped = true;
            haha = Content.Load<SoundEffect>("Sounds\\haha");


            scary_horror_instance.Volume = .1f;
            
            powerups = new powerup(this, cam.position);
            powerups.Initialize();

            MediaPlayer.Play(menu);


            base.Initialize();
        }

        /// <summary>
        /// Draws colored surfaces with PositionColor.fx shader. 
        /// </summary>
        /// <param name="primitiveType">Object type drawn with vertex data.</param>
        /// <param name="vertexData">Array of vertices.</param>
        /// <param name="numPrimitives">Total primitives drawn.</param>
        public void PositionColorShader(PrimitiveType         primitiveType,
                                         VertexPositionColor[] vertexData,
                                         int                   numPrimitives){
            //positionColorEffect.Begin(); // begin using PositionColor.fx
            positionColorEffect.Techniques[0].Passes[0].Apply();

            // set drawing format and vertex data then draw primitive surface
            //graphics.GraphicsDevice.VertexDeclaration = positionColor;
            graphics.GraphicsDevice.DrawUserPrimitives<VertexPositionColor>(
                                    primitiveType, vertexData, 0, numPrimitives);

            //positionColorEffect.Techniques[0].Passes[0].End();
            //positionColorEffect.End();  // stop using PositionColor.fx
        }

        /// <summary>
        /// Draws textured primitive objects using Texture.fx shader. 
        /// </summary>
        /// <param name="primitiveType">Object type drawn with vertex data.</param>
        /// <param name="vertexData">Array of vertices.</param>
        /// <param name="numPrimitives">Total primitives drawn.</param>
        private void TextureShader(PrimitiveType                primitiveType,
                                   VertexPositionColorTexture[] vertexData,
                                   int                          numPrimitives){
            //textureEffect.Begin(); // begin using Texture.fx
            textureEffect.Techniques[0].Passes[0].Apply();

            // set drawing format and vertex data then draw surface
            //graphics.GraphicsDevice.VertexDeclaration = positionColorTexture;
            graphics.GraphicsDevice.DrawUserPrimitives
                                    <VertexPositionColorTexture>(
                                    primitiveType, vertexData, 0, numPrimitives);

            //textureEffect.Techniques[0].Passes[0].End();
            //textureEffect.End(); // stop using Textured.fx
        }

        Vector3 OffsetFromCamera()
        {
            const float PLANEHEIGHOFFGROUND = 0.195f;
            Vector3 offsetFromCamera = new Vector3(0.0f, PLANEHEIGHOFFGROUND, 2.1f);
            return offsetFromCamera;
        }

        Matrix TransformPlane(Camera camera)
        {
            // 1: declare matrices and other variables
            Matrix translate, rotateX, rotateY;
            
            // 2: initialize matrices
            translate = Matrix.CreateTranslation(currentPosition);
            rotateX = Matrix.CreateRotationX(0.0f);
            rotateY = Matrix.CreateRotationY((float)Math.PI + Yrotation);

            // 3: buil cumulative world matrix
            return rotateY * rotateX * translate;
        }

        float planeYDirection(Matrix tempCam)
        {
            return (float)Math.Atan2(tempCam.Forward.X - tempCam.Translation.X, tempCam.Forward.Z - tempCam.Translation.Z);
        }

        private void DrawSpheres(int group, Camera camera, GameTime gameTime)
        {
            // 1: declare matrices
            Matrix world;
            
            for (int j = 0; j < sphereGroup[group].sphere.Count; j++)
            {
                if (sphereGroup[group].Show)
                {
                    world = Matrix.Identity;
                    // draw plane spheres
                    if (group == (int)Group.plane || group == (int)Group.bigPlane)
                        world = ScaleSpheres() * TransformPlane(camera);
                    
                    // 4: set variables in shader
                    positionColorEffectWVP.SetValue(world * cam.viewMatrix * cam.projectionMatrix);

                    // 5: draw object - primitive type, vertex data, # primitives
                    PositionColorShader(sphereGroup[group].primitiveType, sphereGroup[group].sphere[j].vertices, sphereGroup[group].PrimitiveCount);

                }
            }
        }

        void DrawWindmill(Model model, int modelNum, GameTime gameTime)
        {
            graphics.GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise; // don't draw backfaces

            foreach (ModelMesh mesh in model.Meshes)
            {
                // 1: declare matrices
                Matrix world, scale, rotationZ, translation;

                // 2: initialize matrices
                scale = Matrix.CreateScale(0.1f, 0.1f, 0.1f);
                translation = Matrix.CreateTranslation(0.0f, 0.9f, -4.0f);
                rotationZ = Matrix.CreateRotationZ(0.0f);

                if (modelNum == WINDMILL_FAN)
                {
                    // calculate time betweeen frames for system independent speed
                    fanRotation += gameTime.ElapsedGameTime.Ticks / 6000000.0f;
                    // prevent var overflow - store remainder
                    fanRotation = fanRotation % (2.0f * (float)Math.PI);
                    rotationZ = Matrix.CreateRotationZ(fanRotation);
                }

                // 3: build cumulative world matrix using I.S.R.O.T sequence
                // identity, scale, rotate, orbit(translate&rotate), translate
                world = scale * rotationZ * translation;

                // 4: set shader parameters
                foreach (BasicEffect effect in mesh.Effects)
                {
                    if (modelNum == WINDMILL_BASE)
                        effect.World = baseMatrix[mesh.ParentBone.Index] * world;
                    if (modelNum == WINDMILL_FAN)
                        effect.World = fanMatrix[mesh.ParentBone.Index] * world;
                    
                    effect.View = cam.viewMatrix;
                    effect.Projection = cam.projectionMatrix;
                    effect.EnableDefaultLighting();
                }

                // 5: draw object
                mesh.Draw();
            }
            // stop culling
            graphics.GraphicsDevice.RasterizerState = RasterizerState.CullNone;
        }

        void DrawImage(Rectangle rect, Texture2D texture)
        {
            got_hit -= 0.01f;
            
            spriteBatch.Begin();
            spriteBatch.Draw(
                // texture drawn
                texture, rect, Color.White * got_hit);

            spriteBatch.End();
        }
        // Draw animated hud
        void DrawAnimatedHUD(GameTime gameTime, Vector2 startPixel, Texture2D texture, int numFrames)
        {
            // get width and height of the section of the image to be drawn
            int width = texture.Width;  // measured in pixels
            int height = texture.Height / numFrames; // measured in pixels

            if (Timer(gameTime))
            {
                frameNum += 1;                      // swap image frame
                frameNum = frameNum % numFrames;    // set to 0 after last frame
            }
            spriteBatch.Begin();
            spriteBatch.Draw(
                // texture drawn
                texture,

                // area of window used for drawing
                new Rectangle((int)startPixel.X, // starting X window position
                    (int)startPixel.Y,            // starting Y window position
                    width, height),               // area of window used
                // area of image that is drawn
                    new Rectangle(0,              // starting X pixel in texture
                        frameNum * height,          // starting Y pixel in texture
                        width, height),           // area of image used

                    // color
                    Color.White);

            spriteBatch.End();
        }

        // Draw the skybox
        private void DrawSkybox()
        {
            const float DROP = -1.2f;

            // 1: declare matrices and set defaults
            Matrix world;
            Matrix rotationY = Matrix.CreateRotationY(0.0f);
            Matrix rotationX = Matrix.CreateRotationX(0.0f);
            Matrix translation = Matrix.CreateTranslation(0.0f, 0.0f, 0.0f);
            Matrix camTranslation  // move skybox with camera
                = Matrix.CreateTranslation(cam.position.X, 0.0f, cam.position.Z);

            // 2: set transformations and also texture for each wall
            for (int i = 0; i < 5; i++)
            {
                switch (i)
                {
                    case 0: // BACK
                        translation = Matrix.CreateTranslation(0.0f, DROP, EDGE);
                        textureEffectImage.SetValue(backTexture); break;
                    case 1: // RIGHT
                        translation = Matrix.CreateTranslation(EDGE, DROP, 0.0f);
                        rotationY = Matrix.CreateRotationY((float)Math.PI / 2.0f);
                        textureEffectImage.SetValue(rightTexture); break;
                    case 2: // FRONT
                        translation = Matrix.CreateTranslation(0.0f, DROP, -EDGE);
                        rotationY = Matrix.CreateRotationY((float)Math.PI);
                        textureEffectImage.SetValue(frontTexture); break;
                    case 3: // LEFT
                        translation = Matrix.CreateTranslation(-EDGE, DROP, 0.0f);
                        rotationY = Matrix.CreateRotationY(-(float)Math.PI / 2.0f);
                        textureEffectImage.SetValue(leftTexture); break;
                    case 4: // SKY
                        translation = Matrix.CreateTranslation(0.0f, EDGE + DROP, 0.0f);
                        rotationX = Matrix.CreateRotationX(-(float)Math.PI / 2.0f);
                        rotationY = Matrix.CreateRotationY(3.0f * MathHelper.Pi / 2.0f);
                        textureEffectImage.SetValue(skyTexture); break;
                }

                // 3: build cumulative world matrix using I.S.R.O.T sequence
                world = rotationX * rotationY * translation * camTranslation;

                // 4: set shader variables
                textureEffectWVP.SetValue(world * cam.viewMatrix * cam.projectionMatrix);

                // 5: draw object - primitive type, vertices, # primitives
                TextureShader(PrimitiveType.TriangleStrip, skyVertices, 2);
            }
        }
        /// <summary>
        /// Triggers drawing of ground with texture shader.
        /// </summary>
        /// 
        private void DrawGround()
        {
            // 1: declare matrices
            Matrix world, translation;

            // 2: initialize matrices
            translation = Matrix.CreateTranslation(0.0f, 0.0f, 0.0f);
            
            // 3: build cumulative world matrix using I.S.R.O.T. sequence
            // identity, scale, rotate, orbit(translate & rotate), translate
            world       = translation;

            // 4: set shader parameters
            textureEffectWVP.SetValue(world * cam.viewMatrix * cam.projectionMatrix);
            //textureEffectImage.SetValue(grassTexture);
            textureEffectImage.SetValue(groundTexture);
            
            // 5: draw object - primitive type, vertex data, # primitives
            TextureShader(PrimitiveType.TriangleStrip, groundVertices, 2);
        }

        private void DrawCF18(Model model)
        {
            // 1: declare matrices
            Matrix scale, translate, rotateX, rotateY, world;

            // 2: initialize matrices
            translate = Matrix.CreateTranslation(currentPosition);
            scale = Matrix.CreateScale(0.3f, 0.3f, 0.3f);
            rotateX = Matrix.CreateRotationX(0.0f);
            rotateY = Matrix.CreateRotationY(Yrotation);

            // 3: build cumulative world matrix using I.S.R.O.T sequence
            // identity, scale, rotate, orbit(translate & rotate), translate
            world = scale * rotateX * rotateY * translate;

            // set shader parameters
            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.World = jetMatrix[mesh.ParentBone.Index] * world;
                    effect.View = cam.viewMatrix;
                    effect.Projection = cam.projectionMatrix;
                    effect.EnableDefaultLighting();
                    effect.SpecularColor = new Vector3(0.0f, 0.0f, 0.0f);
                }
                mesh.Draw();
            }
        }
        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            font = Content.Load<SpriteFont>("Font");

            // create SpriteBatch object for drawing animated 2D images
            spriteBatch     = new SpriteBatch(GraphicsDevice);

            // load texture
            grassTexture    = Content.Load<Texture2D>("Images\\grass");

            // load skybox textures
            frontTexture = Content.Load<Texture2D>("Images\\front");
            backTexture = Content.Load<Texture2D>("Images\\back");
            leftTexture = Content.Load<Texture2D>("Images\\left");
            rightTexture = Content.Load<Texture2D>("Images\\right");
            skyTexture = Content.Load<Texture2D>("Images\\sky");
            groundTexture = Content.Load<Texture2D>("Images\\grass");

            // load HUD
            spriteTexture = Content.Load<Texture2D>("Images\\warninglight");

            // load windmill
            baseModel = Content.Load<Model>("Models\\base");
            baseMatrix = new Matrix[baseModel.Bones.Count];
            baseModel.CopyAbsoluteBoneTransformsTo(baseMatrix);

            fanModel = Content.Load<Model>("Models\\fan");
            fanMatrix = new Matrix[fanModel.Bones.Count];
            fanModel.CopyAbsoluteBoneTransformsTo(fanMatrix);

            // load jet
            jetModel = Content.Load<Model>("Models\\cf18");
            jetMatrix = new Matrix[jetModel.Bones.Count];
            jetModel.CopyAbsoluteBoneTransformsTo(jetMatrix);

            // load rocket
            InitializeModels();

            spriteFont = Content.Load<SpriteFont>("font");


            
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Updates camera viewer in forwards and backwards direction.
        /// </summary>
        float Move()
        {
            KeyboardState kb    = Keyboard.GetState();
            GamePadState  gp    = GamePad.GetState(PlayerIndex.One);
            float         move  = 0.0f;
            const float   SCALE = 3.0f;

            // gamepad in use
            if (gp.IsConnected)
            {
                // left stick shifted left/right
                if (gp.ThumbSticks.Left.Y != 0.0f)
                    move = (SCALE * gp.ThumbSticks.Left.Y);
            }
            // no gamepad - use UP&DOWN or W&S
            else
            {
#if !XBOX
                if (kb.IsKeyDown(Keys.Up) || kb.IsKeyDown(Keys.W))
                    move = 1.0f * SCALE;  // Up or W - move ahead
                else if (kb.IsKeyDown(Keys.Down) || kb.IsKeyDown(Keys.S))
                    move = -1.0f * SCALE; // Down or S - move back
                if (kb.IsKeyDown(Keys.LeftShift))
                    move *= .4f;
#endif
            }

            if (move != 0) moving = true;

            Vector3 look = cam.view-cam.position; look.Normalize();
            cam_velocity = move * look;
            
            return move * SCALE;         
        }

        /// <summary>
        /// Updates camera viewer in sideways direction.
        /// </summary>
        float Strafe()
        {
            KeyboardState kb = Keyboard.GetState();
            GamePadState  gp = GamePad.GetState(PlayerIndex.One);
            float scale = 1.0f, move = 0.0f;
            // using gamepad leftStick shifted left / right for strafe
            if (gp.IsConnected)
            {
                if (gp.ThumbSticks.Left.X != 0.0f)
                    return gp.ThumbSticks.Left.X;
            }
            // using keyboard - strafe with Left&Right or A&D
            else if (kb.IsKeyDown(Keys.Left) || kb.IsKeyDown(Keys.A))
                move = -1.0f * scale; // strafe left
            else if (kb.IsKeyDown(Keys.Right) || kb.IsKeyDown(Keys.D))
                move = 1.0f * scale;  // strafe right
            if (kb.IsKeyDown(Keys.LeftShift))
                move *= .4f;

            if (move != 0) moving = true;
            Vector3 look = cam.view-cam.position; look.Normalize();
            cam_velocity = move * Vector3.Cross(look, cam.up);

            return move;
        }

        /// <summary>
        /// Changes camera viewing angle.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        Vector2 ChangeView(GameTime gameTime)
        {
            const float SENSITIVITY         = .5f;
            const float VERTICAL_INVERSION  =  -1.0f; // vertical view control
                                                      // negate to reverse

            // handle change in view using right and left keys
            KeyboardState kbState      = Keyboard.GetState();
            int           widthMiddle  = Window.ClientBounds.Width/2;
            int           heightMiddle = Window.ClientBounds.Height/2;
            Vector2       change       = Vector2.Zero;
            GamePadState  gp           = GamePad.GetState(PlayerIndex.One);

            if (gp.IsConnected == true) // gamepad on PC / Xbox
            {
                float scaleY = VERTICAL_INVERSION*(float)
                               gameTime.ElapsedGameTime.Milliseconds/50.0f;
                change.Y     = scaleY * gp.ThumbSticks.Right.Y * SENSITIVITY;
                change.X     = gp.ThumbSticks.Right.X * SENSITIVITY;
            }
            else
            { 
            // use mouse only (on PC)
            #if !XBOX
                float scaleY = VERTICAL_INVERSION*(float)
                               gameTime.ElapsedGameTime.Milliseconds/100.0f;
                float scaleX = (float)gameTime.ElapsedGameTime.Milliseconds/400.0f;

                // get cursor position
                mouse        = Mouse.GetState();
                
                // cursor not at center on X
                if (mouse.X != widthMiddle)
                {
                    change.X  = mouse.X - widthMiddle;
                    change.X /= scaleX;
                    change.X *= SENSITIVITY;
                }
                // cursor not at center on Y
                if (mouse.Y != heightMiddle)
                {
                    change.Y  = mouse.Y - heightMiddle;
                    change.Y /= scaleY;
                    change.Y *= SENSITIVITY;
                }
                // reset cursor back to center
                Mouse.SetPosition(widthMiddle, heightMiddle);
            #endif
            }
            return change;
        }

        private void start_game()
        {
            if (!game_has_started)
            {
                HP = 100;
                score = 0; total_score = 0;
                Initialize();
                game_has_started = true;
                GameOver = "NO";
                ambienceInstance.Play();
                ambienceInstance.Volume = .4f;
                MediaPlayer.Play(game_music);
            }
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        /// 
        bool moving = false;
        protected override void Update(GameTime gameTime)
        {
            // Allows the game to exit
            KeyboardState kbState = Keyboard.GetState();
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
                || kbState.IsKeyDown(Keys.Escape))
            {
                highscore_text = "";
                foreach (int s in high_scores)
                    highscore_text += s.ToString() + "\n";
                System.IO.File.WriteAllText(@"highscores.txt", highscore_text);

                this.Exit();
            }
            if (kbState.IsKeyDown(Keys.N))
            {
                deadman_walking = false;
                start_game();
            }
            if (kbState.IsKeyDown(Keys.D))
            {
                deadman_walking = true;
                start_game();
            }
            Vector3 look = cam.view - cam.position;
            cam.position = inbound(cam.position); cam.view = cam.position + look;

            if (gun.gun_clip == 0)
                if (more_than_one_second_has_passed(2f))
                    NUM_ENEMIES += 2;

            if (GameOver == "NO" && game_has_started)
            {
                cam.up = ProjectedUp(cam.position, cam.direction, 3);
                BASE_HEIGHT = cam.view.Y - 0.03f;
                powerups.Update(gameTime);
                gun.Update(gameTime);
                
                if (kbState.IsKeyDown(Keys.R))
                    gun.Reload();
                if (game_has_started)
                {
                    if (enemies.active_enemies() == 0)
                    {
                        spawn_enemy_wave();
                        if (enemies_in_a_wave < 10)
                            enemies_in_a_wave++;
                    }

                }

                // update camera
                cam.SetFrameInterval(gameTime);
                cam.Move(Move());
                cam.Strafe(Strafe());
                cam.SetView(ChangeView(gameTime));
                UpdateCameraHeight();
               // UpdateShipPosition(gameTime);

                UpdateKeyframeAnimation(gameTime);

                if (moving)
                    footstepInstance1.Play();
                else
                    footstepInstance1.Stop();
                moving = false;
                // refresh key and button states

#if !XBOX
                mouseCurrent = Mouse.GetState();
#endif
                gamepad = GamePad.GetState(PlayerIndex.One);

                // launch rocket for right trigger and left click events
                if (gamepad.Triggers.Right > 0 && gamepadPrevious.Triggers.Right == 0
#if !XBOX
 || mouseCurrent.LeftButton == ButtonState.Pressed
                    && mousePrevious.LeftButton == ButtonState.Released
#endif
)
                { 
                    gun.add_shot();
                }
                
                // archive current state for camparison next frame
                gamepadPrevious = gamepad;
#if !XBOX
                mousePrevious = mouseCurrent;
#endif

                enemies.Update(gameTime);
      
            }
            
            base.Update(gameTime);
        }

        void DrawString(String msg)
        {
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            spriteBatch.DrawString(font, msg, new Vector2(152, 152), Color.White);
            spriteBatch.End();
        }


        public void DrawMessage(string message)
        {
            if (!BeginDraw())   // starts drawing of frame
                return;
            GraphicsDevice.Clear(Color.CornflowerBlue);
            DrawString(message);
            EndDraw();  // ends drawing of frame
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            if (game_has_started)
            {
                DrawIndexedGrid(groundTexture, Matrix.Identity);

                DrawSkybox();
                powerups.Draw();
                gun.Draw(gameTime);

                DrawCF18(jetModel);

                for (int i = 0; i < TOTAL_SPHERE_GROUPS; i++)
                    DrawSpheres(i, cam, gameTime);

                enemies.Draw();

                const int NUM_FRAMES = 2;
                Rectangle safeArea = TitleSafeRegion(spriteTexture, NUM_FRAMES);
                Vector2 startPixel = new Vector2(safeArea.Left, safeArea.Bottom);

                if (HP < 30)
                    DrawAnimatedHUD(gameTime, startPixel, spriteTexture, NUM_FRAMES);

                if (got_hit >0){
                    Rectangle rect = new Rectangle(0, 0, Window.ClientBounds.Width, Window.ClientBounds.Height);
                    
                    DrawImage( rect, blood);
                }
            }
                // This part should always be in the end
                //
                //////////////////////////////////////////////////////////////////////
            
                string outputString = "";
                if (GameOver == "NO" && game_has_started)
                    outputString += "Score : " + total_score.ToString() + "\n" +
                        "HP : " + HP.ToString() + "\n" +
                        gun.gun_clip.ToString() + "/" + gun.gun_clip_limit.ToString() + "\n" +
                        gun.bullets.ToString();

                else if (GameOver == "NO")
                    outputString += "Press N for new game.\nPress D for dead man walking mode.\n\nControls:\n   w   move forward\n   a   strafe left\n   s   move backwards\n" +
                        "   d   strafe right\n\n    r   reload\n    left mouse button   shoot";
                else
                {
                    if (GameOver == "WIN")
                    {
                        outputString += "YOU WIN!\n\n\n\nPress N for new game.\nPress D for dead man walking mode.\nPress ESC to quit";
                    }
                    else if (GameOver == "LOSE")
                    {
                        outputString += "YOU LOSE\n\n\n\nPress N for new game.\nPress D for dead man walking mode.\nPress ESC to quit";
                    }

                    if (deadman_walking)
                    {
                        high_scores.Sort();
                        if (high_scores.Count > 0)
                            outputString += "\n High Scores:";
                        foreach (int s in high_scores)
                            outputString += "\n     " + s.ToString();
                    }
                }

                DisplayStats(outputString);



                
            
            // A: Culling off   B: Enable 3D    C: transparency off     D: pixel testing
            graphics.GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            GraphicsDevice.BlendState = BlendState.Opaque;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;

            // re-enable tiling
            SamplerState temp = new SamplerState();
            temp.AddressU = TextureAddressMode.Wrap;
            temp.AddressV = TextureAddressMode.Wrap;
            graphics.GraphicsDevice.SamplerStates[0] = temp;
           
            base.Draw(gameTime);
        }
    }
}
