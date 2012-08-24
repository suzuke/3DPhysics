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
        #region Todo
        //Refactor Physics Controllers

        #endregion

        Camera cam;
        LunarVehicle lv;
        enum ControlModes
        { 
            Camera,
            Object            
        }
        ControlModes controlMode = ControlModes.Object;

        #region Content
        public ContentManager Content { get; private set; }
        Model cubeModel;
        Model sphereModel;
        #endregion

        #region Debug
        private SpriteBatch spriteBatch;
        private SpriteFont debugFont;        
        public bool Debug { get; set; }
        public bool DebugPhysics { get; set; }
        #endregion

        #region Physics
        BoostController bController;
        public PhysicsSystem PhysicsSystem { get; private set; }
        private System.Timers.Timer tmrPhysicsUpdate;
        #endregion

        #region Game
        private Stopwatch tmrElapsed;
        private List<Gobject> gameObjects;
        
        double TIME_STEP = .01; // Recommended timestep
        #endregion

        #region Init
        protected override void Initialize()
        {
            Content = new ContentManager(Services, "content");

            try
            {
                InitializePhysics();
                InitializeObjects();

                cam = new Camera(new Vector3(.05f, 1.5f, 2.7f));

                tmrElapsed = Stopwatch.StartNew();
                spriteBatch = new SpriteBatch(GraphicsDevice);
                
                new Game();
                debugFont = Content.Load<SpriteFont>("DebugFont");
                
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
                System.Console.WriteLine(e.Message);
            }
        }
        private void InitializeObjects()
        {
            cubeModel = Content.Load<Model>("Cube");
            sphereModel = Content.Load<Model>("Sphere");
            //AddSphere(new Vector3(0, 0, .2f), 1f, sphereModel, false);
            //AddSphere(new Vector3(0, -3, 0), 2f, sphereModel, true);
            AddBox(new Vector3(0, 1, 0), new Vector3(.05f, .05f, .05f), cubeModel, true);
            AddBox(new Vector3(0, 0, 0), new Vector3(.5f, .5f, .5f), cubeModel, false);
        }
        private void InitializePhysics()
        {
            gameObjects = new List<Gobject>();

            PhysicsSystem = new PhysicsSystem();
            PhysicsSystem.CollisionSystem = new CollisionSystemSAP();
            PhysicsSystem.SolverType = PhysicsSystem.Solver.Normal;
            PhysicsSystem.Gravity = new Vector3(0, -2.8f, 0);
        }
        #endregion

        #region Methods
        private void AddBox(Vector3 pos, Vector3 size, Model model, bool moveable)
        {
            Box boxPrimitive = new Box(pos, Matrix.Identity, size);
            Gobject box = new Gobject(
                pos,
                size/2,
                boxPrimitive,
                model,
                moveable
                );

           
            gameObjects.Add(box);

            if (PhysicsSystem.Controllers.Contains(bController))
                PhysicsSystem.RemoveController(bController);
            bController = new BoostController(box.Body, Vector3.Up * 12, Vector3.Zero);
            PhysicsSystem.AddController(bController);
        }
        private void AddSphere(Vector3 pos, float radius, Model model, bool moveable)
        {
            Sphere spherePrimitive = new Sphere(pos, radius);
            Gobject sphere = new Gobject(
                pos,
                Vector3.One * radius,
                spherePrimitive,
                model,
                moveable);
            gameObjects.Add(sphere);
            
            if(PhysicsSystem.Controllers.Contains(bController))
                PhysicsSystem.RemoveController(bController);
            bController = new BoostController(sphere.Body, Vector3.Up*12, Vector3.Zero);
            PhysicsSystem.AddController(bController);
        }
        #endregion

        #region User Input
        private void ProcessCameraControl(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Q)
            {
                cam.IncreaseSpeed();
            }
            if (e.KeyCode == Keys.Z)
            {
                cam.DecreaseSpeed();
            }
            if (e.KeyCode == Keys.W)
            {
                cam.MoveForward();
            }
            if (e.KeyCode == Keys.A)
            {
                cam.MoveLeft();
            }
            if (e.KeyCode == Keys.S)
            {
                cam.MoveBackward();
            }
            if (e.KeyCode == Keys.D)
            {
                cam.MoveRight();
            }

            if (e.KeyCode == Keys.N)
            {
                AddSphere();
            }

            if (e.KeyCode == Keys.B)
            {
                //bController.EnableController();
            }
        }
        private void ProcessObjectControlKeyDown(KeyEventArgs e)
        {

            if (e.KeyCode == Keys.L)
            {
                Vector3 size = new Vector3(.5f, .5f, .5f);
                Vector3 pos = new Vector3(0, 1, 0);
                Box boxPrimitive = new Box(pos, Matrix.Identity, size);
                Gobject box = new Gobject(
                    pos,
                    size / 2,
                    boxPrimitive,
                    cubeModel,
                    true
                    );
                lv = new LunarVehicle(box.Body);

                gameObjects.Add(box);
            }
            if (lv == null)
                return;
            if (e.KeyCode == Keys.T)
            {
                lv.SetVertJetThrust(1.0f);
            }
            if (e.KeyCode == Keys.W)
            {
                lv.SetFireRotJetZThrust(-.9f);
            }
            if (e.KeyCode == Keys.A)
            {
                lv.SetRotJetXThrust(.9f);
            }
            if (e.KeyCode == Keys.S)
            {
                lv.SetFireRotJetZThrust(.9f);
            }
            if (e.KeyCode == Keys.D)
            {
                lv.SetRotJetXThrust(-.9f);
                
            }
        }
        private void ProcessObjectControlKeyUp(KeyEventArgs e)
        {
            if (lv == null)
                return;
            if (e.KeyCode == Keys.T)
            {
                lv.SetVertJetThrust(0);
            }
            if (e.KeyCode == Keys.W)
            {
                lv.SetFireRotJetZThrust(0);
            }
            if (e.KeyCode == Keys.A)
            {
                lv.SetRotJetXThrust(0);
            }
            if (e.KeyCode == Keys.S)
            {
                lv.SetFireRotJetZThrust(0);
                
            }
            if (e.KeyCode == Keys.D)
            {
                lv.SetRotJetXThrust(0);
            }
        }

        public void ProcessKeyDown(KeyEventArgs e)
        {

            switch (controlMode)
            {
                case ControlModes.Camera:
                    ProcessCameraControl(e);
                    break;
                case ControlModes.Object:
                    ProcessObjectControlKeyDown(e);
                    break;
                default:
                    break;
            }

            
            if (e.KeyCode == Keys.M)
            {
                if (controlMode == ControlModes.Object)
                    controlMode = ControlModes.Camera;
                else
                    controlMode = ControlModes.Object;
            }

        }

        
        private void AddSphere()
        {
            AddSphere(new Vector3(0, 300, 0), .5f, sphereModel, true);
        }
        internal void PanCam(float dX, float dY)
        {
            cam.AdjustOrientation(-dY*.001f,-dX*.001f);
        }
        #endregion

        public void ResetTimer()
        {
            tmrPhysicsUpdate.Stop();
            tmrPhysicsUpdate.Start();
        }
        void tmrPhysicsUpdate_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            // Should use a variable timerate to keep up a steady "feel" if we bog down?
            PhysicsSystem.CurrentPhysicsSystem.Integrate((float)TIME_STEP);


            tmrPhysicsUpdate.Stop();
            tmrPhysicsUpdate.Start();
        }
        

        #region Draw
        protected override void Draw()
        {
            Matrix proj = Matrix.Identity;
            GraphicsDevice.Clear(Color.Gray);

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

            DrawObjects();

            if (Debug)
            {
                try
                {
                    double time = tmrElapsed.ElapsedMilliseconds;
                    spriteBatch.Begin();
                    Vector2 position = new Vector2(5, 5);
                    spriteBatch.DrawString(debugFont, "Debug Text: Enabled", position, Color.LightGray);
                    position.Y += debugFont.LineSpacing;
                    spriteBatch.DrawString(debugFont, "FPS: " + (1000.0 / time), position, Color.LightGray);
                    position.Y += debugFont.LineSpacing;
                    position = DebugShowVector(spriteBatch, debugFont, position, "CameraPosition", cam.Position);
                    position = DebugShowVector(spriteBatch, debugFont, position, "CameraOrientation", Matrix.CreateFromQuaternion(cam.Orientation).Forward);
                    spriteBatch.End();
                    tmrElapsed.Restart();
                }
                catch (Exception e)
                {
                    System.Console.WriteLine(e.Message);
                }
            }
        }

        private Vector2 DebugShowVector(SpriteBatch sb, SpriteFont font, Vector2 p, string s, Vector3 vector)
        {
            sb.DrawString(font, s + ".X = " + vector.X, p, Color.LightGray);
            p.Y += font.LineSpacing;

            sb.DrawString(font, s + ".Y = " + vector.Y, p, Color.LightGray);
            p.Y += font.LineSpacing;

            sb.DrawString(font, s + ".Z = " + vector.Z, p, Color.LightGray);
            p.Y += font.LineSpacing;

            return p;
        }

        public void DrawObjects()
        {
            foreach (Gobject go in gameObjects)
            {
                go.Draw(cam.RhsLevelViewMatrix, cam._projection);
                if (DebugPhysics)
                    go.DebugDraw(GraphicsDevice, cam.RhsLevelViewMatrix, cam._projection);
            }
        }
        #endregion

        internal void ProcessKeyUp(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.B)
            {
                //bController.DisableController();
            }

            switch (controlMode)
            {
                case ControlModes.Camera:
                    break;
                case ControlModes.Object:
                    ProcessObjectControlKeyUp(e);
                    break;
                default:
                    break;
            }
        }
    }
}
