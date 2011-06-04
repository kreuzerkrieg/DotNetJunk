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
                OutputWindow oOutputWin = (OutputWindow)mApplication.Windows.Item(Constants.vsWindowKindOutput).Collection.Item("Output").Object;
                OutputWindowPane oPane = oOutputWin.OutputWindowPanes.Item("Build");

                SortedDictionary<IncludesKey, VCCodeInclude> oIncludes = new SortedDictionary<IncludesKey, VCCodeInclude>();
                Utilities.RetrieveIncludes(oFile, ref oIncludes);

                foreach (VCCodeInclude oCI in oIncludes.Values)
                {
                    String sIncludeFile = oCI.FullName;
                    TextPoint oStartPoint = oCI.StartPoint;
                    EditPoint oEditPoint = oStartPoint.CreateEditPoint();
                    String sOrigText = oEditPoint.GetText(oCI.EndPoint);
                    oEditPoint.Insert("//");
                    oPane.Activate();
                    oPane.Clear();
                    VCFileConfiguration oCurrConfig = Utilities.GetCurrentFileConfiguration(oFile);
                    oCurrConfig.Compile(true, true);
                    TextDocument oTD = oPane.TextDocument;
                    EditPoint oOutEP = oTD.CreateEditPoint(oTD.StartPoint);
                    oTD.Selection.SelectAll();
                    if (!oTD.Selection.Text.Contains(" 0 error"))
                    {
                        oEditPoint.ReplaceText(oStartPoint, "", (int)vsEPReplaceTextOptions.vsEPReplaceTextAutoformat);
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