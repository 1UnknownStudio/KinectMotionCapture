using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using Microsoft.Kinect;

namespace KinectMocap
{
    class Program
    {
        static void Main(string[] args)
        {
            KinectMocap mocap = new KinectMocap();
            mocap.Init();


            while (mocap.Update()) ;
   
        }
    }
}
