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

        public IncludesCanonicalizer(Logger logger, DTE2 oApplication)
        {
            mLogger = logger;
            mApplication = oApplication;
        }

        public Boolean CanonicalizeIncludes(VCProject oProject, VCFile oFile)
        {
            try
            {
                if (Utilities.IsThirdPartyFile(oFile.FullPath, Utilities.GetCurrentConfiguration((VCProject)oFile.project)))
                {
                    return false;
                }
                SortedDictionary<IncludesKey, VCCodeInclude> oIncludes = new SortedDictionary<IncludesKey, VCCodeInclude>();
                Utilities.RetrieveIncludes(oFile, ref oIncludes);
                
                List<IncludeStructEx> arrIncludesToRemove = new List<IncludeStructEx>();

                foreach (VCCodeInclude oCI in oIncludes.Values)
                {
                    IncludeStructEx oInc = null;
                    Boolean isLocal = Utilities.IsLocalFile(oCI, ref oInc);
                    if (oInc != null)
                    {
                        arrIncludesToRemove.Add(oInc);
                    }
                }

                for (int j = 0; j < arrIncludesToRemove.Count; j++)
                {
                    try
                    {
                        IncludeStructEx oIncEx = arrIncludesToRemove[j];

                        TextPoint oStartPoint = oIncEx.oInc.StartPoint;
                        EditPoint oEditPoint = oStartPoint.CreateEditPoint();
                        String sTmpInclude = oEditPoint.GetText(oIncEx.oInc.EndPoint);
                        String sNewDirective = "#include " + ((oIncEx.bLocalFile) ? "\"" : "<") + oIncEx.sFileName.Replace("\\", "/") + (oIncEx.bLocalFile ? "\"" : ">");
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
                    catch (Exception ex)
                    {
                        mLogger.PrintMessage("Failed to parse file: " + oFile.FullPath + " while canonicalizing includes. Reason: " + ex.Message);
                        return false;
                    }
                }

            }
            catch (Exception ex)
            {
                mLogger.PrintMessage("Failed to parse file: " + oFile.FullPath + " while canonicalizing includes. Reason: " + ex.Message);
                return false;
            }
            Utilities.SaveFile((ProjectItem)oFile.Object);
            return true;
        }
    }
}