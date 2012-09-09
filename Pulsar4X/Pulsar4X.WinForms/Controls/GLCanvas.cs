﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Graphics;
using Pulsar4X.WinForms;
using Pulsar4X.WinForms.Controls;
using log4net.Config;
using log4net;

namespace Pulsar4X.WinForms.Controls
{
    /// <summary>
    /// An Customised version of GLControle, Used as a base for the OpenGL Version Specifc dervied classes.
    /// </summary>
    public abstract class GLCanvas : OpenTK.GLControl
    {

        public static readonly ILog logger = LogManager.GetLogger(typeof(GLCanvas));

        /// <summary>
        /// Our Projections/ViewMatricies.
        /// </summary>
        protected Matrix4 m_m4ProjectionMatrix, m_m4ViewMatrix;

        /// <summary> The shader program used by default.</summary>
        protected GLUtilities.GLShader m_oShaderProgram;

        /// <summary>   Gets the default shader. </summary>
        /// <value> The default shader. </value>
        public GLUtilities.GLShader DefaultShader
        {
            get
            {
                return m_oShaderProgram;
            }
        }

        /// <summary>
        /// used to determine if this control hase bee sucessfully loaded.
        /// </summary>
        public bool m_bLoaded = false;

        /// <summary> 
        /// The zoom scaler, make this smaller to zoom out, larger to zoom in.
        /// </summary>
        protected float m_fZoomScaler = UIConstants.ZOOM_DEFAULT_SCALLER;

        /// <summary> The view offset, i.e. how much the view should be offset from 0, 0 </summary>
        protected Vector3 m_v3ViewOffset = new Vector3(0, 0, 0);

        /// <summary>
        /// Keeps tract of the start location when calculation Panning.
        /// </summary>
        Vector3 m_v3PanStartLocation;

        /// <summary> 
        /// List of objects to render 
        /// </summary>
        protected List<GLUtilities.GLPrimitive> m_loRenderList = new List<GLUtilities.GLPrimitive>();

        /// <summary> Gets the list of Objects for render </summary>
        /// <value> A List of GLPrimitives for render </value>
        public List<GLUtilities.GLPrimitive> RenderList
        {
            get
            {
                return m_loRenderList;
            }
        }

        public SceenGraph.Sceen SceenToRender { get; set; }

        /// <summary> Gets or sets the zoom factor. </summary>
        /// <value> The zoom factor. </value>
        public float ZoomFactor
        {
            get
            {
                return m_fZoomScaler;
            }
            set
            {
                m_fZoomScaler = value;
                // update view matrix:
                m_m4ViewMatrix = Matrix4.Scale(m_fZoomScaler) * Matrix4.Translation(m_v3ViewOffset);
                if (m_bLoaded && m_oShaderProgram != null)
                {
                    m_oShaderProgram.SetViewMatrix(ref m_m4ViewMatrix);
                }

            }
        }


        ///< @todo FPS counter,  For testing will need to be either deleted or cleaned up at some point
        protected float m_fps = 0;
        public float FPS
        {
            get
            {
                return m_fps;
            }
        }


        /// <summary>   Default constructor. </summary>
        public GLCanvas()
        {
            RegisterEventHandlers();
        }

        /// <summary> Constructor. </summary>
        /// <param name="a_oGraphicsMode">  The openGL graphics mode. </param>
        public GLCanvas(GraphicsMode a_oGraphicsMode)
            : base(a_oGraphicsMode)
        {
            RegisterEventHandlers();
        }


        /// <summary>   Constructor. </summary>
        /// <remarks>   Gregory.nott, 9/7/2012. </remarks>
        /// <param name="a_oGraphicsMode">  The openGL graphics mode. </param>
        /// <param name="a_iMajor">  The Major part of the openGL Version (1, 2, 3 or 4). </param>
        /// <param name="a_iMinor">  The minor part of the openGL version.</param>
        /// <param name="a_eFlags">  (optional) Any OpenGL Flags, e.g. Debug or Normal</param>
        public GLCanvas(GraphicsMode a_oGraphicsMode, int a_iMajor, int a_iMinor, GraphicsContextFlags a_eFlags = GraphicsContextFlags.Default)
            : base(a_oGraphicsMode, a_iMajor, a_iMinor, a_eFlags)
        {
            RegisterEventHandlers();
        }

        /// <summary> Registers the event handlers. </summary>
        private void RegisterEventHandlers()
        {
            // Below we setup even handlers for this class:
            Load += new System.EventHandler(this.OnLoad);                           // Setup Load Event Handler
            Paint += new System.Windows.Forms.PaintEventHandler(this.OnPaint);      // Setup Paint Event Handler
            SizeChanged += new System.EventHandler(this.OnSizeChange);              // Setup Size Changed Enet Handler.
            MouseMove += new MouseEventHandler(OnMouseMove);                        // Setup Mouse Move Event handler
            MouseDown += new MouseEventHandler(OnMouseDown);                        // Setup Mouse Down Event handler.
            //MouseUp += new MouseEventHandler(OnMouseUp);
            //Application.Idle += Application_Idle;
        }

        public abstract void OnLoad(object sender, EventArgs e);

        /// <summary> Executes the size change action. </summary>
        /// <remarks> Must be overloaded by the inherited classes. 
        /// 		  This is needed to update the view and projection matricies whenever the form size changes.
        /// 		  If these matricies are not updates then the view will be cut off and not draw for the whole screen. </remarks>
        /// <param name="sender">   Source of the event. </param>
        /// <param name="e">        Event information to send to registered event handlers. </param>
        public abstract void OnSizeChange(object sender, EventArgs e);

