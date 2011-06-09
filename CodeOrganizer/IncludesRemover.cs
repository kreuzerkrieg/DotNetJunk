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
            if (oFile.Extension != ".cpp")
                return bRetVal;
            try
            {
                SortedDictionary<IncludesKey, VCCodeInclude> oIncludes = new SortedDictionary<IncludesKey, VCCodeInclude>();
                Utilities.RetrieveIncludes(oFile, ref oIncludes);

                foreach (VCCodeInclude oCI in oIncludes.Values)
                {
                    String sIncludeFile = oCI.FullName;
                    TextPoint oStartPoint = oCI.StartPoint;
                    EditPoint oEditPoint = oStartPoint.CreateEditPoint();
                    String sOrigText = oEditPoint.GetText(oCI.EndPoint);
                    oEditPoint.Insert("//");
                    Utilities.SaveFile(oFile.Object);
                    if (!Utilities.CompileFile(oFile))
                    {
                        oEditPoint.ReplaceText(oStartPoint, "", (int)vsEPReplaceTextOptions.vsEPReplaceTextAutoformat);
                        Utilities.SaveFile(oFile.Object);
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