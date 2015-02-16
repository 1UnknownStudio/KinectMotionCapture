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
            SkeletalTracker m_skeletalTracker = new SkeletalTracker();
            m_skeletalTracker.StartKinectST();

            while (true) { 
                
            }
        }
    }
}
