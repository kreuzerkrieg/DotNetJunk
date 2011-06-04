using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.VCCodeModel;
using Microsoft.VisualStudio.VCProjectEngine;
using CodeOrganizer;

namespace CPPHelpers
{
    public class IncludesSorter
    {
        private Logger mLogger;
        private DTE2 mApplication;

        public IncludesSorter(Logger logger, DTE2 oApplication)
        {
            mLogger = logger;
            mApplication = oApplication;
        }

        public void SortIncludes(VCFile oFile)
        {
            try
            {
                IncludeComparer comparer = new IncludeComparer();
                SortedDictionary<IncludesKey, VCCodeInclude> oIncludes = new SortedDictionary<IncludesKey, VCCodeInclude>(comparer);
                mLogger.PrintMessage("Processing file ..::" + oFile.FullPath + "::..");
                Utilities.RetrieveIncludes(oFile, ref oIncludes);
                SortInclude(oIncludes);
            }
            catch (SystemException ex)
            {
                mLogger.PrintMessage("Failed when processing file " + oFile.Name + ". Reason: " + ex.Message);
            }
        }

        private void SortInclude(SortedDictionary<IncludesKey, VCCodeInclude> oIncludes)
        {
            EditPoint oInserPoint = null;
            List<String> arrIncludesToInsert = new List<String>(oIncludes.Count);
            List<KeyValuePair<TextPoint, TextPoint>> arrTextPairs = new List<KeyValuePair<TextPoint, TextPoint>>(oIncludes.Count);
            foreach (VCCodeInclude oInclude in oIncludes.Values)
            {
                arrTextPairs.Add(new KeyValuePair<TextPoint, TextPoint>(oInclude.StartPoint, oInclude.EndPoint));
                String sIncludeText = (oInclude.StartPoint.CreateEditPoint().GetText(oInclude.EndPoint) + Environment.NewLine);
                if (!arrIncludesToInsert.Contains(sIncludeText))
                {
                    arrIncludesToInsert.Add(sIncludeText);
                }
            }
            for (int i = 0; i < arrTextPairs.Count; i++)
            {
                KeyValuePair<TextPoint, TextPoint> oInclude = arrTextPairs[i];
                if (oInserPoint == null)
                {
                    oInserPoint = oInclude.Key.CreateEditPoint();
                }
                EditPoint oStartPoint = oInclude.Key.CreateEditPoint();
                oStartPoint.Delete(oInclude.Value);
                oInclude.Key.CreateEditPoint().DeleteWhitespace(vsWhitespaceOptions.vsWhitespaceOptionsVertical);

            }
            for (int i = 0; i < arrIncludesToInsert.Count; i++)
            {
                oInserPoint.Insert(arrIncludesToInsert[i]);
            }
        }

    }
}