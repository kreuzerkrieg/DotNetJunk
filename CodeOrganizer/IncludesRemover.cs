using System;
using CodeOrganizer;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.VCCodeModel;
using Microsoft.VisualStudio.VCProjectEngine;
using System.Collections.Generic;

namespace CPPHelpers
{
    public class IncludesRemover
    {
        private DTE2 mApplication;
        private Logger mLogger;
        public IncludesRemover(Logger logger,DTE2 oApplication)
        {
            mLogger = logger;
            mApplication = oApplication;
        }

        public Boolean RemoveIncludes(VCFile oFile)
        {
            Boolean bRetVal = false;
            if (oFile.Extension != ".cpp" ||
                oFile.Name.ToLowerInvariant().Contains("stdafx.cpp"))
            {
                return bRetVal;
            }

            if (Utilities.IsThirdPartyFile(oFile.FullPath, Utilities.GetCurrentConfiguration((VCProject)oFile.project)))
            {
                return bRetVal;
            }

            if (!Utilities.CompileFile(oFile, true))
            {
                mLogger.PrintMessage("ERROR: File '" + oFile.Name + "' must be in a compilable condition before you proceed! Aborting...");
                return bRetVal;
            }
            try
            {
                SortedDictionary<IncludesKey, VCCodeInclude> oIncludes = new SortedDictionary<IncludesKey, VCCodeInclude>();
                List<String> SkipFiles = new List<String>();
                Utilities.RetrieveIncludes(oFile, ref oIncludes);
                Utilities.RetrieveFilesToSkip(oFile, ref SkipFiles);

                foreach (VCCodeInclude oCI in oIncludes.Values)
                {
                    IncludeStructEx oInc = null;
                    Boolean isLocal = Utilities.IsLocalFile(oCI, ref oInc);
                    if (oInc != null && SkipFiles.Contains(oInc.sFullPath.ToUpperInvariant()))
                    {
                        continue;
                    }
                    String sIncludeFile = oCI.FullName;
                    TextPoint oStartPoint = oCI.StartPoint;
                    EditPoint oEditPoint = oStartPoint.CreateEditPoint();
                    String sOrigText = oEditPoint.GetText(oCI.EndPoint);
                    oEditPoint.Insert("//");
                    Utilities.SaveFile((ProjectItem)oFile.Object);
                    if (!Utilities.CompileFile(oFile))
                    {
                        oEditPoint.ReplaceText(oStartPoint, "", (int)vsEPReplaceTextOptions.vsEPReplaceTextAutoformat);
                        Utilities.SaveFile((ProjectItem)oFile.Object);
                    }
                    else
                    {
                        mLogger.PrintMessage("Dirrective " + sOrigText + " in file " + oFile.Name + " has been found as unnesessary and removed.");
                        bRetVal = true;
                    }
                }
                if (bRetVal)
                    bRetVal = RemoveIncludes(oFile);
            }
            catch (SystemException ex)
            {
                String msg = ex.Message;
            }
            return bRetVal;
        }
    }
}