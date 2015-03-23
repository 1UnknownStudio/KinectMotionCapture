//#define EXPORT_TEST

using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Kinect;

namespace KinectMocap {

    class SkeletonSensor {
        public KinectSensor kinect;
        public Skeleton[] skeletonData;
    };

    class SkeletalTracker {
        private SkeletonSensor sensor;
        ConsoleKeyInfo keyInfo;
        private int trackedSkeletonIndex = -1;
        private List<Skeleton> trackedSkeletonFrames = new List<Skeleton>();
       
        private bool isRecording = false;
        private bool newFrameReady = false;
        private string output;
        private const int PRECISION = 2;

        public void Init() {
            // Start Kinect skeleton tracking
            StartKinectST();
        }

        private void StartKinectST() {
            sensor = new SkeletonSensor();

            while( sensor.kinect == null ) {
                sensor.kinect = KinectSensor.KinectSensors.FirstOrDefault(s => s.Status == KinectStatus.Connected);

                if (sensor.kinect == null) {
                    Console.Clear();
                    Console.WriteLine("No Kinect sensor found.");
                }
            }
            Console.Clear();

#if EXPORT_TEST
#else

            sensor.kinect.SkeletonStream.Enable(); // Enable skeletal tracking

            sensor.skeletonData = new Skeleton[sensor.kinect.SkeletonStream.FrameSkeletonArrayLength]; // Allocate ST data

            sensor.kinect.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(kinect_SkeletonFrameReady); // Get ready for Skeleton Ready Events

            sensor.kinect.Start();

#endif
        }

