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
using CameraViewer;
using MGHGame;

namespace Projectiles
{
    public class Projectile
    {
        public Vector3 position, previousPosition; // Rocket position
        private Vector3 speed;                     // relative change in X, Y, Z
        public Matrix directionMatrix;             // direction transformations
        public bool active;                        // visibility
        private float boundary;                    // edge of world on X and Z
        private float seconds;                     // seconds since launch
        private Vector3 startPosition;             // launch position

        public Projectile(float border)
        {
            boundary = border;
            active = false;
        }

        public void Launch(Vector3 look, Vector3 start)
        {
            position = startPosition = start;   // start at camera
            speed = Vector3.Normalize(look);    // unitize direction
            active = true;                      // make visible
            seconds = 0.0f;                     // used with gravity only
        }

        private void SetDirectionMatrix()
        {
            Vector3 Look = position - previousPosition;
            Look.Normalize();
            Vector3 Up = new Vector3(0.0f, 1.0f, 0.0f); // fake Up to get

            Vector3 Right = Vector3.Cross(Up, Look);
            Right.Normalize();

            Up = Vector3.Cross(Look, Right);    // Calculate up with correct vectors
            Up.Normalize();

            Matrix matrix = new Matrix();   // Compute direction matrix
            matrix.Right = Right;
            matrix.Up = Up;
            matrix.Forward = Look;
            matrix.M44 = 1.0f;              // W is set to 1 to enable transforms
            directionMatrix = matrix;
        }

        public void UpdateProjectile(GameTime gameTime)
        {
            previousPosition = position; // archive lat position
            position += speed   // update current position
                * (float)gameTime.ElapsedGameTime.Milliseconds / 90.0f;
            SetDirectionMatrix();

            // deactivate if outer border exceeded on X or Z
            if (position.Z > 2.0f * boundary || position.X > 2.0f * boundary || position.Z < -2.0f * boundary || position.X < -2.0f * boundary)
                active = false;
        }
    }

    public class Gun : Microsoft.Xna.Framework.GameComponent
    {
        public const int NUM_ROCKETS = 40;
        Game game;
        Model launcherModel; Matrix[] launcherMatrix;
        Camera cam;
        public float GUN_RECOIL = 0.0f; const float GUN_RECOIL_MAX = MathHelper.Pi / 8.0f;
        public Projectile[] rocket = new Projectile[NUM_ROCKETS];
        Model rocketModel; Matrix[] rocketMatrix;

        public int gun_clip = 13, gun_clip_limit = 13, bullets = 65;
        SoundEffect reload, dryfire;
        SoundEffect gunshot;

        public Gun(Game game)
            : base(game)
        {
            this.game = game;
            this.cam = ((Game1)game).cam;
        }

        public void Draw(GameTime gameTime)
        {

            DrawLauncher(launcherModel);

            for (int i = 0; i < NUM_ROCKETS; i++)
                if (rocket[i].active)
                {
                    DrawRockets(rocketModel, i);
                    DrawRocketSpheres(i, cam, gameTime);
                }
        }

        public override void Update(GameTime gameTime)
        {
            for (int i = 0; i < NUM_ROCKETS; i++)
                if (rocket[i].active)
                    rocket[i].UpdateProjectile(gameTime);

            decrease_recoil();
            // archive current state for camparison next frame
            base.Update(gameTime);
        }

        public override void Initialize()
        {
            gunshot = ((Game1)game).Content.Load<SoundEffect>("Sounds\\gunshot");

            rocketModel = ((Game1)game).Content.Load<Model>("Models\\bullet");
            rocketMatrix = new Matrix[rocketModel.Bones.Count];
            rocketModel.CopyAbsoluteBoneTransformsTo(rocketMatrix);
            launcherModel = ((Game1)game).Content.Load<Model>("Models\\glock");
            launcherMatrix = new Matrix[launcherModel.Bones.Count];
            launcherModel.CopyAbsoluteBoneTransformsTo(launcherMatrix);

            for (int i = 0; i < NUM_ROCKETS; i++)
                rocket[i] = new Projectile(Game1.BOUNDARY);
            dryfire = ((Game1)game).Content.Load<SoundEffect>("Sounds\\dryfire_pistol");
            reload = ((Game1)game).Content.Load<SoundEffect>("Sounds\\generic_reload");

            base.Initialize();
        }
        public void add_shot()
        {
            if (gun_clip == 0)
            {
                dryfire.Play();
                return;
            }

            for (int i = 0; i < NUM_ROCKETS; i++)
                if (rocket[i].active == false)
                {
                    LaunchRocket(i);
                    GUN_RECOIL = GUN_RECOIL_MAX;
                    break;
                }
        }
        private void decrease_recoil()
        {
            if (GUN_RECOIL <= 0)
                GUN_RECOIL = 0;
            else
                GUN_RECOIL -= MathHelper.Pi / 128.0f;
        }
        public Matrix transformRocket(int i)
        {
            Matrix rotateX, translate;

            rotateX = Matrix.CreateRotationX(-MathHelper.Pi / 2.0f);
            translate = Matrix.CreateTranslation(rocket[i].position);

            return rotateX * rocket[i].directionMatrix * translate;

        }

