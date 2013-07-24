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
        private const float BOUNDARY = 16.0f;

        // accesses drawing methods and properties
        GraphicsDeviceManager graphics;

        // handle mouse on the PC
#if !XBOX
        MouseState mouse;
#endif

        // for loading and drawing 2D images on the game window
        SpriteBatch                 spriteBatch;

        // load and access PositionColor.fx shader
        private Effect              positionColorEffect;    // shader object
        private EffectParameter     positionColorEffectWVP; // to set display matrix for window

        // load and access Texture.fx shader
        private Effect              textureEffect;          // shader object                 
        private EffectParameter     textureEffectWVP;       // cumulative matrix w*v*p 
        private EffectParameter     textureEffectImage;     // texture parameter

        // camera 
        private Camera              cam = new Camera();

        // vertex types and buffers
        private VertexDeclaration   positionColor;
        private VertexDeclaration   positionColorTexture;

        // ground vertices and texture
        VertexPositionColorTexture[] 
            groundVertices = new    VertexPositionColorTexture[4];
        private Texture2D           grassTexture;

        /// <summary>
        /// Initializes:    -GraphicsDeviceManager object for drawing 
        ///                 -ContentManager object for loading media
        /// </summary>
        public Game1()
        {
            graphics              = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// This method is called when the program begins to set game application
        /// properties such as status bar title and draw mode.  It initializes the  
        /// camera viewer projection, vertex types, and shaders.
        /// </summary>
        private void InitializeBaseCode()
        {
            // set status bar in PC Window (there is none for the Xbox 360)
            Window.Title = "Microsoft® XNA Game Studio Creator's Guide, Second Edition";

            // see both sides of objects drawn
            graphics.GraphicsDevice.RenderState.CullMode = CullMode.None;
            
            // set camera matrix
            cam.SetProjection(Window.ClientBounds.Width, 
                              Window.ClientBounds.Height);

            // initialize vertex types
            positionColor           = new VertexDeclaration(graphics.GraphicsDevice,
                                          VertexPositionColor.VertexElements);
            positionColorTexture    = new VertexDeclaration(graphics.GraphicsDevice,
                                          VertexPositionColorTexture.VertexElements);

            // load PositionColor.fx and set global params
            positionColorEffect     = Content.Load<Effect>("Shaders\\PositionColor");
            positionColorEffectWVP  = positionColorEffect.Parameters["wvpMatrix"];

            // load Texture.fx and set global params
            textureEffect           = Content.Load<Effect>("Shaders\\Texture");
            textureEffectWVP        = textureEffect.Parameters["wvpMatrix"];
            textureEffectImage      = textureEffect.Parameters["textureImage"];
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
            uv.X= 0.0f; uv.Y=10.0f;     pos.X=-BORDER; pos.Y=0.0f; pos.Z= BORDER;
            groundVertices[1]  = new VertexPositionColorTexture(pos, color, uv);
            
            // top right
            uv.X=10.0f; uv.Y= 0.0f;     pos.X= BORDER; pos.Y=0.0f; pos.Z=-BORDER;
            groundVertices[2]  = new VertexPositionColorTexture(pos, color, uv);
            
            // bottom right
            uv.X=10.0f; uv.Y=10.0f;     pos.X= BORDER; pos.Y=0.0f; pos.Z= BORDER;
            groundVertices[3]  = new VertexPositionColorTexture(pos, color, uv);
        }

        /// <summary>
        /// Executes set-up routines when program begins. 
        /// </summary>
        protected override void Initialize()
        {
            InitializeBaseCode();
            InitializeGround();
            base.Initialize();
        }

        /// <summary>
        /// Draws colored surfaces with PositionColor.fx shader. 
        /// </summary>
        /// <param name="primitiveType">Object type drawn with vertex data.</param>
        /// <param name="vertexData">Array of vertices.</param>
        /// <param name="numPrimitives">Total primitives drawn.</param>
        private void PositionColorShader(PrimitiveType         primitiveType,
                                         VertexPositionColor[] vertexData,
                                         int                   numPrimitives){
            positionColorEffect.Begin(); // begin using PositionColor.fx
            positionColorEffect.Techniques[0].Passes[0].Begin();

            // set drawing format and vertex data then draw primitive surface
            graphics.GraphicsDevice.VertexDeclaration = positionColor;
            graphics.GraphicsDevice.DrawUserPrimitives<VertexPositionColor>(
                                    primitiveType, vertexData, 0, numPrimitives);

            positionColorEffect.Techniques[0].Passes[0].End();
            positionColorEffect.End();  // stop using PositionColor.fx
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
            textureEffect.Begin(); // begin using Texture.fx
            textureEffect.Techniques[0].Passes[0].Begin();

            // set drawing format and vertex data then draw surface
            graphics.GraphicsDevice.VertexDeclaration = positionColorTexture;
            graphics.GraphicsDevice.DrawUserPrimitives
                                    <VertexPositionColorTexture>(
                                    primitiveType, vertexData, 0, numPrimitives);

            textureEffect.Techniques[0].Passes[0].End();
            textureEffect.End(); // stop using Textured.fx
        }

        /// <summary>
        /// Triggers drawing of ground with texture shader.
        /// </summary>
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
            textureEffectImage.SetValue(grassTexture);
            
            // 5: draw object - primitive type, vertex data, # primitives
            TextureShader(PrimitiveType.TriangleStrip, groundVertices, 2);
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // create SpriteBatch object for drawing animated 2D images
            spriteBatch     = new SpriteBatch(GraphicsDevice);

            // load texture
            grassTexture    = Content.Load<Texture2D>("Images\\grass");
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
            const float   SCALE = 1.50f;

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
                    move = 1.0f;  // Up or W - move ahead
                else if (kb.IsKeyDown(Keys.Down) || kb.IsKeyDown(Keys.S))
                    move = -1.0f; // Down or S - move back
#endif
            }
            return move;         
        }

        /// <summary>
        /// Updates camera viewer in sideways direction.
        /// </summary>
        float Strafe()
        {
            KeyboardState kb = Keyboard.GetState();
            GamePadState  gp = GamePad.GetState(PlayerIndex.One);

            // using gamepad leftStick shifted left / right for strafe
            if (gp.IsConnected)
            {
                if (gp.ThumbSticks.Left.X != 0.0f)
                    return gp.ThumbSticks.Left.X;
            }
            // using keyboard - strafe with Left&Right or A&D
            else if (kb.IsKeyDown(Keys.Left) || kb.IsKeyDown(Keys.A))
                return -1.0f; // strafe left
            else if (kb.IsKeyDown(Keys.Right) || kb.IsKeyDown(Keys.D))
                return 1.0f;  // strafe right
            return 0.0f;
        }

        /// <summary>
        /// Changes camera viewing angle.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        Vector2 ChangeView(GameTime gameTime)
        {
            const float SENSITIVITY         = 250.0f;
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
                }
                // cursor not at center on Y
                if (mouse.Y != heightMiddle)
                {
                    change.Y  = mouse.Y - heightMiddle;
                    change.Y /= scaleY;
                }
                // reset cursor back to center
                Mouse.SetPosition(widthMiddle, heightMiddle);
            #endif
            }
            return change;
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Allows the game to exit
            KeyboardState kbState = Keyboard.GetState();
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
            ||  kbState.IsKeyDown(Keys.Escape))
            { 
                this.Exit(); 
            }

            // update camera
            cam.SetFrameInterval(gameTime);
            cam.Move(Move());
            cam.Strafe(Strafe());
            cam.SetView(ChangeView(gameTime));

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
           
            DrawGround();
            base.Draw(gameTime);
        }
    }
}