        private void kinect_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e) {
            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame()) { // Open the Skeleton frame
                if (skeletonFrame != null && sensor.skeletonData != null && isRecording == true) {
                    skeletonFrame.CopySkeletonDataTo(sensor.skeletonData);
                    newFrameReady = true;
                }
            }
        }

        public void Update() {
            HandleInput();

            if (isRecording && newFrameReady && sensor.skeletonData != null) {
                // Check if we are currently tracking a skeleton
                if(trackedSkeletonIndex == -1) {
                    // If we aren't, try to get the first skeleton that the kinect sees or exit the update loop
                    for(int i = 0, j = sensor.skeletonData.Length; i < j; i++) {
                        if( sensor.skeletonData[i].TrackingState == SkeletonTrackingState.Tracked ) {
                            trackedSkeletonIndex = i;
                            break;
                        }
                    }

                    if (trackedSkeletonIndex == -1) {
                        newFrameReady = false;
                        return;
                    }
                }

                // If we are still tracking the same skeleton, push the new frame to the frame list. If we can't find the skeleton in the frame, stop tracking it.
                if (sensor.skeletonData[trackedSkeletonIndex].TrackingState == SkeletonTrackingState.Tracked) {
                    Skeleton skeletonCopy = Clone(sensor.skeletonData[trackedSkeletonIndex]);
                    trackedSkeletonFrames.Add(skeletonCopy);
                } else {
                    trackedSkeletonIndex = -1;
                }

                newFrameReady = false;
            }
        }

        private Skeleton Clone(Skeleton skOrigin)
        {
            // It serializes the skeleton to the memory and retrieves again, making a copy of the object
            MemoryStream ms = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();

            bf.Serialize(ms, skOrigin);

            ms.Position = 0;
            object obj = bf.Deserialize(ms);
            ms.Close();

            return obj as Skeleton;
        }

        private void HandleInput() {
            // Check for keyboard input
            if (Console.KeyAvailable)
                keyInfo = Console.ReadKey(true);
            else
                return;

            switch (keyInfo.Key) 
            { 
                case ConsoleKey.R:
                    isRecording = !isRecording;
                    break;
                case ConsoleKey.G:
                    CreateBVH();
                    break;
            }
        }

        public void Debug() {
            Console.WriteLine("Sensor: " + sensor.kinect.Status.ToString());

            if (!isRecording)
                Console.WriteLine("Press \'R\' to start recording");
            else {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("■" + " Recording");
                Console.ResetColor();

                Console.WriteLine("Press \'R\' to stop recording");
            }

            if (sensor != null) {

                if (sensor.kinect.Status == KinectStatus.Connected) {
                    if (trackedSkeletonIndex != -1) {
                        Console.ForegroundColor = ConsoleColor.DarkGreen;
                        Console.WriteLine("Tracking skeleton: " + trackedSkeletonIndex);
                        Console.ResetColor();
                    } else { 
                        for (int k = 0, m = 6; k < m; k++) {
                            if (sensor.skeletonData[k] != null) {
                                if (sensor.skeletonData[k].TrackingState == SkeletonTrackingState.Tracked)
                                    Console.ForegroundColor = ConsoleColor.DarkGreen;

                                Console.WriteLine("Skeleton " + k.ToString() + "- " + sensor.skeletonData[k].TrackingState.ToString());

                                Console.ResetColor();
                            }
                        }
                    }
                }
            }

            Console.Write("\n");
        }

        private void TrackClosestSkeleton()
        {
            if (sensor.kinect != null && sensor.kinect.SkeletonStream != null)
            {
                if (!sensor.kinect.SkeletonStream.AppChoosesSkeletons)
                {
                    sensor.kinect.SkeletonStream.AppChoosesSkeletons = true; // Ensure AppChoosesSkeletons is set
                }

                float closestDistance = 10000f; // Start with a far enough distance
                int closestID = 0;

                foreach (Skeleton skeleton in sensor.skeletonData.Where(s => s.TrackingState != SkeletonTrackingState.NotTracked))
                {
                    if (skeleton.Position.Z < closestDistance)
                    {
                        closestID = skeleton.TrackingId;
                        closestDistance = skeleton.Position.Z;
                    }
                }

                if (closestID > 0)
                {
                    sensor.kinect.SkeletonStream.ChooseSkeletons(closestID); // Track this skeleton
                }
            }
        }

        //private void FindPlayerInDepthPixel(short[] depthFrame) {
        //    foreach (short depthPixel in depthFrame) {
        //        int player = depthPixel & DepthImageFrame.PlayerIndexBitmask;

        //        if (player > 0 && this.skeletonData != null) {
        //            Skeleton skeletonAtPixel = this.skeletonData[player - 1];   // Found the player at this pixel
        //            // ...
        //        }
        //    }
        //}

        public void CreateBVH() {
            // Create first section: Hierarchy
            CreateHierarchy();

            //Create second section: Motion
            CreateMotion();

            System.IO.File.WriteAllText("test.bvh", output);
        }

        private void CreateHierarchy() {
            output = "HEIRARCHY \r\n";
            output += "ROOT " + ((JointType)0).ToString() + "\r\n";
            output += "{\r\n";
            output += "\tOFFSET\t0.00\t0.00\t0.00\r\n";
            output += "\tCHANNELS 6 Xposition Yposition Zposition Zrotation Xrotation Yrotation\r\n";

            // Hipcenter, our ROOT, is connected to:
            CreateJoint(1, 1);  // 1, Spine
            CreateJoint(1, 12); // 12, HipLeft
            CreateJoint(1, 16); // 16, HipRight

            output += "}\r\n";
        }

        private void CreateJoint(int depth, int index) {
            for (int i = 0; i < depth; i++)
                output += "\t";
            
            // Write name of joint, if end site just write "End Site"
            if (index != 3 && index != 7 && index != 11 && index != 15 && index != 19)
                output += "JOINT " + ((JointType)index).ToString() + "\r\n";
            else
                output += "End Site\r\n";
            for (int i = 0; i < depth; i++)
                output += "\t";
            output += "{\r\n";

            for (int i = 0; i < depth+1; i++)
                output += "\t";            
            // Write joint offset
            output += "OFFSET";
            OutputOffsets(index, ref output);

            
            // Don't output Channels if we are at one of our end nodes
            if (index != 3 && index != 7 && index != 11 && index != 15 && index != 19) {
                for (int i = 0; i < depth + 1; i++)
                    output += "\t";
                output += "CHANNELS 3 Zrotation Xrotation Yrotation\r\n";
            }

            int[] children = new int[1];
            switch (index) { 
                case 1:
                    children = new int[1] { 2 };
                    break;
                case 2:
                    children = new int[3] { 3,4,8 };
                    break;
                case 4:
                    children = new int[1] { 5 };
                    break;
                case 5:
                    children = new int[1] { 6 };
                    break;
                case 6:
                    children = new int[1] { 7 };
                    break;
                case 8:
                    children = new int[1] { 9 };
                    break;
                case 9:
                    children = new int[1] { 10 };
                    break;
                case 10:
                    children = new int[1] { 11 };
                    break;
                case 12:
                    children = new int[1] { 13 };
                    break;
                case 13:
                    children = new int[1] { 14 };
                    break;
                case 14:
                    children = new int[1] { 15 };
                    break;
                case 16:
                    children = new int[1] { 17 };
                    break;
                case 17:
                    children = new int[1] { 18 };
                    break;
                case 18:
                    children = new int[1] { 19 };
                    break;
                default:
                    for (int i = 0; i < depth; i++)
                        output += "\t";
                    output += "}\r\n";
                    return;
            }

            // Create joints out of children
            for (int i = 0; i < children.Length; i++) {
                CreateJoint(depth + 1, children[i]);
            }

            for (int i = 0; i < depth; i++)
                output += "\t";

            output += "}\r\n";
        }

        private void OutputOffsets(int boneIndex, ref string output) {
            string[] skeletonOffsets = new string[20];

            skeletonOffsets[(int)JointType.Spine]           = "\t 0.00\t 0.00\t 0.00";
            skeletonOffsets[(int)JointType.ShoulderCenter]  = "\t 0.00\t 0.00\t 0.00";
            skeletonOffsets[(int)JointType.Head]            = "\t 0.00\t 0.00\t 0.00";
            skeletonOffsets[(int)JointType.ShoulderLeft]    = "\t 0.00\t 0.00\t 0.00";
            skeletonOffsets[(int)JointType.ElbowLeft]       = "\t 0.00\t 0.00\t 0.00";
            skeletonOffsets[(int)JointType.WristLeft]       = "\t 0.00\t 0.00\t 0.00";
            skeletonOffsets[(int)JointType.HandLeft]        = "\t 0.00\t 0.00\t 0.00";
            skeletonOffsets[(int)JointType.ShoulderRight]   = "\t 0.00\t 0.00\t 0.00";
            skeletonOffsets[(int)JointType.ElbowRight]      = "\t 0.00\t 0.00\t 0.00";
            skeletonOffsets[(int)JointType.WristRight]      = "\t 0.00\t 0.00\t 0.00";
            skeletonOffsets[(int)JointType.HandRight]       = "\t 0.00\t 0.00\t 0.00";
            skeletonOffsets[(int)JointType.HipLeft]         = "\t 0.00\t 0.00\t 0.00";
            skeletonOffsets[(int)JointType.KneeLeft]        = "\t 0.00\t 0.00\t 0.00";
            skeletonOffsets[(int)JointType.AnkleLeft]       = "\t 0.00\t 0.00\t 0.00";
            skeletonOffsets[(int)JointType.FootLeft]        = "\t 0.00\t 0.00\t 0.00";
            skeletonOffsets[(int)JointType.HipRight]        = "\t 0.00\t 0.00\t 0.00";
            skeletonOffsets[(int)JointType.KneeRight]       = "\t 0.00\t 0.00\t 0.00";
            skeletonOffsets[(int)JointType.AnkleRight]      = "\t 0.00\t 0.00\t 0.00";
            skeletonOffsets[(int)JointType.FootRight]       = "\t 0.00\t 0.00\t 0.00";

            output += skeletonOffsets[boneIndex] + "\r\n";
        }

        private void CreateMotion() {
            output += "MOTION\r\n";
            output += "Frames:\t" + trackedSkeletonFrames.Count + "\r\n";
            output += "Frame Time: 0.033333\r\n";

            CreateMotionData();
        }

        private void CreateMotionData() {
            foreach (Skeleton skeleton in trackedSkeletonFrames) {
                // Get Root's position
                output += Math.Round(skeleton.Joints[JointType.HipCenter].Position.X, PRECISION).ToString() + " ";
                output += Math.Round(skeleton.Joints[JointType.HipCenter].Position.Y, PRECISION).ToString() + " ";
                output += Math.Round(skeleton.Joints[JointType.HipCenter].Position.Z, PRECISION).ToString() + " ";

                // Get bone orientations
                for (int i = 0, j = skeleton.Joints.Count; i < j; i++) {
                    // TODO: Compute joint rotations
                    Vector4 jointRots = QuatertionToEuler(skeleton.BoneOrientations[(JointType)i].HierarchicalRotation.Quaternion);

                    output += Math.Round(jointRots.Z, PRECISION) + " ";
                    output += Math.Round(jointRots.X, PRECISION) + " ";
                    output += Math.Round(jointRots.Y, PRECISION) + " ";
                }

                output += "\r\n";
            }
        }

        private Vector4 QuatertionToEuler(Vector4 quatRot) {
            Vector4 eulerRot = new Vector4();

            // Pitch
            eulerRot.X = (float)Math.Atan2(2 * quatRot.X * quatRot.W - 2 * quatRot.Y * quatRot.Z, 1 - 2 * quatRot.X * quatRot.X - 2 * quatRot.Z * quatRot.Z);
            eulerRot.X *= 180f / (float)Math.PI;
            // Yaw
            eulerRot.Y = (float)Math.Asin(2 * quatRot.X * quatRot.Y + 2 * quatRot.Z * quatRot.W);
            eulerRot.Y *= 180f / (float)Math.PI;
            // Roll
            eulerRot.Z = (float)Math.Atan2(2 * quatRot.Y * quatRot.W - 2 * quatRot.X * quatRot.Z, 1 - 2 * quatRot.Y * quatRot.Y - 2 * quatRot.Z * quatRot.Z);
            eulerRot.Z *= 180f / (float)Math.PI;

            return eulerRot;
        }
    }
}