        private void DrawRocketSpheres(int i, Camera camera, GameTime gameTime)
        {
            // 1: declare matrices
            Matrix scale, world;

            int group = (int) Game1.Group.rocketSpheres;
            for (int j = 0; j < ((Game1)game).sphereGroup[group].sphere.Count; j++)
            {
                if (((Game1)game).sphereGroup[group].Show)
                {
                    world = Matrix.Identity;

                    // 2: initialize matrices
                    scale = Matrix.CreateScale(0.53f, 0.53f, 0.53f);

                    // 3: build cumulative matrix using I.S.R.O.T. sequence
                    world = scale * transformRocket(i);


                    // 4: set variables in shader
                    ((Game1)game).positionColorEffectWVP.SetValue(world * cam.viewMatrix * cam.projectionMatrix);

                    // 5: draw object - primitive type, vertex data, # primitives
                    ((Game1)game).PositionColorShader(((Game1)game).sphereGroup[group].primitiveType, ((Game1)game).sphereGroup[group].sphere[j].vertices, ((Game1)game).sphereGroup[group].PrimitiveCount);
                }
            }
        }


        public void Reload()
        {
            if (bullets == 0 || gun_clip >= gun_clip_limit)
            {
                if (bullets == 0 && gun_clip == 0) bullets++;
                return;
            }
            else
            {
                reload.Play();
                int b = Math.Min(bullets, Math.Min( gun_clip_limit, gun_clip_limit-gun_clip));
                gun_clip += b;
                bullets -= b;
            }
        }
        private void DrawLauncher(Model model)
        {
            // 1: declare matrices
            Matrix world = gunWorldMatrix();

            // 4: set shader parameters
            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.World = launcherMatrix[mesh.ParentBone.Index] * world;
                    effect.View = cam.viewMatrix;
                    effect.Projection = cam.projectionMatrix;
                    effect.EnableDefaultLighting();
                    effect.SpecularPower = 0.01f;
                }

                // 5: draw object
                mesh.Draw();
            }
        }
        private Matrix gunWorldMatrix()
        {
            Matrix world, translation, scale, rotationX, rotationY, rotationZ;
            world = Matrix.Identity;
            // 2: initialize matrices
            scale = Matrix.CreateScale(0.1f, 0.1f, 0.1f);
            Vector3 look = cam.view - cam.position;
            Vector3 temp = cam.view; temp.Y = ((Game1)game).BASE_HEIGHT; temp -= GUN_RECOIL * look;
            translation = Matrix.CreateTranslation(temp);
            rotationX = Matrix.CreateRotationX((float)Math.Atan2(look.Y, Math.Sqrt(look.Z * look.Z + look.X * look.X)) + GUN_RECOIL);
            rotationY = Matrix.CreateRotationY((float)Math.PI + (float)Math.Atan2(look.X, look.Z));
            Vector3 t = cam.up; t.Z = 0f; t.Normalize();

            float angle = (float)Math.Acos((Vector3.Dot(t, Vector3.Up)));
            rotationZ = Matrix.CreateRotationZ(0);
            rotationZ.Up = cam.up;
            rotationZ.Right = Vector3.Cross(rotationZ.Forward, rotationZ.Up);
            rotationZ.Right = Vector3.Normalize(rotationZ.Right);
            rotationZ.Forward = Vector3.Cross(rotationZ.Up, rotationZ.Right);


            // 3: build cumulative matrix using I.S.R.O.T sequence
            // identity, scale, rotate, orbit, (translate & rotate), translate
            world = scale * rotationX * rotationY  * translation;
            return world; // Matrix.CreateTranslation(new Vector3(0, 0, -10)) * cam.viewMatrix;
        }

        public void LaunchRocket(int i)
        {
            Matrix orbitTranslate, orbitX, orbitY, translate, position;
            Vector3 look, start;

            // create matrix and store origin in first row
            position = new Matrix(); // zero matrix
            position.M14 = 1.0f;         // set W to 1 so you can transform it

            // move to tip of launcher
            orbitTranslate = Matrix.CreateTranslation(0.0f, 0.0f, -0.0f);

            // use same direction as launcher
            look = gunWorldMatrix().Forward;
            // offset needed to rotate rocket about X to see it with camera
            float offsetAngle = MathHelper.Pi;

            // adjust angle about X with changes in Look (Forward) direction
            orbitX = Matrix.CreateRotationX((float)Math.Atan2(look.Y, Math.Sqrt(look.Z * look.Z + look.X * look.X)) + GUN_RECOIL);
            orbitY = Matrix.CreateRotationY((float)Math.Atan2(look.X, look.Z));

            // move rocket to camera view where gun base is also located
            translate = Matrix.CreateTranslation(cam.view - GUN_RECOIL * look);

            // use the I.S.R.O.T. sequence to get rocket start position
            position = position * orbitTranslate * orbitX * orbitY * translate;

            // convert from matrix back to vector so it can be used for updates
            start = new Vector3(position.M11, position.M12, position.M13);

            rocket[i].Launch(look, start);
            gun_clip--;
            SoundEffectInstance gunshot_instance = gunshot.CreateInstance();
            gunshot_instance.Volume = .2f;
            gunshot_instance.Play();
        }

        private void DrawRockets(Model model, int i)
        {
            // 1: declare matrices
            Matrix world, scale, rotateX, translate;

            // 2: initialize matrices
            scale = Matrix.CreateScale(0.53f, 0.53f, 0.53f);
            rotateX = Matrix.CreateRotationX(-MathHelper.Pi / 2.0f);
            translate = Matrix.CreateTranslation(rocket[i].position);

            // 3: build cumulative matrix using I.S.R.O.T. sequence
            world = scale * rotateX * rocket[i].directionMatrix * translate;

            // 4: set shader parameters
            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.World = rocketMatrix[mesh.ParentBone.Index]
                                 * world;
                    effect.View = cam.viewMatrix;
                    effect.Projection = cam.projectionMatrix;
                    effect.EnableDefaultLighting();
                    effect.SpecularPower = 16.5f;
                }
                // 5: draw object
                mesh.Draw();
            }
        }

    }
}
