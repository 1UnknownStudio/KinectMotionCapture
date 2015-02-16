using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KinectMocap
{
    class KinectMocap
    {
        private SkeletalTracker m_skeletalTracker;

        public KinectMocap() {
            m_skeletalTracker = new SkeletalTracker();
        }
    }
}