        /// <summary>   Paints this window, Calles the Render() functio to make sure our sceen is rendered. </summary>
        /// <param name="sender">   Source of the event. </param>
        /// <param name="e">        Event information to send to registered event handlers. </param>
        public void OnPaint(object sender, PaintEventArgs e)
        {
            if (!m_bLoaded)
            {
                return;
            }

            Render();
            this.Invalidate();
        }

        /// <summary>   Event handler. Called by Application for idle events. Keeps the Canvas rendering evan when nothing is happening! </summary>
        /// <param name="sender">   Source of the event. </param>
        /// <param name="e">        Event information to send to registered event handlers. </param>
        public void Application_Idle(object sender, EventArgs e)
        {
            if (m_bLoaded != true)
            {
                return;
            }

            this.Invalidate();
        }


        /// <summary>   Executes the mouse move action. i.e. Panning </summary>
        /// <param name="sender">   Source of the event. </param>
        /// <param name="e">        Event information to send to registered event handlers. </param>
        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Vector3 v3PanEndLocation;
                v3PanEndLocation.X = e.Location.X;
                v3PanEndLocation.Y = e.Location.Y;
                v3PanEndLocation.Z = 0.0f;

                Vector3 v3PanAmount = (v3PanEndLocation - m_v3PanStartLocation);

                v3PanAmount.Y = -v3PanAmount.Y; // we flip Y to make the panning go in the right direction.
                this.Pan(ref v3PanAmount);

                m_v3PanStartLocation.X = e.Location.X;
                m_v3PanStartLocation.Y = e.Location.Y;
                m_v3PanStartLocation.Z = 0.0f;
            }
        }


        /// <summary>   Executes the mouse down action. i.e. Start panning </summary>
        /// <param name="sender">   Source of the event. </param>
        /// <param name="e">        Event information to send to registered event handlers. </param>
        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            // An left mouse down, start pan.
            if (e.Button.Equals(System.Windows.Forms.MouseButtons.Left))
            {
                m_v3PanStartLocation.X = e.Location.X;
                m_v3PanStartLocation.Y = e.Location.Y;
                m_v3PanStartLocation.Z = 0.0f;
            }
            else if (e.Button.Equals(System.Windows.Forms.MouseButtons.Middle))
            {
                // on middle or mouse wheel button, centre!
                this.CenterOnZero();
            }
        }


        /*
        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            // On Left mouse release Pan.
            if (e.Button.Equals(System.Windows.Forms.MouseButtons.Left))
            {
                Vector3 v3PanEndLocation;
                v3PanEndLocation.X = e.Location.X;
                v3PanEndLocation.Y = e.Location.Y;
                v3PanEndLocation.Z = 0.0f;

                Vector3 v3PanAmount = v3PanEndLocation - m_v3PanStartLocation;
                v3PanAmount.Y = -v3PanAmount.Y; // we flip Y to make the panning go in the right direction.
                this.Pan(ref v3PanAmount);
            }
        }
        */

        
        /// <summary>
        /// Sets up the OpenGL viewport/camera.
        /// </summary>
        /// <param name="a_iViewportPosX">The X-axis Position of the Viewport relative to the world</param>
        /// <param name="a_iViewportPosY">The Y-axis Position of the Viewport Relative to the world</param>
        /// <param name="a_iViewportWidth">The Width of the Viewport</param>
        /// <param name="a_iViewPortHeight">The Height of the Viewport</param>
        public virtual void SetupViewPort(  int a_iViewportPosX,    int a_iViewportPosY, 
                                            int a_iViewportWidth,    int a_iViewPortHeight)
        {
            GL.Viewport(a_iViewportPosX, a_iViewportPosY, a_iViewportWidth, a_iViewPortHeight);
            //float aspectRatio = a_iViewportWidth / (float)(a_iViewPortHeight); // Calculate Aspect Ratio.

            // Setup our Projection Matrix, This defines how the 2D image seen on screen is created from our 3d world.
            // this will give us a 2d perspective looking Down onto the X,Y plane with 0,0 being the centre of the screen (by default)
            m_m4ProjectionMatrix = Matrix4.CreateOrthographic(a_iViewportWidth, a_iViewPortHeight, -10, 10);
            
            // This will setup a projection where 0,0 is in the bottom left of the screen and we are looking at the X,Y plane from above (i think, i might be below).
            //Matrix4 m_m4ProjectionMatrix2 = new Matrix4(new Vector4((2.0f / a_iViewportWidth), 0, 0, 0),
            //                                    new Vector4(0, (2.0f / a_iViewPortHeight), 0, 0),
            //                                    new Vector4(0, 0, 1, 0),
            //                                    new Vector4(-1, -1, 1, 1));

            // Setup our Model View Matrix i.e. the position and faceing of our camera. We are setting it up to look at (0,0,0) from (0,3,5) with positive y being up.
            m_m4ViewMatrix = Matrix4.Scale(m_fZoomScaler) * Matrix4.Translation(m_v3ViewOffset);
        }

        /// <summary>   Adds a GLPrimitive to the render list. </summary>
        /// <param name="a_oPrimitive"> The primitive to add. </param>
        public void AddToRenderList(GLUtilities.GLPrimitive a_oPrimitive)
        {
            m_loRenderList.Add(a_oPrimitive);
        }

        public abstract void IncreaseZoomScaler();

        public abstract void DecreaseZoomScaler();

        public abstract void Pan(ref Vector3 a_v3PanAmount);

        public abstract void CenterOnZero();

        public abstract void CenterOn(ref Vector3 a_v3Location);

        public abstract void Render();

        public abstract void TestFunc(int a_itest);
    }
}
