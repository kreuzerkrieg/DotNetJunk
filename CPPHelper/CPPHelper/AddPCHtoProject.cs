using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using EnvDTE;
using Microsoft.VisualStudio.VCCodeModel;
using Microsoft.VisualStudio.VCProjectEngine;
using CPPHelper;
using EnvDTE80;
using CPPHelpers;

namespace CPPHelper
{
    class AddPCHtoProject
    {
        public AddPCHtoProject(Logger logger)
        {
            mLogger = logger;
            StdAfxHName = "StdAfx.h";
        }

        public void PCHize(VCProject oProject)
        {
            try
            {
                mLogger.PrintMessage("Processing project ..::" + oProject.Name + "::..");
                if (!BuildOperations.BuildCurrentConfiguration(oProject))
                {
                    mLogger.PrintError("ERROR: Project '" + oProject.Name + "' must be in a buildable condition before you proceed! Aborting...");
                    return;
                }
                VCConfiguration oActiveConfig = Utilities.GetCurrentConfiguration(oProject);
                if (!Utilities.hasPrecompileHeader(oActiveConfig))
                {
                    addPrecompiledHeaderIncludes(oProject);
                    addPrecompiledHeaderToProject(oProject);
                    addPrecompiledHeaderFiles(oProject);
                }
                else
                {
                    mLogger.PrintMessage("Project '" + oProject.Name + "' already employs precompiled headers");
                }
                Utilities.Sleep(10);
            }
            catch (SystemException ex)
            {
                mLogger.PrintMessage("Failed when processing  project \"" + oProject.Name + "\". Reason: " + ex.Message);
            }
        }

        private void addPrecompiledHeaderFiles(VCProject oProject)
        {
            try
            {
                String HPath = Path.Combine(oProject.ProjectDirectory, StdAfxHName);
                if (!File.Exists(HPath))
                {
                    StreamWriter oPCHH = File.CreateText(HPath);
                    oPCHH.Write(Resources.PCHData.stdafx_h.Replace(@"$$ProjectName$$", oProject.Name.ToUpperInvariant()));
                    oPCHH.Close();
                }
                if (Utilities.GetFile(oProject,HPath) == null)
                    oProject.AddFile(HPath);

                String CPPPath = Path.Combine(oProject.ProjectDirectory, "stdafx.cpp");
                if (!File.Exists(CPPPath))
                {
                    StreamWriter oPCHCPP = File.CreateText(CPPPath);
                    oPCHCPP.Write(Resources.PCHData.stdafx_cpp);
                    oPCHCPP.Close();
                }
                VCFile CPP = null;
                if (Utilities.GetFile(oProject, CPPPath) == null)
                    CPP = (VCFile)oProject.AddFile(CPPPath);
                else
                    CPP = Utilities.GetFile(oProject, CPPPath);
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
            }
            catch (Exception ex)
            {
                mLogger.PrintMessage("Failed when adding precompiled header files. Reason: " + ex.Message);
            }
        }

        private void addPrecompiledHeaderIncludes(VCProject oProject)
        {
            IVCCollection oConfigurations = ((IVCCollection)oProject.Configurations);
            StdAfxHName = "";
            foreach (VCConfiguration oConfig in oConfigurations)
            {
                try
                {
                    Object ToolObject = ((IVCCollection)oConfig.Tools).Item("VCCLCompilerTool");
                    VCCLCompilerTool oCompilerTool = (VCCLCompilerTool)(ToolObject);
                    oConfig.ClearToolProperty(ToolObject, "UsePrecompiledHeader");
                    oConfig.ClearToolProperty(ToolObject, "PrecompiledHeaderFile");
                    oConfig.ClearToolProperty(ToolObject, "PrecompiledHeaderThrough");
                    oCompilerTool.UsePrecompiledHeader = pchOption.pchUseUsingSpecific;
                    if (String.IsNullOrEmpty(StdAfxHName))
                        StdAfxHName = oConfig.Evaluate(oCompilerTool.PrecompiledHeaderThrough);
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
                Boolean ExcludedFile = true;
                try
                {
                    if (Utilities.IsThirdPartyFile(oFile.FullPath, Utilities.GetCurrentConfiguration((VCProject)oFile.project)))
                    {
                        foreach (VCFileConfiguration Config in (IVCCollection)oFile.FileConfigurations)
                        {
                            try
                            {
                                VCCLCompilerTool oCompilerTool = (VCCLCompilerTool)Config.Tool;
                                oCompilerTool.UsePrecompiledHeader = pchOption.pchNone;
                            }
                            catch (Exception ex)
                            {
                                mLogger.PrintMessage("Failed when setting configuration \"" + Config.Name + "\" NOT to use precompiled headers in third-party source file. Reason: " + ex.Message);
                            }                          
                        }
                        continue;
                    }
                    ProjectItem oPI = ((ProjectItem)oFile.Object);
#if !RUNNING_ON_FW_4
                    if (oFile.FileType == eFileType.eFileTypeCppCode)
#endif
                    {
                        foreach (VCFileConfiguration Config in (IVCCollection)oFile.FileConfigurations)
                        {
                            try
                            {
                                VCCLCompilerTool oCompilerTool = (VCCLCompilerTool)Config.Tool;
                                if (oCompilerTool.UsePrecompiledHeader != pchOption.pchUseUsingSpecific)
                                {
                                    oCompilerTool.UsePrecompiledHeader = pchOption.pchUseUsingSpecific;
                                }
                            }
                            catch (Exception ex)
                            {
                                mLogger.PrintMessage("Failed when setting configuration \"" + Config.Name + "\" to use precompiled headers. Reason: " + ex.Message);
                            }
                            ExcludedFile &= Config.ExcludedFromBuild;
                        }
                        // This file excluded from ALL configurations, just skip it
                        if (ExcludedFile)
                            continue;
                        VCFileCodeModel oFCM = (VCFileCodeModel)oPI.FileCodeModel;
                        if (oFCM == null)
                        {
                            throw new Exception("Cannot get FilecodeModel for file " + oFile.Name);
                        }
                        VCCodeInclude PCHInclude = null;
                        if (Utilities.HasInclude(oFile, StdAfxHName, false, ref PCHInclude))
                        {
                            EditPoint EP = PCHInclude.StartPoint.CreateEditPoint();
                            EP.Delete(PCHInclude.EndPoint.CreateEditPoint());
                            EP.DeleteWhitespace(vsWhitespaceOptions.vsWhitespaceOptionsVertical);
                        }
                        EditPoint oEditPoint = oFCM.StartPoint.CreateEditPoint();
                        oEditPoint.Insert("#include \"" + StdAfxHName + "\"" + Environment.NewLine);
                        Utilities.SaveFile(oPI);
                    }
                }
                catch (Exception ex)
                {
                    mLogger.PrintMessage("Failed when adding \"" + StdAfxHName + "\" include directive to \"" + oFile.Name + "\". Reason: " + ex.Message);
                }
            }
        }
        private Logger mLogger;
        private String StdAfxHName;
    }
}
