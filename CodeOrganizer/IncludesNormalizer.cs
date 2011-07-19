using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.VCCodeModel;
using Microsoft.VisualStudio.VCProjectEngine;
using CPPHelpers;

namespace CodeOrganizer
{
    class IncludesNormalizer
    {
        private Logger mLogger;
        private DTE2 mApplication;

        public IncludesNormalizer(Logger logger, DTE2 oApplication)
        {
            mLogger = logger;
            mApplication = oApplication;
        }

        public Boolean NormalizeIncludes(VCFile oFile)
        {
            try
            {
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
                    IncludeStructEx oIncEx = arrIncludesToRemove[j];
                    if (oIncEx.bLocalFile)
                    {
                        TextPoint oStartPoint = oIncEx.oInc.StartPoint;
                        EditPoint oEditPoint = oStartPoint.CreateEditPoint();
                        String sTmpInclude = oEditPoint.GetText(oIncEx.oInc.EndPoint);
                        String sNewDirective = "#include \"" + oIncEx.sRelativePath + "\"";
                        if (sTmpInclude != sNewDirective)
                        {
                            oEditPoint.ReplaceText(oIncEx.oInc.EndPoint, sNewDirective, (int)vsEPReplaceTextOptions.vsEPReplaceTextAutoformat);
                            mLogger.PrintMessage("Directive " + sTmpInclude + " normalizer as " + sNewDirective);
                        }
                        else
                        {
                            mLogger.PrintMessage("Directive " + sTmpInclude + " already normalized.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                mLogger.PrintMessage("Failed to parse file: " + oFile.FullPath + " while normalizing includes. Reason: " + ex.Message);
                return false;
            }
            return true;
        }
    }
}
