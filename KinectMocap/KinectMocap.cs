using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KinectMocap
{
    class KinectMocap
    {
        private SkeletalTracker skeletalTracker;

        public KinectMocap() {
            skeletalTracker = new SkeletalTracker();
        }

        public void Init() {
            skeletalTracker.StartKinectST();
        }

        public bool Update() {

            return true;
        }
    }
}
