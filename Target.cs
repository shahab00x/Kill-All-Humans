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
using Projectiles;

namespace MGHGame
{
    /// <summary>
    /// This is a game component that implements IUpdateable.
    /// </summary>
    // This class supports all the targets in the game.
    public class Target : Microsoft.Xna.Framework.GameComponent
    {
        Model model; Matrix[] modelMatrix;
        protected Game game;
        public Vector3 position;
        protected Camera cam;
        protected Model modelSpheres;  Matrix[] modelSpheresMatrix;
        protected Matrix world;
        protected SoundEffect soundEffect;
        protected SoundEffectInstance sfi;
        protected SoundEffect walking;
        protected SoundEffectInstance walkingInstance;

        Vector3 color;
        public float speed;
        Vector3 direction;
        public float scale = 10f;

        const int SLICES = 10;
        const int STACKS = 10;
        public List<SphereData> sphereList = new List<SphereData>();

        private int numPrimitives = 0;

        protected bool SHOW = false;

        public bool enabled = true;
        public void enable() { enabled = true; }
        public virtual void disable() { enabled = false; }

        System.Diagnostics.Stopwatch time;

        // This is the vector that gets added to the direction vector to simulate wandering.
        protected Vector3 wander_vector;


        protected Matrix ScaleModel()
        {
            return Matrix.CreateScale(scale, scale, scale);
        }

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
        
        // find a direction to wander off to. (this gets added to the original direction)
        protected virtual Vector3 wander(Vector3 dir)
        {
            if (more_than_one_second_has_passed())
            {
                wander_vector = ((Game1)game).wander(dir, Vector3.Up);
                wander_vector.Y = 0;
                wander_vector.Normalize();
            }
            return wander_vector;
        }

        public virtual bool detect_collision(BoundingSphere A)
        {
            foreach (SphereData s in sphereList)
            {
                BoundingSphere tempSphere =
                        s.boundingSphere.Transform( world);

                tempSphere.Radius
                    = s.boundingSphere.Radius * scale;

                if (Collision(A, tempSphere))
                    return true;
            }
            return false;
        }

        // When a collision is detected this function is run
        public virtual void collision_event_action()
        {
            AudioListener al = new AudioListener();
            al.Position = cam.position;
            AudioEmitter am = new AudioEmitter();
            am.Position = position;
            sfi.Apply3D(al, am);
            sfi.Play();

            ( (Game1)game).adjust_score_and_hp(false);

            enabled = false;
        }

        public Target(Game game, Camera cam, Model model, Vector3 position, Model modelSpheres, Vector3 color)
            : base(game)
        {
            // TODO: Construct any child components here
            this.model = model;
            this.game = game;
            this.position = position;
            this.cam = cam;
            this.modelSpheres = modelSpheres;
            this.color = color;
            this.speed = 3.0f;
            this.SHOW = ((Game1)game).SHOW;
        }

        float last_time = 0f;

        // The following function returns true if one second has passed. Because I need to do many things every one second, not every frame.
        public bool more_than_one_second_has_passed()
        {
            if (last_time > time.Elapsed.Seconds) last_time = 0;
            if (time.Elapsed.Seconds - last_time > 1f)
            {
                last_time = time.Elapsed.Seconds;
                return true;
            }
            return false;
        }

        public void spawn(Vector3 direction, Vector3 start)
        {
            this.position = start;
            this.direction = direction;
            enable();
        }

        public void move(Vector3 pos, bool absolute=false){
            if (!absolute)
                this.position += pos;
            else
                this.position = pos;
        }

