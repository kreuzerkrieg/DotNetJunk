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
using EnvDTE80;

namespace CPPHelpers
{
    public static class Utilities
    {
        public static Boolean RebuildCurrentConfiguration(VCProject oProject)
        {
            Boolean bRetVal = false;
            VCConfiguration oCurConfig = GetCurrentConfiguration(oProject);
            oCurConfig.Rebuild();
            return bRetVal;
        }

        public static Boolean CleanCurrentConfiguration(VCProject oProject)
        {
            Boolean bRetVal = false;
            VCConfiguration oCurConfig = GetCurrentConfiguration(oProject);
            oCurConfig.Clean();
            return bRetVal;
        }

        public static Boolean BuildCurrentConfiguration(VCProject oProject)
        {
            Boolean bRetVal = false;
            VCConfiguration oCurConfig = GetCurrentConfiguration(oProject);
            oCurConfig.Build();
            return bRetVal;
        }

        public static Boolean LinkCurrentConfiguration(VCProject oProject)
        {
            Boolean bRetVal = false;
            VCConfiguration oCurConfig = GetCurrentConfiguration(oProject);
            oCurConfig.Relink();
            return bRetVal;
        }

        public static Boolean CompileFile(VCFile oFile)
        {
            Boolean bRetVal = false;
            try
            {
                DTE2 oApp = (DTE2)((((Project)((VCProject)oFile.project).Object)).DTE);
                OutputWindow oOutputWin = (OutputWindow)oApp.ToolWindows.OutputWindow;
                OutputWindowPane oPane = oOutputWin.OutputWindowPanes.Item("Build");
                oOutputWin.Parent.Activate();
                oPane.Activate();
                oPane.Clear();
                VCFileConfiguration oCurConfig = GetCurrentFileConfiguration(oFile);
                oCurConfig.Compile(true, true);
                TextDocument oTD = oPane.TextDocument;
                EditPoint oOutEP = oTD.CreateEditPoint(oTD.StartPoint);
                oTD.Selection.SelectAll();
                bRetVal = oTD.Selection.Text.Contains(" 0 error");
            }
            catch (Exception ex)
            {
                return bRetVal;
            }
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

                        String oTmpDir = Utilities.PathCanonicalize(sPath);

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
             bool bRetVal = false;
             try
             {
                 DirectoryInfo oPathTo3rdParties = new DirectoryInfo(@"f:\Development\AC_SERVER_4_8_1\3rdParty\");
                 //use DTE2.Properties("Projects and Solutions", "VC++ Directories")
                 DirectoryInfo oPathToVS1 = new DirectoryInfo(oProjConfig.Evaluate(@"$(VCInstallDir)include\"));
                 DirectoryInfo oPathToVS2 = new DirectoryInfo(oProjConfig.Evaluate(@"$(VCInstallDir)atlmfc\include\"));
                 DirectoryInfo oPathToVS3 = new DirectoryInfo(oProjConfig.Evaluate(@"$(VCInstallDir)PlatformSDK\include\"));
                 DirectoryInfo oPathToVS4 = new DirectoryInfo(oProjConfig.Evaluate(@"$(FrameworkSDKDir)include\"));
                 StringComparer invICCmp = StringComparer.InvariantCultureIgnoreCase;

                 String dummy = PathCommonPrefix(sPath, oPathTo3rdParties.FullName);
                 String dummy1 = PathCommonPrefix(sPath, oPathToVS1.FullName);
                 String dummy2 = PathCommonPrefix(sPath, oPathToVS2.FullName);
                 String dummy3 = PathCommonPrefix(sPath, oPathToVS3.FullName);
                 String dummy4 = PathCommonPrefix(sPath, oPathToVS4.FullName);

                 bRetVal |= (invICCmp.Compare(dummy, PathCanonicalize(oPathTo3rdParties.FullName)) == 0);
                 bRetVal |= (invICCmp.Compare(dummy1, PathCanonicalize(oPathToVS1.FullName)) == 0);
                 bRetVal |= (invICCmp.Compare(dummy2, PathCanonicalize(oPathToVS2.FullName)) == 0);
                 bRetVal |= (invICCmp.Compare(dummy3, PathCanonicalize(oPathToVS3.FullName)) == 0);
                 bRetVal |= (invICCmp.Compare(dummy4, PathCanonicalize(oPathToVS4.FullName)) == 0);
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

        public static String PathCommonPrefix(String pszFile1, String pszFile2)
        {
            StringBuilder dummy = new StringBuilder();
            SHLWAPI.PathCommonPrefix(PathCanonicalize(pszFile1), PathCanonicalize(pszFile2), dummy);
            return PathCanonicalize(dummy.ToString());
        }

        public static String PathCanonicalize(String pszFile1)
        {
            StringBuilder dummy = new StringBuilder();
            SHLWAPI.PathCanonicalize(dummy, pszFile1 + Path.DirectorySeparatorChar.ToString());

            return Path.GetDirectoryName(dummy.ToString());
        }

        public static Boolean IsFolder(String path)
        {
            return (File.GetAttributes(path) & FileAttributes.Directory) == FileAttributes.Directory;
        }
    }
    public class IncludesKey : IComparable
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
                return 1;
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

    public static class SHLWAPI
    {
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
}
