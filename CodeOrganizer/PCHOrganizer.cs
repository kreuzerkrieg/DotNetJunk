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

namespace CPPHelpers
{
    public class PrecompiledHeadersOrganizer
    {
        public PrecompiledHeadersOrganizer(Logger logger, DTE2 oApplication)
        {
            mLogger = logger;
            mApplication = oApplication;
        }

        public void CleanFiles(VCFile oFile)
        {
            VCProject oCurrentProject = (VCProject)oFile.project;
            try
            {
                mLogger.PrintMessage("Processing file ..::" + oFile.FullPath + "::..");
                VCConfiguration oActiveConfig = Utilities.GetCurrentConfiguration(oCurrentProject);
                if (Utilities.hasPrecompileHeader(oActiveConfig))
                {
                    List<String> arrToPch = new List<String>();
                    VCFile oStdAfx = GetStdAfxFile(oCurrentProject);
                    IncludeComparer comparer = new IncludeComparer();
                    SortedDictionary<IncludesKey, VCCodeInclude> oIncludes = new SortedDictionary<IncludesKey, VCCodeInclude>(comparer);
                    Utilities.RetrieveIncludes(oFile, ref oIncludes);
                    List<String> IncludeDirs = Utilities.GetIncludePaths(oCurrentProject);
                    RemoveIncludes(oFile, IncludeDirs, oIncludes, ref arrToPch);
                    arrToPch.Sort();
                    for (int i = 0; i < arrToPch.Count; i++)
                    {
                        MoveToPCH(arrToPch[i], oStdAfx);
                    }
                }
                else
                {
                    mLogger.PrintMessage("Project " + oCurrentProject.Name + " does not employ PCH.");
                }

                
                return;
            }
            catch (SystemException ex)
            {
                mLogger.PrintMessage("Failed when processing files in project \"" + oCurrentProject.Name + "\". Reason: " + ex.Message);
            }
        }

        private VCFile GetStdAfxFile(VCProject oProj)
        {
            try
            {
                foreach (VCFile oFile in (IVCCollection)oProj.Files)
                {
                    if (oFile.Name.ToLowerInvariant().Contains("stdafx.h"))
                        return oFile;
                }
            }
            catch (SystemException ex)
            {
                mLogger.PrintMessage("Failed when processing files in project \"" + oProj.Name + "\". Reason: " + ex.Message);
            }
            return null;
        }

        private Boolean RemoveIncludes(
            VCFile oFile,
            List<String> arIncludes,
            SortedDictionary<IncludesKey, VCCodeInclude> oIncludes,
            ref List<String> arToPCH)
        {
            Boolean bRetVal = false;
            if (oFile.Extension != ".cpp" &&
                oFile.Extension != ".c" &&
                oFile.Extension != ".h" &&
                oFile.Extension != ".hpp")
                return bRetVal;
            if (oFile.Name.ToLowerInvariant().Contains("stdafx"))
                return bRetVal;
            String sIncludePattern = ("\\.*#.*include.*(\\<|\\\")(?'FileName'.+)(\\>|\\\")");
           
            try
            {
                List<KeyValuePair<TextPoint, TextPoint>> arrIncludesToRemove = new List<KeyValuePair<TextPoint, TextPoint>>();
                VCConfiguration oCurConfig = Utilities.GetCurrentConfiguration((VCProject)oFile.project);
                foreach (VCCodeInclude oCI in oIncludes.Values)
                {
                    TextPoint oStartPoint = oCI.StartPoint;
                    EditPoint oEditPoint = oStartPoint.CreateEditPoint();
                    String sTmpInclude = oEditPoint.GetText(oCI.EndPoint);
                    Match match = Regex.Match(sTmpInclude, sIncludePattern);
                    String sIncFullName;
                    if (match.Success)
                    {
                        sIncFullName = match.Groups["FileName"].Value;
                        for (int i = 0; i < arIncludes.Count; i++)
                        {
                            FileInfo oTmpFI = new FileInfo(Path.Combine(arIncludes[i], sIncFullName));

                            if (oTmpFI.Exists)
                            {
                                if (Utilities.IsThirdPartyFile(oTmpFI.FullName, oCurConfig))
                                {
                                    arrIncludesToRemove.Add(new KeyValuePair<TextPoint, TextPoint>(oCI.StartPoint, oCI.EndPoint));
                                    if (!arToPCH.Contains(sTmpInclude))
                                    {
                                        arToPCH.Add(sTmpInclude);
                                    }
                                    break;
                                }
                                else
                                {
                                    break; // File found, not thirdparty
                                }
                            }
                        }
                    }
                }
                for (int j = 0; j < arrIncludesToRemove.Count; j++)
                {
                    TextPoint oStartPoint = arrIncludesToRemove[j].Key;
                    TextPoint oEndPoint = arrIncludesToRemove[j].Value;
                    EditPoint oEditPoint = oStartPoint.CreateEditPoint();
                    mLogger.PrintMessage("Directive  " + oEditPoint.GetText(oEndPoint) + " removed from " + oFile.Name);
                    oEditPoint.Delete(oEndPoint);
                    oEditPoint.DeleteWhitespace(vsWhitespaceOptions.vsWhitespaceOptionsVertical);
                }
                Utilities.SaveFile((ProjectItem)oFile.Object);
            }
            catch (Exception ex)
            {
                mLogger.PrintMessage("Failed to parse file: " + oFile.FullPath + ".Reason: " + ex.Message);
                return false;
            }
            return true;
        }

        private void MoveToPCH(String sTmpInclude, VCFile oPCH)
        {
            ProjectItem oPI = ((ProjectItem)oPCH.Object);

            VCFileCodeModel oFCM = (VCFileCodeModel)oPI.FileCodeModel;
            if (oFCM == null)
            {
                mLogger.PrintMessage("Cannot get FilecodeModel for file " + oPCH.FullPath);
            }
            EditPoint oEditPoint = oFCM.EndPoint.CreateEditPoint();
            oEditPoint.Insert(sTmpInclude + Environment.NewLine);
            oFCM.StartPoint.CreateEditPoint().SmartFormat(oFCM.EndPoint);
            mLogger.PrintMessage("Include " + sTmpInclude + " moved to stdafx.h");
        }

        private DTE2 mApplication;
        private Logger mLogger;
    }
}