        public void change_sound(SoundEffect se)
        {
            soundEffect = se;
            sfi = se.CreateInstance();
        }
        public void move_towards(Vector3 center, GameTime gameTime)
        {
            Vector3 dir = new Vector3(-position.X + center.X, 0.0f, -position.Z + center.Z);
            direction = dir;
            dir.Normalize();
            dir = dir + wander(dir);
            dir.Normalize();
            this.position = this.position + speed * dir *(float)gameTime.ElapsedGameTime.Milliseconds / 4090.0f;
            this.position.Y = ((Game1)game).CellHeight(position);
        }
        public void move_towards_3d(Vector3 center, GameTime gameTime)
        {
            if ((position - center).X < 0.001f && (position - center).Y < 0.001f && (position - center).Z < 0.001f)
                return;
            Vector3 dir = new Vector3(-position.X + center.X, -position.Y + center.Y, -position.Z + center.Z);
            direction = dir;
            dir.Normalize();
            this.position = this.position + speed * dir * (float)gameTime.ElapsedGameTime.Milliseconds / 4090.0f;
        }
        /// <summary>
        /// Allows the game component to perform any initialization it needs to before starting
        /// to run.  This is where it can query for any required services and load content.
        /// </summary>
        public override void Initialize()
        {
            // TODO: Add your initialization code here
            modelMatrix = new Matrix[model.Bones.Count];
            model.CopyAbsoluteBoneTransformsTo(modelMatrix);

            modelSpheresMatrix = new Matrix[modelSpheres.Bones.Count];
            modelSpheres.CopyAbsoluteBoneTransformsTo(modelSpheresMatrix);

            soundEffect = game.Content.Load<SoundEffect>("Sounds\\f_scream1");
            walking = game.Content.Load<SoundEffect>("Sounds\\walking");
            walkingInstance = walking.CreateInstance();
            AudioEmitter am = new AudioEmitter(); am.Position = position;
            AudioListener al = new AudioListener(); al.Position = cam.position;
            walkingInstance.Apply3D(al, am);
            walkingInstance.IsLooped = true;

            sphereList = new List<SphereData>();

            ExtractBoundingSphere(modelSpheres, Color.Green);

            sfi = soundEffect.CreateInstance();

            time = new System.Diagnostics.Stopwatch(); time.Restart();
            base.Initialize();
        }

        /// <summary>
        /// Allows the game component to update itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Update(GameTime gameTime)
        {
            // TODO: Add your update code here
            if (!enabled || !((Game1)game).game_has_started)
            {
                walkingInstance.Stop();
                return;
            }
            else
            {
                AudioEmitter am = new AudioEmitter(); am.Position = position;
                AudioListener al = new AudioListener(); al.Position = cam.position;
                walkingInstance.Apply3D(al, am);
                if (walkingInstance.State != SoundState.Playing)
                    walkingInstance.Play();
            }
            this.position = ((Game1)game).inbound(this.position);

            int group = (int)Game1.Group.rocketSpheres;
            for (int i = 0; i < Gun.NUM_ROCKETS ; i++)
                if ( ((Game1)Game).gun.rocket[i].active )
                    for (int j = 0; j < ((Game1)Game).sphereGroup[group].sphere.Count; j++)
                    {
                        //Matrix scale = Matrix.CreateScale(0.53f, 0.53f, 0.53f);
                        Matrix transfrom = ((Game1)game).gun.transformRocket(i);
                        BoundingSphere tempRocketSphere = ((Game1)Game).sphereGroup[group].sphere[j].boundingSphere.Transform(transfrom);
                        tempRocketSphere.Radius = ((Game1)Game).sphereGroup[group].sphere[j].boundingSphere.Radius;

                        if (detect_collision(tempRocketSphere) )
                        {
                            collision_event_action();
                            ((Game1)Game).gun.rocket[i].active = false;
                        }

                    }
            

            base.Update(gameTime);
        }

        void ExtractBoundingSphere(Model tempModel, Color color)
        {
            // set up model temporarily 
            Matrix[] tempMatrix = new Matrix[tempModel.Bones.Count];
            tempModel.CopyAbsoluteBoneTransformsTo(tempMatrix);

            // generate new sphere group
            BoundingSphere sphere = new BoundingSphere();
            
            foreach (ModelMesh mesh in tempModel.Meshes)
            {
                sphere = mesh.BoundingSphere;
                //Vector3 newCenter = sphere.Center;
                //Matrix transformationMatrix = ScaleModel();
                AddSphere(sphere.Radius , sphere.Center, color);
            }
        }

