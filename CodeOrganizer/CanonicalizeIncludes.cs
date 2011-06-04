using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using EnvDTE;
using Microsoft.VisualStudio.VCProjectEngine;
using Microsoft.VisualStudio.VCCodeModel;
using System.Text.RegularExpressions;
using CodeOrganizer;
using EnvDTE80;
namespace CPPHelpers
{
    class IncludesCanonicalizer
    {
        private Logger mLogger;
        private DTE2 mApplication;
        private class IncludeStructEx
        {
            public VCCodeInclude oInc;
            public String sFileName;
            public Boolean bLocalFile;
        }

        public IncludesCanonicalizer(Logger logger, DTE2 oApplication)
        {
            mLogger = logger;
            mApplication = oApplication;
        }

        public Boolean CanonicalizeIncludes(VCProject oProject, VCFile oFile)
        {
            try
            {
                SortedDictionary<IncludesKey, VCCodeInclude> oIncludes = new SortedDictionary<IncludesKey, VCCodeInclude>();
                Utilities.RetrieveIncludes(oFile, ref oIncludes);
                List<IncludeStructEx> arrIncludesToRemove = new List<IncludeStructEx>();
                String sIncludePattern = ("\\.*#.*include.*(\\<|\\\")(?'FileName'.+)(\\>|\\\")");
                foreach (VCCodeInclude oCI in oIncludes.Values)
                {
                    String sIncludeFile = oCI.FullName;
                    TextPoint oStartPoint = oCI.StartPoint;
                    EditPoint oEditPoint = oStartPoint.CreateEditPoint();
                    String sTmpInclude = oEditPoint.GetText(oCI.EndPoint);
                    Match match = Regex.Match(sTmpInclude, sIncludePattern);
                    String sIncFullName;
                    if (match.Success)
                    {
                        sIncFullName = match.Groups["FileName"].Value;
                        FileInfo oFI = new FileInfo(Path.Combine(Path.GetDirectoryName(oFile.FullPath), sIncFullName));
                        DirectoryInfo oParentFolder = new DirectoryInfo(Path.GetFullPath(oProject.ProjectDirectory)).Parent;
                        FileInfo oFIParent = new FileInfo(Path.Combine(oParentFolder.FullName, sIncFullName));
                        IncludeStructEx oInc = new IncludeStructEx();
                        oInc.oInc = oCI;
                        oInc.sFileName = sIncFullName;
                        if (oFI.Exists || oFIParent.Exists)
                        {
                            oInc.bLocalFile = true;
                        }
                        else
                        {
                            oInc.bLocalFile = false;
                        }
                        arrIncludesToRemove.Add(oInc);
                    }
                }
                
                for (int j = 0; j < arrIncludesToRemove.Count; j++)
                {
                    IncludeStructEx oIncEx = arrIncludesToRemove[j];

                    TextPoint oStartPoint = oIncEx.oInc.StartPoint;
                    EditPoint oEditPoint = oStartPoint.CreateEditPoint();
                    String sTmpInclude = oEditPoint.GetText(oIncEx.oInc.EndPoint);
                    String sNewDirective = "#include " + ((oIncEx.bLocalFile) ? "\"" : "<") + oIncEx.sFileName + (oIncEx.bLocalFile ? "\"" : ">");
                    if (sTmpInclude != sNewDirective)
                    {
                        oEditPoint.ReplaceText(oIncEx.oInc.EndPoint, sNewDirective, (int)vsEPReplaceTextOptions.vsEPReplaceTextAutoformat);
                        mLogger.PrintMessage("Directive " + sTmpInclude + " canonicalized as " + sNewDirective);
                    }
                    else
                    {
                        mLogger.PrintMessage("Directive " + sTmpInclude + " already canonicalized.");
                    }
                }
            }
            catch (Exception ex)
            {
                mLogger.PrintMessage("Failed to parse file: " + oFile.FullPath + " while canonicalizing includes. Reason: " + ex.Message);
                return false;
            }
            return true;
        }
    }
}