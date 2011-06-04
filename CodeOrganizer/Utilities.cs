using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using EnvDTE;
using Microsoft.VisualStudio.VCProjectEngine;
using Microsoft.VisualStudio.VCCodeModel;
using System.Text.RegularExpressions;

namespace CPPHelpers
{
    class Utilities
    {
        public static Boolean RebuildCurrentConfiguration(VCProject oProject)
        {
            Boolean bRetVal = false;
            VCConfiguration oCurConfig = GetCurrentConfiguration(oProject);
            oCurConfig.Rebuild();
            return bRetVal;
        }

        public static VCConfiguration GetCurrentConfiguration(VCProject oProject)
        {
            String sActiveConfig = ((Project)oProject.Object).ConfigurationManager.ActiveConfiguration.ConfigurationName;
            VCConfiguration oActiveConfig = (VCConfiguration)((IVCCollection)oProject.Configurations).Item(sActiveConfig);
            return oActiveConfig;
        }

        public static VCFileConfiguration GetCurrentFileConfiguration(VCFile oFile)
        {
            String sActiveConfig = ((ProjectItem)oFile.Object).ConfigurationManager.ActiveConfiguration.ConfigurationName;
            VCFileConfiguration oActiveConfig = (VCFileConfiguration)((IVCCollection)oFile.FileConfigurations).Item(sActiveConfig);
            return oActiveConfig;
        }

        public static List<String> GetIncludePaths(VCProject oProject)
        {
            String[] sSplitArr;
            List<String> sArr = new List<String>();
            try
            {
                VCConfiguration oActiveConfig = GetCurrentConfiguration(oProject);
                DirectoryInfo oPathToVS1 = new DirectoryInfo(oActiveConfig.Evaluate(@"$(VCInstallDir)include"));
                DirectoryInfo oPathToVS2 = new DirectoryInfo(oActiveConfig.Evaluate(@"$(VCInstallDir)atlmfc\include"));
                DirectoryInfo oPathToVS3 = new DirectoryInfo(oActiveConfig.Evaluate(@"$(VCInstallDir)PlatformSDK\include"));
                DirectoryInfo oPathToVS4 = new DirectoryInfo(oActiveConfig.Evaluate(@"$(FrameworkSDKDir)include"));

                VCCLCompilerTool oCompilerTool = (VCCLCompilerTool)((IVCCollection)oActiveConfig.Tools).Item("VCCLCompilerTool");
                String sValues = oCompilerTool.AdditionalIncludeDirectories;
                sSplitArr = sValues.Split(new Char[] { ';', ',' });
                IDictionary<String, String> tmp = new Dictionary<String, String>();

                tmp.Add(@"$(VCInstallDir)include", oPathToVS1.FullName);
                tmp.Add(@"$(VCInstallDir)atlmfc\include", oPathToVS1.FullName);
                tmp.Add(@"$(VCInstallDir)PlatformSDK\include", oPathToVS1.FullName);
                tmp.Add(@"$(FrameworkSDKDir)include", oPathToVS1.FullName);

                for (int i = 0; i < sSplitArr.Length; i++)
                {
                    String sPath = oActiveConfig.Evaluate(sSplitArr[i].Trim());

                    if (!String.IsNullOrEmpty(sPath))
                    {
                        sPath = sPath.Replace("\"", "") + "\\";
                        if (!Path.IsPathRooted(sPath))
                            sPath = Path.Combine(oProject.ProjectDirectory, sPath);

                        StringBuilder tmpDir = new StringBuilder();
                        Utilities.PathCanonicalize(tmpDir, sPath);

                        String oTmpDir = Path.GetDirectoryName(tmpDir.ToString());
                        if (!tmp.ContainsKey(oTmpDir))
                        {
                            tmp.Add(oTmpDir, oTmpDir);
                        }
                    }
                }
                List<String> retVal = new List<String>(tmp.Values);
                retVal.Sort();
                return retVal;
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to parse additional include folders. Reason: " + ex.Message, ex);
            }
            return sArr;
        }

        public static Boolean SaveFile(Project oProject, ProjectItem oFile)
        {
            return IterateAndSaveItems(oProject.ProjectItems, oFile);
        }