        public void AddSphere(float radius, Vector3 position, Color color)
        {
            SphereData sphereData = new SphereData();
            sphereData.boundingSphere.Center = position;
            sphereData.boundingSphere.Radius = radius;

            if (SHOW)
            {
                numPrimitives = SLICES * STACKS * 2 - 1;
                SphereVertices sphereVertices = new SphereVertices(color, numPrimitives, position);
                sphereData.vertices = sphereVertices.InitializeSphere(SLICES, STACKS, radius);
            }
            sphereList.Add(sphereData);
        }

        public float find_angle(Vector3 a, Vector3 b)
        {
            return (float)Math.Acos(Vector3.Dot(a, b) / (a.Length() * b.Length()));
        }

        public virtual void Draw()
        {
            if (!enabled) return;
            
            // 1: declare matrices
            Matrix translate, rotateY;

            // 2: initialize matrices
            translate = Matrix.CreateTranslation(position);           
            

            // 3: build cumulative world matrix using I.S.R.O.T sequence
            // identity, scale, rotate, orbit(translate & rotate), translate
            Vector3 f = -position + ((Game1)game).cam.position;
            float angle = (float)Math.Atan2((double)f.X, (double)f.Z);

            rotateY = Matrix.CreateRotationY(angle);

            world = ScaleModel() * translate;

            world = rotateY * world;
            
            
            // set shader parameters
            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.World = modelMatrix[mesh.ParentBone.Index] * world;
                    effect.View = cam.viewMatrix;
                    effect.Projection = cam.projectionMatrix;
                    effect.EnableDefaultLighting();
                    effect.AmbientLightColor = new Vector3(1.0f, 1.0f, 1.0f) * .2f;
                    effect.SpecularColor = new Vector3(1.0f, 1.0f, 1.0f) * .3f;
                    effect.DiffuseColor = this.color;
                }
                mesh.Draw();
            }
            
