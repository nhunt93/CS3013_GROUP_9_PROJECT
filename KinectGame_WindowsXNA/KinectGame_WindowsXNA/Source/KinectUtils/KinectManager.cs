﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

using Microsoft.Kinect;
using System.IO;
using System.Diagnostics;

using KinectGame_WindowsXNA.Source;

/*CHANGELOG:
 * NEIL - Created class & basic functionality (from Microsoft examples).
 * NEIL - Commented out the rendering resources & functions (now operates as a purely in the background of the game).
 * NEIL - Added reference object to the base game class.
 * NEIL - Moved the colour stream manager to this class so that disconnect/reconnect resets the stream managers.
 */

namespace KinectGame_WindowsXNA.Source.KinectUtils
{
    // Utility class to manage a valid Kinect Sensor.
    public class KinectManager
    {
        /*/////////////////////////////////////////
          * MEMBER DATA
          *////////////////////////////////////////
        private readonly Dictionary<KinectStatus, string> status_map = null; // Kinect status feedback
        private readonly DepthImageFormat depth_image_format;
        private readonly ColorImageFormat colour_image_format;
        private KinectGame_WindowsXNA root_game = null; // root game class

        public KinectSensor kinect_sensor { get; private set; }
        public KinectStatus last_status { get; private set; }

        // Internal stream managers:
        ColourStreamManager colour_stream = null;

        // Debug status message label:
        private SpriteFont msg_font;
        private Vector2 msg_label_pos;



        /*/////////////////////////////////////////
          * CONSTRUCTOR(S)/DESTRUCTOR(S)
          *////////////////////////////////////////
        public KinectManager(ColorImageFormat p_colour_format,
                              DepthImageFormat p_depth_format,
                              KinectGame_WindowsXNA p_game)
        {
            // Initialise the Kinect selector...
            this.colour_image_format = p_colour_format;
            this.depth_image_format = p_depth_format;
            this.root_game = p_game;

            status_map = new Dictionary<KinectStatus, string>();
            KinectSensor.KinectSensors.StatusChanged += this.KinectSensorsStatusChanged;
            this.DiscoverSensor();

            this.status_map.Add(KinectStatus.Undefined, "UNKNOWN STATUS MESSAGE");
            this.status_map.Add(KinectStatus.Connected, string.Empty);
            this.status_map.Add(KinectStatus.DeviceNotGenuine, "Detected device is not genuine!");
            this.status_map.Add(KinectStatus.DeviceNotSupported, "Detected device is not supported!");
            this.status_map.Add(KinectStatus.Disconnected, "Disconnected/Device required!");
            this.status_map.Add(KinectStatus.Error, "Error in Kinect sensor!");
            this.status_map.Add(KinectStatus.Initializing, "Initialising Kinect sensor...");
            this.status_map.Add(KinectStatus.InsufficientBandwidth, "Insufficient bandwidth for Kinect sensor!");
            this.status_map.Add(KinectStatus.NotPowered, "Detected device is not powered!");
            this.status_map.Add(KinectStatus.NotReady, "Detected device is not ready!");

            // Load the status message font:
            msg_font = this.root_game.Content.Load<SpriteFont>("Fonts/Segoe16");
            msg_label_pos = new Vector2(4.0f, 2.0f);
        }

        ~KinectManager()
        {
            // Shutdown & clean-up...
            this.close();
        }



        /*/////////////////////////////////////////
          * STARTUP/LOAD FUNCTION(S)
          *////////////////////////////////////////
        public void loadStreamManagers()
        {
            // Create/load the Kinect stream managers...
            if (colour_stream != null) colour_stream.close(this); // close if already open

            colour_stream = new ColourStreamManager(new Rectangle(this.root_game.GraphicsDevice.Viewport.Width - 128, // small rectange in the far-right top corner
                                                                  0,
                                                                  128,
                                                                  96 ),
                                                    this.root_game.Content.Load<Effect>("Effects_Shaders/KinectColorVisualizer"),
                                                    this,
                                                    this.root_game.GraphicsDevice);
        }



