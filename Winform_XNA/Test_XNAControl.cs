﻿using System;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using System.Diagnostics;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Content;
using JigLibX.Physics;
using JigLibX.Geometry;
using JigLibX.Collision;
using JigLibX.Math;

namespace Winform_XNA
{
    class Test_XNAControl : XNAControl
    {

        #region Camera
        Matrix _view;
        Matrix _projection;
        public Vector3 camPosition = new Vector3();
        public Quaternion camOrientation;
        float camSpeed = 10;
        float camSpeedChangeRate = 1.2f;
        #endregion

        #region Content
        public ContentManager Content { get; private set; }
        Model bullet;
        Model stickman;
        #endregion

        #region debug
        private SpriteBatch spriteBatch;
        private SpriteFont debugFont;        
        public bool Debug { get; set; }
        #endregion

        #region Physics
        BoostController bController = new BoostController();
        public PhysicsSystem PhysicsSystem { get; private set; }
        private System.Timers.Timer tmrPhysicsUpdate;
        #endregion

        #region game
        private Stopwatch tmrElapsed;
        private List<Gobject> gameObjects;
        
        double TIME_STEP = .01; // Recommended timestep
        #endregion

        #region Init
        protected override void Initialize()
        {
            Content = new ContentManager(Services, "content");

            // Potential timers for drawing
            // Any GraphicsDevice effects
            try
            {
                InitializePhysics();
                InitializeObjects();

                camPosition = new Vector3(0, 0, 800);
                camOrientation = Quaternion.Identity;

                tmrElapsed = Stopwatch.StartNew();
                spriteBatch = new SpriteBatch(GraphicsDevice);
                
                new Game();
                debugFont = Content.Load<SpriteFont>("DebugFont");
                
                _projection = Matrix.CreatePerspectiveFieldOfView(
                MathHelper.ToRadians(45.0f),
                (float)GraphicsDeviceManager.DefaultBackBufferWidth / (float)GraphicsDeviceManager.DefaultBackBufferHeight,
                0.1f,
                5000.0f);
                
                // From the example code, should this be a timer instead?
                Application.Idle += delegate { Invalidate(); };

                tmrPhysicsUpdate = new System.Timers.Timer();
                tmrPhysicsUpdate.AutoReset = false;
                tmrPhysicsUpdate.Enabled = false;
                tmrPhysicsUpdate.Interval = 10;
                tmrPhysicsUpdate.Elapsed += new System.Timers.ElapsedEventHandler(tmrPhysicsUpdate_Elapsed);
            }
            catch (Exception e)
            {
            }
        }
        private void InitializeObjects()
        {
            bullet = Content.Load<Model>("bullet");
            stickman = Content.Load<Model>("stickman");
            AddSphere(new Vector3(0, 200, 0), .8f, bullet, true);
            AddBox(new Vector3(0, 30, 0), new Vector3(.5f, .5f, .5f), bullet, false);
        }
        private void InitializePhysics()
        {
            gameObjects = new List<Gobject>();

            PhysicsSystem = new PhysicsSystem();
            PhysicsSystem.CollisionSystem = new CollisionSystemSAP();
            PhysicsSystem.SolverType = PhysicsSystem.Solver.Normal;
            PhysicsSystem.Gravity = new Vector3(0, -9.8f, 0);

            

        }
        #endregion

        #region Methods
        private void AddBox(Vector3 pos, Vector3 size, Model model, bool moveable)
        {
            Box boxPrimitive = new Box(pos, Matrix.Identity, size);
            Gobject box = new Gobject(
                Vector3.Zero,
                Vector3.One,
                boxPrimitive,
                model,
                moveable
                );

           
            gameObjects.Add(box);
        }
        private void AddSphere(Vector3 pos, float radius, Model model, bool moveable)
        {
            Sphere spherePrimitive = new Sphere(pos, radius);
            Gobject sphere = new Gobject(
                pos,
                Vector3.One*radius,
                spherePrimitive,
                model,
                moveable);
            gameObjects.Add(sphere);
            
            if(PhysicsSystem.Controllers.Contains(bController))
                PhysicsSystem.RemoveController(bController);
            bController.Initialize(sphere.Body);
            bController.DisableController();
            PhysicsSystem.AddController(bController);
        }
        #endregion

