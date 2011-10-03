using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.VCProjectEngine;
using System.Threading;
using CPPHelpers;

namespace CPPHelper
{
    class BuildOperations
    {
        class BuildProject
        {
            public Boolean Build(VCProject oProject)
            {
                Boolean RetVal = false;
                VCProjectEngineEvents events = null;
                _dispVCProjectEngineEvents_ProjectBuildFinishedEventHandler FinishedHandler = null;
                try
                {
                    events = (VCProjectEngineEvents)((VCProjectEngine)oProject.VCProjectEngine).Events;
                    FinishedHandler = new _dispVCProjectEngineEvents_ProjectBuildFinishedEventHandler(events_ProjectBuildFinished);
                    events.ProjectBuildFinished += FinishedHandler;
                    VCConfiguration oCurConfig = Utilities.GetCurrentConfiguration(oProject);
                    inProcess = true;
                    BuildErrors = 0;
                    oCurConfig.Build();

                    while (inProcess)
                    {
                        System.Windows.Forms.Application.DoEvents();
                    }
                    events.ProjectBuildFinished -= FinishedHandler;
                    RetVal = (BuildErrors == 0);
                }
                catch (Exception)
                {
                }
                return RetVal;
            }

            void events_ProjectBuildFinished(object Cfg, int warnings, int errors, bool Cancelled)
            {
                inProcess = false;
                BuildErrors = errors;
            }

            Boolean inProcess = true;
            int BuildErrors = 0;
        }
        class LinkProject
        {
            Boolean Link(VCProject oProject)
            {
                return false;
            }
        }
        class CleanProject
        {
            Boolean Clean(VCProject oProject)
            {
                return false;
            }
        }
        class RebuildProject
        {
            Boolean Rebuild(VCProject oProject)
            {
                return false;
            }
        }
        static public Boolean BuildCurrentConfiguration(VCProject oProject)
        {
            BuildProject BuildOperation = new BuildProject();
            return BuildOperation.Build(oProject);
        }
    }
}
