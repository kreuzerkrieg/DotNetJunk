﻿using System;
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
            VCFile CPP = oProject.AddFile(CPPPath);
            IVCCollection oConfigurations = (IVCCollection)CPP.FileConfigurations;
            foreach (VCFileConfiguration oConfig in oConfigurations)
            {
                VCCLCompilerTool oCompilerTool = (VCCLCompilerTool)oConfig.Tool;
                oCompilerTool.UsePrecompiledHeader = pchOption.pchCreateUsingSpecific;
            }
            String HPath = Path.Combine(oProject.ProjectDirectory, "stdafx.h");
            StreamWriter oPCHH = File.CreateText(HPath);
            oPCHH.Write(Resources.PCHData.stdafx_h.Replace(@"$$ProjectName$$",oProject.Name.ToUpperInvariant()));
            oPCHH.Close();
            oProject.AddFile(HPath);
        }

        private void addPrecompiledHeaderIncludes(VCProject oProject)
        {
            IVCCollection oConfigurations = ((IVCCollection)oProject.Configurations);
            foreach (VCConfiguration oConfig in oConfigurations)
            {
                VCCLCompilerTool oCompilerTool = (VCCLCompilerTool)((IVCCollection)oConfig.Tools).Item("VCCLCompilerTool");
                oCompilerTool.UsePrecompiledHeader = pchOption.pchUseUsingSpecific;
            }
        }

        private void addPrecompiledHeaderToProject(VCProject oProject)
        {
            foreach (VCFile oFile in oProject.GetFilesWithItemType("ClCompile"))
            {
                ProjectItem oPI = ((ProjectItem)oFile.Object);
                VCFileCodeModel oFCM = (VCFileCodeModel)oPI.FileCodeModel;
                if (oFCM == null)
                {
                    throw new Exception("Cannot get FilecodeModel for file " + oFile.FullPath);
                }
                oFCM.AddInclude("\"stdafx.h\"", Type.Missing);
            }
        }
        private DTE2 mApplication;
        private Logger mLogger;
    }
}