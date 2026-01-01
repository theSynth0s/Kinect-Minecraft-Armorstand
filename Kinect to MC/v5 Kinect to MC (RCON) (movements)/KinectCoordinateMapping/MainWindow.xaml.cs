using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

//Added libraries:
using System.Globalization;
using TSendKeys = System.Windows.Forms.SendKeys;
using System.Net;

namespace KinectCoordinateMapping
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    
    public partial class MainWindow : Window
    {
        CameraMode _mode = CameraMode.Color;

        KinectSensor _sensor;
        Skeleton[] _bodies = new Skeleton[6];
        int x = 0;
        //double lastAngleZ;
        //double lastAngleX;

        string command; //commande à envoyer au serveur
        bool detected = false;
        int direction = 0; // Direction de déplacment de l'armor stand
        const double PI = 3.14159265359;

        string ipaddress, password;
        int port;

        double ArmRightY_Angle;
        double ArmLeftY_Angle;
        double ArmRightZ_Angle;
        double ArmLeftZ_Angle;

        double LegRightY_Angle;
        double LegLeftY_Angle;
        double LegRightZ_Angle;
        double LegLeftZ_Angle;
            

        public MainWindow()
        {
            InitializeComponent();

            Thread thread = new Thread(RCON); //Nouveau thread
            thread.Start(command);

            AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnProcessExit);
        }

        void RCON(object obj)
        {
            Console.WriteLine("Enter IP Address (IPv4) :");
            ipaddress = Console.ReadLine();
            Console.WriteLine("Enter RCON port :");
            port = int.Parse(Console.ReadLine());
            Console.WriteLine("Enter password :");
            password = Console.ReadLine();

            command = "?"; // DOIT ETRE != null

            SourceRcon Sr = new SourceRcon();
            //Sr.Errors += new StringOutput(ErrorOutput);
            Sr.ServerOutput += new StringOutput(ConsoleOutput);

            if (Sr.Connect(new IPEndPoint(IPAddress.Parse(ipaddress), port), password))
            {
                while (!Sr.Connected)
                {
                    Console.WriteLine("Connecting...");
                    Thread.Sleep(10);
                }

                Console.WriteLine("Connected :");
                Console.WriteLine("READY !");

                while (true) // loop d'envoi des commandes au serveur
                {
                    if (detected == true)
                    {
                        //Console.WriteLine(command);
                        if ((ArmRightY_Angle > 50) && (ArmRightY_Angle < 90) && (ArmRightZ_Angle < -50) && (ArmRightZ_Angle > -90) // Les 2 bras à gauche
                                      && (ArmLeftY_Angle > 50) && (ArmLeftY_Angle < 90) && (ArmLeftZ_Angle > 50) && (ArmLeftZ_Angle < 90))
                        {
                            Sr.ServerCommand("/execute at @e[type=minecraft:armor_stand] run tp @e[type=armor_stand] ~ ~ ~ ~-15 ~"); // Tourne à gauche
                            direction -= 15;
                        }
                        else if ((ArmRightY_Angle > 50) && (ArmRightY_Angle < 90) && (ArmRightZ_Angle > 50) && (ArmRightZ_Angle < 90) // Les 2 bras à droite
                                && (ArmLeftY_Angle > 50) && (ArmLeftY_Angle < 90) && (ArmLeftZ_Angle < -50) && (ArmLeftZ_Angle > -90))
                        {
                            Sr.ServerCommand("/execute at @e[type=minecraft:armor_stand] run tp @e[type=armor_stand] ~ ~ ~ ~15 ~"); // Tourne à droite
                            direction += 15;
                        }
                        else if ((ArmRightY_Angle > 50) && (ArmRightY_Angle < 90) && (ArmRightZ_Angle > -50) && (ArmRightZ_Angle < 50) // Les 2 bras en avant
                                && (ArmLeftY_Angle > 50) && (ArmLeftY_Angle < 90) && (ArmLeftZ_Angle > -50) && (ArmLeftZ_Angle < 50))
                        {
                            double X = -Math.Sin(direction * Math.PI / 180.0);
                            double Z = Math.Cos(direction * Math.PI / 180.0);
                            X = Math.Round((X / 2), 5);
                            Z = Math.Round((Z / 2), 5);

                            Sr.ServerCommand("/execute at @e[type=minecraft:armor_stand] run tp @e[type=armor_stand] ~" + X.ToString(new CultureInfo("en-US")) + " ~ ~" + Z.ToString(new CultureInfo("en-US"))); // Avance dans la direction où il regarde
                        }
                        
                        Sr.ServerCommand("execute as @e[type=minecraft:armor_stand] run data merge entity @s {Pose:{RightArm:[" + (-ArmRightY_Angle).ToString(new CultureInfo("en-US")) + "f," + (ArmRightZ_Angle).ToString(new CultureInfo("en-US")) + "f,0f],LeftArm:[" + (-ArmLeftY_Angle).ToString(new CultureInfo("en-US")) + "f," + (-ArmLeftZ_Angle).ToString(new CultureInfo("en-US")) + "f,0f],RightLeg:[" + (-LegRightY_Angle).ToString(new CultureInfo("en-US")) + "f," + (LegRightZ_Angle).ToString(new CultureInfo("en-US")) + "f,0f],LeftLeg:[" + (-LegLeftY_Angle).ToString(new CultureInfo("en-US")) + "f," + (-LegLeftZ_Angle).ToString(new CultureInfo("en-US")) + "f,0f]}}");

                        Thread.Sleep(150); // Le temps que la kinect se raffraichisse
                    }
                }
            }
            else
            {
                Console.WriteLine("No connection!");
                Thread.Sleep(1000);
            }

        }

        static void ErrorOutput(string input)
        {
            Console.WriteLine("Error: {0}", input);
        }

        static void ConsoleOutput(string input)
        {
            Console.WriteLine("Console: {0}", input);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _sensor = KinectSensor.KinectSensors.Where(s => s.Status == KinectStatus.Connected).FirstOrDefault();

            if (_sensor != null)
            {
                _sensor.ColorStream.Enable();
                _sensor.DepthStream.Enable();
                _sensor.SkeletonStream.Enable();

                _sensor.AllFramesReady += Sensor_AllFramesReady;

                _sensor.Start();
            }
        }

        void Sensor_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            // Color
            using (var frame = e.OpenColorImageFrame())
            {
                if (frame != null)
                {
                    if (_mode == CameraMode.Color)
                    {
                        camera.Source = frame.ToBitmap();
                    }
                }
            }

            // Depth
            using (var frame = e.OpenDepthImageFrame())
            {
                if (frame != null)
                {
                    if (_mode == CameraMode.Depth)
                    {
                        camera.Source = frame.ToBitmap();
                    }
                }
            }

            // Body
            using (var frame = e.OpenSkeletonFrame())
            {
                if (frame != null)
                {
                    canvas.Children.Clear();

                    frame.CopySkeletonDataTo(_bodies);

                    foreach (var body in _bodies)
                    {
                        if (body.TrackingState == SkeletonTrackingState.Tracked)
                        {
                            detected = true; // un humain est détecté

                            // COORDINATE MAPPING
                            foreach (Joint joint in body.Joints)
                            {
                                // 3D coordinates in meters
                                SkeletonPoint skeletonPoint = joint.Position;

                                // 2D coordinates in pixels
                                Point point = new Point();


                                x += 1;

                                if (x >= 100)
                                {
                                    var ArmRightX = body.Joints[JointType.HandRight].Position.X;    var ArmRightY = body.Joints[JointType.HandRight].Position.Y;    var ArmRightZ = body.Joints[JointType.HandRight].Position.Z;

                                    var ArmLeftX = body.Joints[JointType.HandLeft].Position.X; var ArmLeftY = body.Joints[JointType.HandLeft].Position.Y; var ArmLeftZ = body.Joints[JointType.HandLeft].Position.Z;

                                    var ShoulderRightX = body.Joints[JointType.ShoulderRight].Position.X;   var ShoulderRightY = body.Joints[JointType.ShoulderRight].Position.Y;   var ShoulderRightZ = body.Joints[JointType.ShoulderRight].Position.Z;

                                    var ShoulderLeftX = body.Joints[JointType.ShoulderLeft].Position.X; var ShoulderLeftY = ShoulderRightY; var ShoulderLeftZ = ShoulderRightZ;

                                    var HipRightX = ShoulderRightX; var HipRightY = body.Joints[JointType.HipRight].Position.Y; var HipRightZ = ShoulderRightZ;

                                    var HipLeftX = ShoulderLeftX; var HipLeftY = body.Joints[JointType.HipLeft].Position.Y; var HipLeftZ = ShoulderLeftZ;


                             /*me*/ var LegRightX = body.Joints[JointType.KneeRight].Position.X; var LegRightY = body.Joints[JointType.KneeRight].Position.Y; var LegRightZ = body.Joints[JointType.KneeRight].Position.Z;

                             /*me*/ var LegLeftX = body.Joints[JointType.KneeLeft].Position.X; var LegLeftY = body.Joints[JointType.KneeLeft].Position.Y; var LegLeftZ = body.Joints[JointType.KneeLeft].Position.Z;

                                    /*
                                    var ABX = Math.Sqrt(Math.Pow(ShoulderRightZ - ArmRightZ, 2) + Math.Pow(ShoulderRightY - ArmRightY, 2));
                                    var BCX = Math.Sqrt(Math.Pow(ShoulderRightZ - HipRightZ, 2) + Math.Pow(ShoulderRightY - HipRightY, 2));
                                    var ACX = Math.Sqrt(Math.Pow(HipRightZ - ArmRightZ, 2) + Math.Pow(HipRightY - ArmRightY, 2));
                                    var AngleX = Math.Acos((BCX * BCX + ABX * ABX - ACX * ACX) / (2 * BCX * ABX)) * (180 / Math.PI);
                                    */
                                    
                                    var ABY = Math.Sqrt(Math.Pow(ShoulderRightX - ArmRightX, 2) + Math.Pow(ShoulderRightY - ArmRightY, 2));
                                    var BCY = Math.Sqrt(Math.Pow(ShoulderRightX - HipRightX, 2) + Math.Pow(ShoulderRightY - HipRightY, 2));
                                    var ACY = Math.Sqrt(Math.Pow(HipRightX - ArmRightX, 2) + Math.Pow(HipRightY - ArmRightY, 2));
                                    ArmRightY_Angle = Math.Acos((BCY * BCY + ABY * ABY - ACY * ACY) / (2 * BCY * ABY)) * (180 / Math.PI);

                                    ABY = Math.Sqrt(Math.Pow(ShoulderLeftX - ArmLeftX, 2) + Math.Pow(ShoulderLeftY - ArmLeftY, 2));
                                    BCY = Math.Sqrt(Math.Pow(ShoulderLeftX - HipLeftX, 2) + Math.Pow(ShoulderLeftY - HipLeftY, 2));
                                    ACY = Math.Sqrt(Math.Pow(HipLeftX - ArmLeftX, 2) + Math.Pow(HipLeftY - ArmLeftY, 2));
                                    ArmLeftY_Angle = Math.Acos((BCY * BCY + ABY * ABY - ACY * ACY) / (2 * BCY * ABY)) * (180 / Math.PI);

                                    var ABZ = Math.Sqrt(Math.Pow(ShoulderLeftX - ShoulderRightX, 2) + Math.Pow(ShoulderLeftZ - ShoulderRightZ, 2));
                                    var BCZ = Math.Sqrt(Math.Pow(ShoulderRightX - ArmRightX, 2) + Math.Pow(ShoulderRightZ - ArmRightZ, 2));
                                    var ACZ = Math.Sqrt(Math.Pow(ShoulderLeftX - ArmRightX, 2) + Math.Pow(ShoulderLeftZ - ArmRightZ, 2));
                                    ArmRightZ_Angle = Math.Acos((BCZ * BCZ + ABZ * ABZ - ACZ * ACZ) / (2 * BCZ * ABZ))* (180 / Math.PI);

                                    ABZ = Math.Sqrt(Math.Pow(ShoulderRightX - ShoulderLeftX, 2) + Math.Pow(ShoulderRightZ - ShoulderLeftZ, 2));
                                    BCZ = Math.Sqrt(Math.Pow(ShoulderLeftX - ArmLeftX, 2) + Math.Pow(ShoulderLeftZ - ArmLeftZ, 2));
                                    ACZ = Math.Sqrt(Math.Pow(ShoulderRightX - ArmLeftX, 2) + Math.Pow(ShoulderRightZ - ArmLeftZ, 2));
                                    ArmLeftZ_Angle = Math.Acos((BCZ * BCZ + ABZ * ABZ - ACZ * ACZ) / (2 * BCZ * ABZ)) * (180 / Math.PI);


                             /*me*/ ABY = Math.Sqrt(Math.Pow(HipRightX - LegRightX, 2) + Math.Pow(HipRightY - LegRightY, 2));
                                    BCY = Math.Sqrt(Math.Pow(HipRightX - HipRightX, 2) + Math.Pow(HipRightY - (-1.9), 2));
                                    ACY = Math.Sqrt(Math.Pow(HipRightX - LegRightX, 2) + Math.Pow((-1.9) - LegRightY, 2));
                                    LegRightY_Angle = Math.Acos((BCY * BCY + ABY * ABY - ACY * ACY) / (2 * BCY * ABY)) * (180 / Math.PI);

                             /*me*/ ABY = Math.Sqrt(Math.Pow(HipLeftX - LegLeftX, 2) + Math.Pow(HipLeftY - LegLeftY, 2));
                                    BCY = Math.Sqrt(Math.Pow(HipLeftX - HipLeftX, 2) + Math.Pow(HipLeftY - (-1.9), 2));
                                    ACY = Math.Sqrt(Math.Pow(HipLeftX - LegLeftX, 2) + Math.Pow((-1.9) - LegLeftY, 2));
                                    LegLeftY_Angle = Math.Acos((BCY * BCY + ABY * ABY - ACY * ACY) / (2 * BCY * ABY)) * (180 / Math.PI);

                             /*me*/ ABZ = Math.Sqrt(Math.Pow(HipLeftX - HipRightX, 2) + Math.Pow(HipLeftZ - HipRightZ, 2));
                                    BCZ = Math.Sqrt(Math.Pow(HipRightX - LegRightX, 2) + Math.Pow(HipRightZ - LegRightZ, 2));
                                    ACZ = Math.Sqrt(Math.Pow(HipLeftX - LegRightX, 2) + Math.Pow(HipLeftZ - LegRightZ, 2));
                                    LegRightZ_Angle = Math.Acos((BCZ * BCZ + ABZ * ABZ - ACZ * ACZ) / (2 * BCZ * ABZ)) * (180 / Math.PI);

                             /*me*/ ABZ = Math.Sqrt(Math.Pow(HipRightX - HipLeftX, 2) + Math.Pow(HipRightZ - HipLeftZ, 2));
                                    BCZ = Math.Sqrt(Math.Pow(HipLeftX - LegLeftX, 2) + Math.Pow(HipLeftZ - LegLeftZ, 2));
                                    ACZ = Math.Sqrt(Math.Pow(HipRightX - LegLeftX, 2) + Math.Pow(HipRightZ - LegLeftZ, 2));
                                    LegLeftZ_Angle = Math.Acos((BCZ * BCZ + ABZ * ABZ - ACZ * ACZ) / (2 * BCZ * ABZ)) * (180 / Math.PI);

                                    //Console.Write(LegLeftX);
                                    //Console.Write(LegLeftY);

                                    ArmRightZ_Angle -= 90;
                                    ArmRightZ_Angle = Math.Round(ArmRightZ_Angle, 4);
                                    ArmRightY_Angle = Math.Round(ArmRightY_Angle, 4);

                                    ArmLeftZ_Angle -= 90;
                                    ArmLeftZ_Angle = Math.Round(ArmLeftZ_Angle, 4);
                                    ArmLeftY_Angle = Math.Round(ArmLeftY_Angle, 4);


                             /*me*/ LegRightY_Angle -= LegLeftY_Angle * 0.35; //Legs error corrector
                                    LegLeftY_Angle -= LegRightY_Angle * 0.35; 
                                    
                                    LegRightZ_Angle -= 90;
                                    LegRightZ_Angle = Math.Round(LegRightZ_Angle, 4);
                                    LegRightY_Angle = Math.Round(LegRightY_Angle, 4);

                                    LegLeftZ_Angle -= 90;
                                    LegLeftZ_Angle = Math.Round(LegLeftZ_Angle, 4);
                                    LegLeftY_Angle = Math.Round(LegLeftY_Angle, 4);


                                    //command = "say RightArm: Y = " + (ArmRightY_Angle).ToString(new CultureInfo("en-US")) + ", Z = " + (ArmRightZ_Angle).ToString(new CultureInfo("en-US")) + " LeftArm: Y = " + (ArmLeftY_Angle).ToString(new CultureInfo("en-US")) + ", Z = " + (ArmLeftZ_Angle).ToString(new CultureInfo("en-US"));
                                   
                                    x = 0;
                                }
                                if (_mode == CameraMode.Color)
                                {
                                    // Skeleton-to-Color mapping
                                    ColorImagePoint colorPoint = _sensor.CoordinateMapper.MapSkeletonPointToColorPoint(skeletonPoint, ColorImageFormat.RgbResolution640x480Fps30);

                                    point.X = colorPoint.X;
                                    point.Y = colorPoint.Y;
                                }
                                else if (_mode == CameraMode.Depth) // Remember to change the Image and Canvas size to 320x240.
                                {
                                    // Skeleton-to-Depth mapping
                                    DepthImagePoint depthPoint = _sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(skeletonPoint, DepthImageFormat.Resolution320x240Fps30);
                                    point.X = depthPoint.X;
                                    point.Y = depthPoint.Y;
                                }

                                // DRAWING...
                                Ellipse ellipse = new Ellipse
                                {
                                    Fill = Brushes.LightBlue,
                                    Width = 20,
                                    Height = 20
                                };

                                Canvas.SetLeft(ellipse, point.X - ellipse.Width / 2);
                                Canvas.SetTop(ellipse, point.Y - ellipse.Height / 2);

                                canvas.Children.Add(ellipse);
                            }
                        }
                        else
                        {
                            detected = false; // personne n'est détecté
                        }
                    }
                }
            }
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_sensor != null)
            {
                _sensor.Stop();
            }
        }

        static void OnProcessExit(object sender, EventArgs e)
        {
            // When the application is exiting (only by closing the graphic window !)
            TSendKeys.SendWait("{ENTER}");
            System.Windows.Forms.Application.ExitThread();
        }
        
    }

    enum CameraMode
    {
        Color,
        Depth
    }
}
