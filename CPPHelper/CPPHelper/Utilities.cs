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
using CPPHelper;
using System.Threading;

namespace CPPHelpers
{
    public static class Utilities
    {
        private static String sIncludePattern = ("\\.*#.*include.*(\\<|\\\")(?'FileName'.+)(\\>|\\\")");


        public static Boolean CompileFile(VCFile oFile)
        {
            return CompileFile(oFile, true);
        }

        public static Boolean CompileFile(VCFile oFile, Boolean rebuild)
        {
            Boolean bRetVal = false;
            try
            {
#if !RUNNING_ON_FW_4
                DTE2 oApp = (DTE2)((((Project)((VCProject)oFile.project).Object)).DTE);
                OutputWindow oOutputWin = (OutputWindow)oApp.ToolWindows.OutputWindow;
                OutputWindowPane oPane = oOutputWin.OutputWindowPanes.Item("Build");
                oOutputWin.Parent.Activate();
                oPane.Activate();
                oPane.Clear();
                Utilities.Sleep(50);
#endif
                VCFileConfiguration oCurConfig = GetCurrentFileConfiguration(oFile);
                oCurConfig.Compile(rebuild, true);
                Utilities.Sleep(50);
#if !RUNNING_ON_FW_4
                TextDocument oTD = oPane.TextDocument;
                EditPoint oOutEP = oTD.CreateEditPoint(oTD.StartPoint);
                oTD.Selection.SelectAll();
                Utilities.Sleep(50);
                bRetVal = oTD.Selection.Text.Contains(" 0 error");
#else
                bRetVal = true;
#endif
            }
            catch (Exception)
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
                List<DirectoryInfo> oDefaultIncludes = GetDefaultIncludePaths(oActiveConfig);

                VCCLCompilerTool oCompilerTool = (VCCLCompilerTool)((IVCCollection)oActiveConfig.Tools).Item("VCCLCompilerTool");
                String sValues = oCompilerTool.AdditionalIncludeDirectories;
                sSplitArr = sValues.Split(new Char[] { ';', ',' });
                IDictionary<String, String> tmp = new Dictionary<String, String>();

                for (int i = 0; i < oDefaultIncludes.Count; i++)
                {
                    if (!tmp.ContainsKey(oDefaultIncludes[i].FullName))
                        tmp.Add(oDefaultIncludes[i].FullName, oDefaultIncludes[i].FullName);
                }

                for (int i = 0; i < sSplitArr.Length; i++)
                {
                    String sPath = oActiveConfig.Evaluate(sSplitArr[i].Trim());

                    if (!String.IsNullOrEmpty(sPath))
                    {
                        sPath = sPath.Replace("\"", "") + "\\";
                        if (!Path.IsPathRooted(sPath))
                            sPath = Path.Combine(oProject.ProjectDirectory, sPath);

                        String oTmpDir = SHLWAPI.PathCanonicalize(sPath);

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
        }

        public static Boolean SaveFile(ProjectItem oFile)
        {
            if (oFile.Document != null)
            {
                return oFile.Document.Save("") == vsSaveStatus.vsSaveSucceeded;
            }
            return false;
        }

        public static Boolean IsThirdPartyFile(String sPath, VCConfiguration oProjConfig)
        {
            bool bRetVal = false;
            try
            {
                List<DirectoryInfo> Includes = GetDefaultIncludePaths(oProjConfig);
                Includes.Add(new DirectoryInfo(@"f:\Development\AC_SERVER_4_8_1\3rdParty\"));
                StringComparer invICCmp = StringComparer.InvariantCultureIgnoreCase;

                List<String> Prefixes = new List<String>(Includes.Count);
                for (int i = 0; i < Includes.Count; i++)
                {
                    Prefixes.Add(SHLWAPI.PathCommonPrefix(sPath, Includes[i].FullName));
                }

                for (int i = 0; i < Includes.Count; i++)
                {
                    bRetVal |= (invICCmp.Compare(Prefixes[i], SHLWAPI.PathCanonicalize(Includes[i].FullName)) == 0);
                }
                
                return bRetVal;
            }
            catch (Exception ex)
            {
                throw new Exception("Determining if file belongs to third party is failed. Reason: " + ex.Message);
            }
            return false;
        }

        private static List<DirectoryInfo> GetDefaultIncludePaths(VCConfiguration oProjConfig)
        {
            List<DirectoryInfo> RetVal = new List<DirectoryInfo>();
            try
            {
                DTE2 oApp = (DTE2)((((Project)((VCProject)oProjConfig.project).Object)).DTE);
                Properties oProps = oApp.get_Properties("Projects", "VCDirectories");
                String oIncludes = (String)oProps.Item("IncludeDirectories").Value;
                String RegExp = @"((?<ConfigName>.*?)\|(?<IncludeFolders>.*?)\|)+";
                Match match = Regex.Match(oIncludes, RegExp, RegexOptions.ExplicitCapture);
                if (match.Success)
                {
                    String tmp = match.Groups["ConfigName"].Value;
                    for (int i = 0; i < match.Groups["ConfigName"].Captures.Count; i++)
                    {
                        if (match.Groups["ConfigName"].Captures[i].Value.ToLowerInvariant() == oProjConfig.Name.Split('|')[1].ToLowerInvariant())
                        {
                            String[] Paths = match.Groups["IncludeFolders"].Captures[i].Value.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                            for (int j = 0; j < Paths.Length; j++)
                            {
                                RetVal.Add(new DirectoryInfo(oProjConfig.Evaluate(Paths[j])));
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                //die in solitude
            }
            return RetVal;
        }
        public static Boolean RetrieveFilesToSkip(VCFile oFile, ref List<String> oIncludes)
        {
            try
            {
                ProjectItem oPI = ((ProjectItem)oFile.Object);
                VCFileCodeModel oFCM = (VCFileCodeModel)oPI.FileCodeModel;
                if (oFCM == null)
                {
                    throw new Exception("Cannot get FilecodeModel for file " + oFile.FullPath);
                }
                foreach (VCCodeElement oCE in oFCM.CodeElements)
                {
                    try
                    {
                        String DeclarationFile = (oCE).get_Location(vsCMWhere.vsCMWhereDeclaration);
                        if (!oIncludes.Contains(DeclarationFile.ToUpperInvariant()))
                        {
                            oIncludes.Add(DeclarationFile.ToUpperInvariant());
                        }
                    }
                    catch (Exception)
                    {
                        //silently die?
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to retrieve declaration files from " + oFile.FullPath + ". Reason:" + ex.Message);
            }
            return true;
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

        internal static List<KeyValuePair<String, String>> GetPossibleIncludeFileLocations(VCCodeInclude oCI)
        {
            List<KeyValuePair<String, String>> oRetVal = new List<KeyValuePair<String, String>>();

            String sIncludeFile = oCI.FullName;
            TextPoint oStartPoint = oCI.StartPoint;
            EditPoint oEditPoint = oStartPoint.CreateEditPoint();
            String sTmpInclude = oEditPoint.GetText(oCI.EndPoint);
            Match match = Regex.Match(sTmpInclude, sIncludePattern);
            String sIncFullName;
            if (match.Success)
            {
                sIncFullName = match.Groups["FileName"].Value;
                VCProject oProject = (VCProject)oCI.Project.Object;
                VCFile oFile = (VCFile)oCI.ProjectItem.Object;
                FileInfo oFI = new FileInfo(Path.Combine(Path.GetDirectoryName(oFile.FullPath), sIncFullName));
                DirectoryInfo oParentFolder = new DirectoryInfo(Path.GetFullPath(oProject.ProjectDirectory)).Parent;
                FileInfo oFIParent = new FileInfo(Path.Combine(oParentFolder.FullName, sIncFullName));
                oRetVal.Add(new KeyValuePair < String, String > (Path.GetDirectoryName(oFile.FullPath), oFI.FullName));
                oRetVal.Add(new KeyValuePair<String, String>(oParentFolder.FullName, oFIParent.FullName));
                List<String> oIncludesArr = GetIncludePaths(oProject);
                foreach (String IncPath in oIncludesArr)
                {
                    oRetVal.Add(new KeyValuePair<String, String>(SHLWAPI.PathCanonicalize(IncPath), SHLWAPI.PathCanonicalize(Path.Combine(IncPath, sIncFullName))));
                }
            }
            return oRetVal;
        }

        internal static Boolean IsLocalFile(VCCodeInclude oCI, ref IncludeStructEx oInc)
        {
            List<KeyValuePair<String, String>> PossibleLocations = GetPossibleIncludeFileLocations(oCI);
            oInc = new IncludeStructEx();
            VCFile oFile = (VCFile)oCI.ProjectItem.Object;
            VCProject oProject = (VCProject)oCI.Project.Object;
            foreach (KeyValuePair<String, String> pair in PossibleLocations)
            {
                FileInfo oFI = new FileInfo(pair.Value);
                if (oFI.Exists)
                {
                    oInc.oInc = oCI;
                    oInc.sFileName = SHLWAPI.PathRelativePathTo_File(pair.Key, oFI.FullName);
                    oInc.sFullPath = oFI.FullName;
                    oInc.sRelativePath = SHLWAPI.PathRelativePathTo_File(pair.Key, oFI.FullName);
                    if (SHLWAPI.PathCommonPrefix(oFI.FullName, oProject.ProjectDirectory) == SHLWAPI.PathCanonicalize(oProject.ProjectDirectory))
                    {
                        oInc.bLocalFile = true;
                    }
                    else
                    {
                        oInc.bLocalFile = false;
                    }
                    return oInc.bLocalFile;
                }
            }
            oInc = null;
            return false;
        }

        internal static Boolean hasPrecompileHeader(VCConfiguration oProjConfig)
        {
            VCCLCompilerTool oCompilerTool = (VCCLCompilerTool)((IVCCollection)oProjConfig.Tools).Item("VCCLCompilerTool");
            return oCompilerTool.UsePrecompiledHeader == pchOption.pchUseUsingSpecific;
        }

        internal static List<VCProject> GetProjects(Project Proj)
        {
            List<VCProject> RetVal = new List<VCProject>();
            if (Proj.Kind == "{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}")
            {
                RetVal.Add((VCProject)Proj.Object);
            }
            else if (Proj.Kind == ProjectKinds.vsProjectKindSolutionFolder)
            {
                foreach (ProjectItem Item in Proj.ProjectItems)
                {
                    if (Item.Object is Project)
                    {
                        List<VCProject> tmp = GetProjects((Project)Item.Object);
                        RetVal.AddRange(tmp);
                    }
                }
            }
            return RetVal;
        }

        internal static VCFile GetFile(VCProject Project, String FileName)
        {
            IVCCollection Items = (IVCCollection)Project.Items;
            return GetFile(Items, FileName);
        }

        internal static VCFile GetFile(IVCCollection Items, String FileName)
        {
            foreach (VCProjectItem Item in Items)
            {
                if (Item.Kind == "VCFile")
                {
                    if (Path.GetFullPath(((VCFile)Item).FullPath).Equals(Path.GetFullPath(FileName), StringComparison.InvariantCultureIgnoreCase))
                        return ((VCFile)Item);
                }
                else if (Item.Kind == "VCFilter")
                {
                    VCFilter Filter = (VCFilter)Item;
                    VCFile File = GetFile((IVCCollection)Filter.Items, FileName);
                    if (File != null)
                        return File;
                }
            }
            return null;
        }

        internal static void Sleep(int Milliseconds)
        {
            DateTime Span = DateTime.Now;
            while ((Span-DateTime.Now).Duration().Milliseconds < Milliseconds)
            {
                System.Windows.Forms.Application.DoEvents();
            }
            System.Windows.Forms.Application.DoEvents();
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

    public class IncludeStructEx
    {
        public VCCodeInclude oInc;
        public String sFileName;
        public String sFullPath;
        public String sRelativePath;
        public Boolean bLocalFile;
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

    public class SHLWAPI
    {
        public const Int32 MAX_PATH = 260;
        #region Shlwapi functions used internally
        [DllImport("shlwapi.dll", CharSet = CharSet.Auto)]
        private static extern bool PathRelativePathTo(
             [Out] StringBuilder pszPath,
             [In] string pszFrom,
             [In] FileAttributes dwAttrFrom,
             [In] string pszTo,
             [In] FileAttributes dwAttrTo
        );

        [DllImport("shlwapi.dll", CharSet = CharSet.Auto)]
        private static extern Int32 PathCommonPrefix(
            [In] String pszFile1,
            [In] String pszFile2,
            [Out] StringBuilder pszPath
            );

        [DllImport("shlwapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool PathCanonicalize(
            [Out] StringBuilder dst, 
            String src);
        #endregion

        public static String PathCommonPrefix(String pszFile1, String pszFile2)
        {
            StringBuilder dummy = new StringBuilder(MAX_PATH);
            PathCommonPrefix(PathCanonicalize(pszFile1), PathCanonicalize(pszFile2), dummy);
            return PathCanonicalize(dummy.ToString());
        }

        public static String PathRelativePathTo_File(String pszFile1, String pszFile2)
        {
            StringBuilder dummy = new StringBuilder(MAX_PATH);
            PathRelativePathTo(dummy, PathCanonicalize(pszFile1), FileAttributes.Directory, PathCanonicalize(pszFile2), FileAttributes.Archive);
            return PathCanonicalize(dummy.ToString());
        }

        public static String PathRelativePathTo_Folder(String pszFile1, String pszFile2)
        {
            StringBuilder dummy = new StringBuilder(MAX_PATH);
            PathRelativePathTo(dummy, PathCanonicalize(pszFile1), FileAttributes.Directory, PathCanonicalize(pszFile2), FileAttributes.Directory);
            return PathCanonicalize(dummy.ToString());
        }

        public static String PathCanonicalize(String pszFile1)
        {
            StringBuilder dummy = new StringBuilder(MAX_PATH);
            String tmp = pszFile1.Replace("/", "\\");
            String RetVal = tmp;
            if (IsFolder(tmp) && !tmp.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                tmp += Path.DirectorySeparatorChar.ToString();
            }
            if (Path.IsPathRooted(tmp))
            {
                tmp = new DirectoryInfo(tmp).FullName;
            }
            SHLWAPI.PathCanonicalize(dummy, tmp);
            RetVal = dummy.ToString();
            return RetVal;
        }

        private static Boolean IsFolder(String path)
        {
            if (File.Exists(path) || Directory.Exists(path))
            {
                return (File.GetAttributes(path) & FileAttributes.Directory) == FileAttributes.Directory;
            }
            else
            {
                return false;
            }
        }
    }
}