        /*/////////////////////////////////////////
          * SHUTDOWN/UNLOAD FUNCTION(S)
          *////////////////////////////////////////
        public void close()
        {
            // Ensure the Kinect sensor is shut down...
            if(this.kinect_sensor != null &&
               this.kinect_sensor.IsRunning)
            {
                // Close all open stream managers:
                if(colour_stream != null) colour_stream.close(this);

                // Close all open streams and stop the Kinect sensor:
                if(this.kinect_sensor.ColorStream.IsEnabled) this.kinect_sensor.ColorStream.Disable();
                if(this.kinect_sensor.DepthStream.IsEnabled) this.kinect_sensor.DepthStream.Disable();
                if(this.kinect_sensor.SkeletonStream.IsEnabled) this.kinect_sensor.SkeletonStream.Disable();
                this.kinect_sensor.Stop();
            }
        }



        /*/////////////////////////////////////////
          * RENDERING FUNCTION(S)
          *////////////////////////////////////////
        public void drawStreamManagers(SpriteBatch p_sprite_batch)
        {
            // Render the video stream output from the Kinect...
            if(this.kinect_sensor != null &&
               this.kinect_sensor.Status == KinectStatus.Connected)
            {
                if(colour_stream != null)
                {
                    colour_stream.draw(p_sprite_batch, this);
                }
            }
        }


        public void drawStatusMessage(SpriteBatch p_sprite_batch)
        {
            // Render the current status message to the top-left corner of the window...
            p_sprite_batch.Begin();
            p_sprite_batch.DrawString(this.msg_font,
                                      this.last_status.ToString(),
                                      this.msg_label_pos,
                                      Color.White);
            p_sprite_batch.End();
        }



        /*/////////////////////////////////////////
          * INTERNAL SUPPORT FUNCTION(S)
          *////////////////////////////////////////
        private void KinectSensorsStatusChanged(object p_sender, StatusChangedEventArgs p_args)
        {
            // Handle Kinect status events (stop the sensor if device is disconnected)...
            if(p_args.Status != KinectStatus.Connected)
            {
                // Close all stream managers, then stop the current Kinect sensor:
                if (colour_stream != null) colour_stream.close(this);

                p_args.Sensor.Stop();
            }

            this.last_status = p_args.Status;
            this.DiscoverSensor();
        }



        private void DiscoverSensor()
        {
            // Attempt to connect to a valid Kinect sensor...
            this.kinect_sensor = KinectSensor.KinectSensors.FirstOrDefault();

            if(this.kinect_sensor != null)
            {
                this.last_status = this.kinect_sensor.Status;

                // Enable the connected sensor:
                if(this.last_status == KinectStatus.Connected)
                {
                    try
                    {
                        this.kinect_sensor.SkeletonStream.Enable();
                        this.kinect_sensor.ColorStream.Enable(this.colour_image_format);
                        this.kinect_sensor.DepthStream.Enable(this.depth_image_format);

                        try
                        {
                            // Start the sensor with the current stream settings:
                            this.kinect_sensor.Start();
                        }
                        catch(IOException ex)
                        {
                            // Sensor in use or otherwise unavailable:
                            Debug.WriteLine(ex.ToString());
                            this.kinect_sensor = null;
                        }

                        // If the Kinect sensor has been acquired, load the stream managers:
                        if(this.kinect_sensor.IsRunning)
                        {
                            this.loadStreamManagers();
                        }
                    }
                    catch(InvalidOperationException ex)
                    {
                        // Sensor may be unplugged or in some invalid state:
                        Debug.WriteLine(ex.ToString());
                        this.kinect_sensor = null;
                    }
                }
            }
            else
            {
                this.last_status = KinectStatus.Disconnected;
            }
        }
    }
}