        #region User Input
        public void ProcessKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Q)
            {
                camSpeed *= camSpeedChangeRate;
            }
            if (e.KeyCode == Keys.Z)
            {
                camSpeed /= camSpeedChangeRate;
            }
            if (e.KeyCode == Keys.W)
            {
                camPosition += Vector3.Transform(Vector3.Forward, camOrientation) * camSpeed;
            }
            if (e.KeyCode == Keys.A)
            {
                camPosition += Vector3.Transform(Vector3.Left, camOrientation) * camSpeed;
            }
            if (e.KeyCode == Keys.S)
            {
                camPosition += Vector3.Transform(Vector3.Backward, camOrientation) * camSpeed;
            }
            if (e.KeyCode == Keys.D)
            {
                camPosition += Vector3.Transform(Vector3.Right, camOrientation) * camSpeed;
            }
            if (e.KeyCode == Keys.N)
                AddSphere();
            if (e.KeyCode == Keys.B)
            {
                bController.EnableController();
            }

        }

        private void AddSphere()
        {
            AddSphere(new Vector3(0, 600, 0), 5, bullet, true);
            
        }

        internal void PanCam(float dX, float dY)
        {
            Quaternion cameraChange =
            Quaternion.CreateFromAxisAngle(Vector3.UnitX, -dY * .001f) *
            Quaternion.CreateFromAxisAngle(Vector3.UnitY, -dX * .001f);
            camOrientation = camOrientation * cameraChange;
        }
        #endregion

        public void ResetTimer()
        {
            tmrPhysicsUpdate.Stop();
            tmrPhysicsUpdate.Start();
        }
        void tmrPhysicsUpdate_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            // every 10 milliseconds
            // Should use a variable timerate to keep up a steady "feel"?
            PhysicsSystem.CurrentPhysicsSystem.Integrate((float)TIME_STEP);


            tmrPhysicsUpdate.Stop();
            tmrPhysicsUpdate.Start();
        }

        #region Draw
        protected override void Draw()
        {
            Matrix proj = Matrix.Identity;
            GraphicsDevice.Clear(Color.Gray);

            double time = tmrElapsed.ElapsedMilliseconds;
            if (Debug)
            {
                try
                {
                    spriteBatch.Begin();
                    Vector2 position = new Vector2(5, 5);
                    spriteBatch.DrawString(debugFont, "Debug Text: Enabled", position, Color.LightGray);
                    position.Y += debugFont.LineSpacing;
                    spriteBatch.DrawString(debugFont, "FPS: " + (1000.0 / time), position, Color.LightGray);
                    position.Y += debugFont.LineSpacing;
                    spriteBatch.End();
                }
                catch (Exception e)
                {
                    System.Console.WriteLine(e.Message);
                }
            }

            tmrElapsed.Restart();

            /* Do Drawing Here!
             * Should probably call Game.Draw(GraphicsDevice);
             * Allow Game to handle all of the Drawing independantly
             *   thus this form just exist heres?
             * Need to think of a good structure for this
             *
             * Answer to above question!
             * This Control should only handle the world VIEW
             *    possibly passing a camera to Game when calling draw.
             * This allows for a 4x split panel for world editing, or 2-4x splitscreen
             */

            Vector3 cameraOriginalTarget = Vector3.Forward;
            Vector3 cameraOriginalUpVector = Vector3.Up;

            Vector3 camRotation = Vector3.Transform(Vector3.Forward, Matrix.CreateFromQuaternion(camOrientation));
            Vector3 cameraRotatedTarget = Vector3.Transform(cameraOriginalTarget, camOrientation);
            Vector3 cameraFinalTarget = camPosition + cameraRotatedTarget;
            Vector3 cameraRotatedUpVector = Vector3.Transform(cameraOriginalUpVector, camOrientation);

            Vector3.Clamp(cameraRotatedUpVector, new Vector3(-1, 0, -1), new Vector3(1, 1, 1)); ;
            _view = Matrix.CreateLookAt(
                camPosition,
                camPosition + camRotation,
                cameraRotatedUpVector);

            DrawObjects();
        }
        public void DrawObjects()
        {
            foreach (Gobject go in gameObjects)
            {
                go.Draw(_view, _projection);
            }
        }
        #endregion

        internal void ProcessKeyUp(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.B)
            {
                bController.DisableController();
            }
        }
    }
}
