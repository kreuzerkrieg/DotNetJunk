using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.VCProjectEngine;
using CPPHelpers;

namespace CPPHelper
{
    class LinkCleaner
    {
        public LinkCleaner(Logger logger)
        {
            mLogger = logger;
        }

        public void RemoveUnnesessaryLibraries(VCProject oProject)
        {
            IVCCollection oConfigurations = (IVCCollection)oProject.Configurations;
            foreach (VCConfiguration oConfiguration in oConfigurations)
            {
                if (!BuildOperations.BuildConfiguration(oProject, oConfiguration))
                {
                    mLogger.PrintError("ERROR: Project '" + oProject.Name + "' must be in a buildable condition before you proceed! Aborting...");
                    return;
                }
                else
                {
                    CleanLibraries(oProject, oConfiguration);
                }
            }
        }

        private void CleanLibraries(VCProject oProject, VCConfiguration oConfiguration)
        {
            VCLinkerTool Linker = (VCLinkerTool)((IVCCollection)oConfiguration.Tools).Item("VCLinkerTool");
            if (Linker != null)
            {
                List<String> Libraries = GetLibraries(Linker);
                for (int i=0; i<Libraries.Count; i++)
                {
                    List<String> NewLibs = Libraries.GetRange(i, 1);
                    Libraries.RemoveRange(i, 1);
                    Linker.AdditionalDependencies = String.Join(" ", Libraries.ToArray());
                    oProject.Save();
                    if (BuildOperations.BuildConfiguration(oProject, oConfiguration))
                    {
                        i--;
                        mLogger.PrintMessage("Library " + String.Join(" ", NewLibs.ToArray()) + " has been found unnesessary and removed.");
                    }
                    else
                    {
                        Libraries.InsertRange(i, NewLibs);
                    }
                }
                Libraries.Sort();
                Linker.AdditionalDependencies = String.Join(" ", Libraries.ToArray());
                oProject.Save();
            }
        }

        private List<String> GetLibraries(VCLinkerTool Linker)
        {
            String Libs = Linker.AdditionalDependencies;
            List<String> RetVal = new List<String>(Libs.Split(new char[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries));
            RetVal.Sort();
            return RetVal;
        }
        private Logger mLogger;
    }
}
