using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.VCProjectEngine;

namespace CPPHelper
{
    class BuildCallbacks : IVCBuildCompleteCallback
    {
        public volatile bool Semaphore = false;
        public void OnBuildCompleted(uint buildId, bool fSuccessful)
        {
            Semaphore = true;

            return;
        }
    }
}
