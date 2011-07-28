using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using EnvDTE;
using Microsoft.VisualStudio.VCCodeModel;
using Microsoft.VisualStudio.VCProjectEngine;
using CodeOrganizer;
using EnvDTE80;
using CPPHelpers;

namespace CodeOrganizer
{
    class AddPCHtoProject
    {
        public AddPCHtoProject(Logger logger, DTE2 oApplication)
        {
            mLogger = logger;
            mApplication = oApplication;
        }

        public void PCHize(VCProject oProject)
        {
            try
            {
                mLogger.PrintMessage("Processing project ..::" + oProject.Name + "::..");
                //if (!Utilities.BuildCurrentConfiguration(oProject))
                //{
                //    mLogger.PrintMessage("ERROR: Project '" + oProject.Name + "' must be in a buildable condition before you proceed! Aborting...");
                //    return;
                //}
                VCConfiguration oActiveConfig = Utilities.GetCurrentConfiguration(oProject);
                if (!Utilities.hasPrecompileHeader(oActiveConfig))
                {
                    addPrecompiledHeaderToProject(oProject);
                    addPrecompiledHeaderFiles(oProject);
                }
                addPrecompiledHeaderIncludes(oProject);
            }
            catch (SystemException ex)
            {
                mLogger.PrintMessage("Failed when processing  project \"" + oProject.Name + "\". Reason: " + ex.Message);
            }
        }

        private void addPrecompiledHeaderFiles(VCProject oProject)
        {
            String CPPPath = Path.Combine(oProject.ProjectDirectory, "stdafx.cpp");
            StreamWriter oPCHCPP = File.CreateText(CPPPath);
            oPCHCPP.Write(Resources.PCHData.stdafx_cpp);
            oPCHCPP.Close();
            VCFile CPP = (VCFile)oProject.AddFile(CPPPath);
            IVCCollection oConfigurations = (IVCCollection)CPP.FileConfigurations;
            foreach (VCFileConfiguration oConfig in oConfigurations)
            {
                try
                {
                    VCCLCompilerTool oCompilerTool = (VCCLCompilerTool)oConfig.Tool;
                    oCompilerTool.UsePrecompiledHeader = pchOption.pchCreateUsingSpecific;
                }
                catch (Exception ex)
                {
                    mLogger.PrintMessage("Failed when setting stdafx.cpp in configuration \"" + oConfig.Name + "\" to create precompiled headers. Reason: " + ex.Message);
                }
            }
            String HPath = Path.Combine(oProject.ProjectDirectory, "stdafx.h");
            StreamWriter oPCHH = File.CreateText(HPath);
            oPCHH.Write(Resources.PCHData.stdafx_h.Replace(@"$$ProjectName$$", oProject.Name.ToUpperInvariant()));
            oPCHH.Close();
            oProject.AddFile(HPath);
        }

        private void addPrecompiledHeaderIncludes(VCProject oProject)
        {
            IVCCollection oConfigurations = ((IVCCollection)oProject.Configurations);
            foreach (VCConfiguration oConfig in oConfigurations)
            {
                try
                {
                    VCCLCompilerTool oCompilerTool = (VCCLCompilerTool)((IVCCollection)oConfig.Tools).Item("VCCLCompilerTool");
                    oCompilerTool.UsePrecompiledHeader = pchOption.pchUseUsingSpecific;
                }
                catch (Exception ex)
                {
                    mLogger.PrintMessage("Failed when setting configuration \"" + oConfig.Name + "\" to use precompiled headers. Reason: " + ex.Message);
                }
            }
        }

        private void addPrecompiledHeaderToProject(VCProject oProject)
        {
#if RUNNING_ON_FW_4
            foreach (VCFile oFile in oProject.GetFilesWithItemType("ClCompile"))
#endif
#if !RUNNING_ON_FW_4
            foreach (VCFile oFile in (IVCCollection)oProject.Files)
#endif
            {
                try
                {
                    if (Utilities.IsThirdPartyFile(oFile.FullPath, Utilities.GetCurrentConfiguration((VCProject)oFile.project)))
                    {
                        continue;
                    }
                    ProjectItem oPI = ((ProjectItem)oFile.Object);
#if !RUNNING_ON_FW_4
                    if (oFile.FileType == eFileType.eFileTypeCppCode)
#endif
                    {
                        VCFileCodeModel oFCM = (VCFileCodeModel)oPI.FileCodeModel;
                        if (oFCM == null)
                        {
                            throw new Exception("Cannot get FilecodeModel for file " + oFile.Name);
                        }
                        EditPoint oEditPoint = oFCM.StartPoint.CreateEditPoint();
                        oEditPoint.Insert("#include \"stdafx.h\"" + Environment.NewLine);
                        Utilities.SaveFile(oPI);
                    }
                }
                catch (Exception ex)
                {
                    mLogger.PrintMessage("Failed when adding \"stdafx.h\" include directive to \"" + oFile.Name + "\". Reason: " + ex.Message);
                }
            }
        }
        private DTE2 mApplication;
        private Logger mLogger;
    }
}