        public static Boolean IsThirdPartyFile(String sPath, VCConfiguration oProjConfig)
        {
            try
            {
                StringBuilder dummy = new StringBuilder();
                StringBuilder dummy1 = new StringBuilder();
                StringBuilder dummy2 = new StringBuilder();
                StringBuilder dummy3 = new StringBuilder();
                StringBuilder dummy4 = new StringBuilder();
                DirectoryInfo oPathTo3rdParties = new DirectoryInfo(@"D:\ais_test_org_freetext_EWZ\AC_SOURCE\3rdparty\");
                //use DTE2.Properties("Projects and Solutions", "VC++ Directories")
                DirectoryInfo oPathToVS1 = new DirectoryInfo(oProjConfig.Evaluate(@"$(VCInstallDir)include\"));
                DirectoryInfo oPathToVS2 = new DirectoryInfo(oProjConfig.Evaluate(@"$(VCInstallDir)atlmfc\include\"));
                DirectoryInfo oPathToVS3 = new DirectoryInfo(oProjConfig.Evaluate(@"$(VCInstallDir)PlatformSDK\include\"));
                DirectoryInfo oPathToVS4 = new DirectoryInfo(oProjConfig.Evaluate(@"$(FrameworkSDKDir)include\"));
                bool bRetVal = false;
                bRetVal |= (PathCommonPrefix(sPath, oPathTo3rdParties.FullName, dummy) != 0);
                bRetVal |= (PathCommonPrefix(sPath, oPathToVS1.FullName, dummy1) != 0);
                bRetVal |= (PathCommonPrefix(sPath, oPathToVS2.FullName, dummy2) != 0);
                bRetVal |= (PathCommonPrefix(sPath, oPathToVS3.FullName, dummy3) != 0);
                bRetVal |= (PathCommonPrefix(sPath, oPathToVS4.FullName, dummy4) != 0);
                return bRetVal;
            }
            catch (Exception ex)
            {
                throw new Exception("Determining if file belongs to third party is failed. Reason: " + ex.Message);
                return false;
            }
            return false;
        }

        public static Boolean RetrieveIncludes(VCFile oFile, ref SortedDictionary<IncludesKey, VCCodeInclude> oIncludes)
        {
            try
            {
                Boolean bRetVal = false;
                if (oFile.Extension != ".cpp" &&
                    oFile.Extension != ".h" &&
                    oFile.Extension != ".c" &&
                    oFile.Extension != ".hpp")
                    return bRetVal;

                String sIncludePattern = ("\\.*#.*include.*(\\<|\\\")(?'FileName'.+)(\\>|\\\")");
                try
                {
                    ProjectItem oPI = ((ProjectItem)oFile.Object);
                    VCFileCodeModel oFCM = (VCFileCodeModel)oPI.FileCodeModel;
                    if (oFCM == null)
                    {
                        throw new Exception("Cannot get FilecodeModel for file " + oFile.FullPath);
                    }
                    if (oFCM.Includes == null)
                    {
                        return false;
                    }
                    foreach (VCCodeInclude oCI in oFCM.Includes)
                    {
                        TextPoint oStartPoint = oCI.StartPoint;
                        EditPoint oEditPoint = oStartPoint.CreateEditPoint();
                        String sTmpInclude = oEditPoint.GetText(oCI.EndPoint);
                        Match match = Regex.Match(sTmpInclude, sIncludePattern);
                        String sIncFullName;
                        if (match.Success)
                        {
                            sIncFullName = match.Groups["FileName"].Value;
                            oIncludes.Add(new IncludesKey(sIncFullName), oCI);
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Failed to parse file: " + oFile.FullPath + ".Reason: " + ex.Message, ex);
                }
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to retrieve includes from file '" + oFile.Name + "'. Reason: " + ex.Message, ex);
            }
        }

        private static Boolean IterateAndSaveItems(ProjectItems oItems, ProjectItem oFile)
        {
            foreach (ProjectItem item in oItems)
            {
                if (item.Name == oFile.Name)
                {
                    item.Save("");
                    return true;
                }
                if (item.ProjectItems != null)
                {
                    if (IterateAndSaveItems(item.ProjectItems, oFile))
                        return true;
                }
            }
            return false;
        }

        #region Shlwapi functions used internally
        [DllImport("shlwapi.dll")]
        public static extern bool PathRelativePathTo(
             [Out] StringBuilder pszPath,
             [In] string pszFrom,
             [In] uint dwAttrFrom,
             [In] string pszTo,
             [In] uint dwAttrTo
        );

        [DllImport("shlwapi.dll")]
        public static extern Int32 PathCommonPrefix(
                         [In] string pszFile1,
                         [In] string pszFile2,
                         [Out] StringBuilder pszPath
                    );

        [DllImport("shlwapi.dll")]
        public static extern bool PathCanonicalize(
            [Out] StringBuilder dst, 
            [In] string src
            );
        #endregion
    }
    
    public class IncludesKey: IComparable
    {
        public String sInclude;
        public Guid IncludeGUID;
        public IncludesKey()
        {
            sInclude = "";
            IncludeGUID = Guid.NewGuid();
        }

        public IncludesKey(String sIncludeStr)
        {
            sInclude = sIncludeStr;
            IncludeGUID = Guid.NewGuid();
        }

        public int CompareTo(object obj)
        {
            IncludeComparer comparer = new IncludeComparer();
            IncludesKey rhs = obj as IncludesKey;
            if (rhs != null)
            {
                return comparer.Compare(this, rhs);
            }
            else
            {
                return 1;
            }
        }

    }

    public class IncludeComparer : IComparer<IncludesKey>
    {
        public int Compare(IncludesKey lhs, IncludesKey rhs)
        {
            StringComparer invICCmp = StringComparer.InvariantCultureIgnoreCase;
            if (rhs.sInclude.ToLowerInvariant().Contains("stdafx.h") && lhs.sInclude.ToLowerInvariant().Contains("stdafx.h"))
            {
                return invICCmp.Compare(lhs.IncludeGUID.ToString(), rhs.IncludeGUID.ToString());
            }
            else if (lhs.sInclude.ToLowerInvariant().Contains("stdafx.h"))
            {
                return -1;
            }
            else if (rhs.sInclude.ToLowerInvariant().Contains("stdafx.h"))
            {
                return invICCmp.Compare(lhs.IncludeGUID.ToString(), rhs.IncludeGUID.ToString());
            }
            else if (invICCmp.Compare(lhs.sInclude, rhs.sInclude) == 0)
            {
                return invICCmp.Compare(lhs.IncludeGUID.ToString(), rhs.IncludeGUID.ToString());
            }
            else
            {
                return invICCmp.Compare(lhs.sInclude, rhs.sInclude);
            }
        }
    }
}