            for (int j = 0; j < sphereList.Count; j++)
            {
                if (SHOW)
                {
                    // 4: set variables in shader
                    ((Game1)game).positionColorEffectWVP.SetValue(world * cam.viewMatrix * cam.projectionMatrix);

                    // 5: draw object - primitive type, vertex data, # primitives
                    ((Game1)game).PositionColorShader(PrimitiveType.LineStrip, sphereList[j].vertices, numPrimitives);
                }
            }
        }

    }


    // This class acts as a model manager for all the enemies. It spawns them and moves them around.
    public class Enemy : Microsoft.Xna.Framework.GameComponent
    {
        private int NUM_ENEMIES = 10;
        private List<Target> enemies;
        public Vector3 center;
        private Game1 game;
        private Random rand;

        public Enemy(Game game, Vector3 center):base(game)
        {
            this.center = center;
            this.game = (Game1)game;
            enemies = new List<Target>();
            rand = ((Game1)game).rnd;
            for (int i = 0; i < NUM_ENEMIES; i++)
            {
                Target en = get_new_enemy();
                en.scale *= 8f;
                enemies.Add(en);
            }

            
        }

        public int active_enemies()
        {
            int count = 0;
            foreach (Target en in enemies)
                if (en.enabled) count++;

            return count;
        }
        private Target get_new_enemy(){
            Vector3 randomPoint = new Vector3( (float) rand.NextDouble() * 20f - 10, (float) rand.NextDouble() * 3f, (float) rand.NextDouble() * 20f - 10);
            randomPoint += new Vector3(0, ( (Game1)game).CellHeight(randomPoint), 0);
            Vector3 color = new Vector3( (float) rand.NextDouble() , (float) rand.NextDouble(), (float) rand.NextDouble() );
            Target en = new Target(game, ( (Game1)Game).cam, ( (Game1)game).humanModel, randomPoint, ( (Game1)game).humanSpheres, color);
            en.Initialize();
            en.disable();
            return en;
        }
        public void add_enemies(int num){
            Target enemy = get_new_enemy();
            if (num > NUM_ENEMIES)
                for (int i = NUM_ENEMIES; i < num; i++)
                    enemies.Add(enemy);
            else if (num < NUM_ENEMIES)
                for (int i = num; i < NUM_ENEMIES; i++)
                    enemies.Remove(enemy);
        }

        public void spawn_new_enemy(float radius)
        {
            float x = (float)(rand.NextDouble() -.5) * radius;
            float z = radius * (float)Math.Sin(Math.Acos(x / radius)); //(float)Math.Sqrt(-Math.Pow(x - center.X, 2) + Math.Pow(radius, 2));
            if (rand.Next(0,2) ==1)
                z = -z;
            z += center.Z; x += center.X;
            float y = game.CellHeight(new Vector3(x, 0.0f, z));
            Vector3 v = new Vector3(x, y, z);

            Vector3 direction = new Vector3(center.X-x, 0f, center.Z-z);

            foreach (Target en in enemies)
            {
                if (!en.enabled)
                {
                    en.spawn(direction, v);
                    en.speed = (float)rand.NextDouble() * 3f + 3f;
                    return;
                }
            }
        }

        private float distance(Vector3 a, Vector3 b)
        {
            a.Y= 0f; b.Y = 0f;
            return Vector3.Distance(a, b);
        }

        
        public override void Update(GameTime gameTime)
        {
            foreach (Target en in enemies)
            {
                if (en.enabled)
                {
                    en.move_towards(center, gameTime);
                    
                    if (distance(en.position, center) < 1.0f)
                    {
                        game.adjust_score_and_hp(true);
                        en.disable();
                    }

                    en.Update(gameTime);
                    
                }
            }
            center = ((Game1)game).cam.position;

            base.Update(gameTime);
        }

        public void Draw()
        {
            foreach (Target en in enemies)
            {
                if (en.enabled)
                    en.Draw();
            }
        }
    }

    // This the box class. It adds particle effects to the "Target" class, and also alters some sounds.
    public class Box : Target
    {

        //Explosion stuff
        ParticleExplosion explosion;
        ParticleExplosionSettings particleExplosionSettings = new ParticleExplosionSettings();
        ParticleSettings particleSettings = new ParticleSettings();
        Texture2D explosionTexture;
        Texture2D explosionColorsTexture;
        Effect explosionEffect;
        public String powerup_name = "not set";

        //public Game game;
        public Box(Game game, Camera cam, Model model, Vector3 position, Model modelSpheres, Vector3 color):
            base(game, cam, model, position, modelSpheres, color)
        {
            this.game = game;
        }
        public void stop_sounds()
        {
            sfi.Stop();
        }
        protected override Vector3 wander(Vector3 dir)
        {
            wander_vector = Vector3.Zero;
            return wander_vector;
        }
        public override void Initialize()
        {
            
            base.position = new Vector3(0, 3, 0);
            base.scale = 0.01f;

            base.Initialize();
            base.soundEffect = game.Content.Load<SoundEffect>("Sounds\\crate");

            // Load explosion textures and effect
            explosionTexture = Game.Content.Load<Texture2D>(@"Images\Particle");
            explosionColorsTexture = Game.Content.Load<Texture2D>(@"Images\ParticleColors");
            explosionEffect = Game.Content.Load<Effect>(@"Effects\particle");

            // Set effect parameters that don't change per particle
            explosionEffect.CurrentTechnique = explosionEffect.Techniques["Technique1"];
            explosionEffect.Parameters["theTexture"].SetValue(explosionTexture);

            particleSettings.maxSize *= scale * 2f ;



        }

        public override void Draw()
        {
            // draw each particle explosion
            if (explosion != null)
                explosion.Draw(((Game1)game).cam);
            base.Draw();
        }

        public override void collision_event_action()
        {
            ((Game1)game).power_up(powerup_name);

            explosion = new ParticleExplosion(game.GraphicsDevice,
                                (this.ScaleModel() * this.world).Translation,
                ((Game1)Game).rnd.Next(
                    particleExplosionSettings.minLife,
                    particleExplosionSettings.maxLife),
                                // particleExplosionSettings.maxLife,
                                ((Game1)Game).rnd.Next(
                                    particleExplosionSettings.minRoundTime,
                                    particleExplosionSettings.maxRoundTime),
                ((Game1)Game).rnd.Next(
                    particleExplosionSettings.minParticlesPerRound,
                    particleExplosionSettings.maxParticlesPerRound),
                               // particleExplosionSettings.maxParticlesPerRound,
                                ((Game1)Game).rnd.Next(
                                    particleExplosionSettings.minParticles,
                                    particleExplosionSettings.maxParticles),
                                explosionColorsTexture, particleSettings,
                                explosionEffect);
            
            disable();

            soundEffect.Play();

        }

        public override void disable()
        {
            if (((Game1)Game).powerups != null)
                ((Game1)Game).powerups.stop_sound();
            base.disable();
        }
        protected void UpdateExplosion(GameTime gameTime)
        {
            if (explosion != null)
                explosion.Update(gameTime);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            UpdateExplosion(gameTime);
        }
    }

    // This class acts as a model manager for all the power ups. It spawns them and moves them around.
    public class powerup : Microsoft.Xna.Framework.GameComponent
    {
        public Box gun_box, bullet_box;
        int gun_limit = 3, bullet_limit = 100;
        Game game;
        Vector3 center;
        Vector3 starting_position, goal_position;
        float gun_powerup_chance = .15f, bullet_powerup_chance = .3f;
        System.Diagnostics.Stopwatch stopwatch;
        float powerup_time = 12.00f;
        SoundEffect sound;
        SoundEffectInstance soundInstance;

        public powerup(Game game, Vector3 center): base(game)
        {
            this.game = game;
            this.center = center;
        }

        public override void Initialize()
        {
            gun_box = new Box(game, ((Game1)game).cam, ((Game1)game).crateModel, center, ((Game1)game).crateSpheres, new Vector3(128, 255, 0));
            bullet_box = new Box(game, ((Game1)game).cam, ((Game1)game).crateModel, center, ((Game1)game).crateSpheres, new Vector3(128, 255, 0));
            gun_box.disable();
            bullet_box.disable();
            gun_box.powerup_name = "gun";
            bullet_box.powerup_name = "bullet";
            gun_box.Initialize();
            bullet_box.Initialize();

            stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Reset();

            sound = ((Game1)game).Content.Load<SoundEffect>("Sounds\\tick");
            soundInstance = sound.CreateInstance(); soundInstance.IsLooped = true;
            soundInstance.Volume = 1f;
            base.Initialize();
        }

        public override void Update(GameTime gameTime)
        {
            update_box(gun_box, gameTime);
         
            update_box(bullet_box, gameTime);
         
            if (((Game1)game).more_than_one_second_has_passed(2f) )
            if (!gun_box.enabled && !bullet_box.enabled)
            {
                if (((Game1)game).rnd.NextDouble() < (double)gun_powerup_chance && gun_limit-- > 0)
                {
                    enable_box(gun_box);
                }

                else if (((Game1)game).rnd.NextDouble() < (double)bullet_powerup_chance && bullet_limit-- >0)
                {
                    enable_box(bullet_box);
                }

                
            }
            
            base.Update(gameTime);
        }

        public void stop_sound()
        {
            if (soundInstance != null)
                soundInstance.Stop();
        }
        protected void enable_box(Box box)
        {
            box.enable();
            starting_position = new Vector3((float)((Game1)game).rnd.NextDouble() * Game1.BOUNDARY,
                        (float)((Game1)game).rnd.NextDouble() * 5f + 5f,
                        (float)((Game1)game).rnd.NextDouble() * Game1.BOUNDARY );
            starting_position += center;

            starting_position = ((Game1)game).inbound(starting_position);

            goal_position = new Vector3(starting_position.X, ((Game1)game).CellHeight(starting_position) + .3f, starting_position.Z);
            box.position = starting_position;
            stopwatch.Restart();

            soundInstance.Play();
        }

        
        protected void update_box(Box box, GameTime gameTime)
        {
            if (stopwatch.Elapsed.Seconds < powerup_time)
            {
                box.move_towards_3d(goal_position, gameTime);
                box.Update(gameTime);
            }
            else
            {
                box.disable();
                stopwatch.Reset();
            }
        }
        public void Draw()
        {
            gun_box.Draw();
            bullet_box.Draw();
        }
    }
}
    