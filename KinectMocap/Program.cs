using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KinectMocap
{
    class Program
    {
        static void Main(string[] args)
        {
            KinectMocap mocap = new KinectMocap();
            mocap.Init();

            while ( mocap.Update() );
        }
    }
}
