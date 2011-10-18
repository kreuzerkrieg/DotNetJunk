using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.VCProjectEngine;
using EnvDTE;
using System.IO;
using EnvDTE80;
using CPPHelpers;

namespace CPPHelper
{
    class ProjectDependencyRebuilder
    {
        public ProjectDependencyRebuilder(Logger logger)
        {
            mLogger = logger;
        }
        public void RebuildDependecies(Solution2 oSolution)
        {
            IDictionary<String, List<String>> OutputData = new Dictionary<String, List<String>>(); 
            IDictionary<String, List<String>> InputData = new Dictionary<String, List<String>>();
            GatherBuildData(oSolution, ref OutputData, ref InputData);
            CleanDependencies(oSolution);
            RebuildDependencies(oSolution, OutputData, InputData);
        }

        public void RebuildDependencies(Solution2 oSolution, IDictionary<String, List<String>> OutputData, IDictionary<String, List<String>> InputData)
        {
            // rebuild project dependencies
            foreach (BuildDependency Dependency in oSolution.SolutionBuild.BuildDependencies)
            {
                List<String> Libs = new List<string>();
                if (InputData.TryGetValue(Dependency.Project.UniqueName, out Libs))
                {
                    foreach (String Lib in Libs)
                    {
                        Boolean LibFound = false;
                        foreach (KeyValuePair<String, List<String>> OutputFiles in OutputData)
                        {
                            if (OutputFiles.Value.Contains(Lib))
                            {
                                try
                                {
                                    Dependency.AddProject(OutputFiles.Key);
                                    LibFound = true;
                                    break;
                                }
                                catch (Exception)
                                {
                                    // most likely we are trying to create circular dependency
                                }
                            }
                        }
                    }
                }
                Utilities.Sleep(10);
            }
        }

        public void CleanDependencies(Solution2 oSolution)
        {
            //just clean everything
            foreach (BuildDependency Dependency in oSolution.SolutionBuild.BuildDependencies)
            {
                if (Dependency.Project.Kind == "{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}")
                {
                    Dependency.RemoveAllProjects();
                }
            }
        }

        public void GatherBuildData(Solution2 oSolution, ref IDictionary<String, List<String>> OutputData, ref IDictionary<String, List<String>> InputData)
        {
            Projects SolutionProjects = oSolution.Projects;

            foreach (Project Proj in SolutionProjects)
            {
                List<VCProject> Projects = Utilities.GetProjects(Proj);
                foreach (VCProject VCProj in Projects)
                {
                    if (VCProj.Kind == "VCProject")
                    {
                        GatherBuildOutputData(VCProj, ref OutputData, ref InputData);
                    }
                }
            }
        }

        private void GatherBuildOutputData(VCProject Project, ref IDictionary<string, List<string>> OutputData, ref IDictionary<string, List<string>> InputData)
        {
            try
            {
                Project Proj = (Project)Project.Object;
                foreach (VCConfiguration Config in (IVCCollection)Project.Configurations)
                {
                    Object Linking = null;
                    if ((VCLinkerTool)((IVCCollection)Config.Tools).Item("VCLinkerTool") != null)
                        Linking = ((IVCCollection)Config.Tools).Item("VCLinkerTool");
                    else if ((VCLibrarianTool)((IVCCollection)Config.Tools).Item("VCLibrarianTool") != null)
                        Linking = ((IVCCollection)Config.Tools).Item("VCLibrarianTool");
                    String Lib = (Linking is VCLinkerTool) ? ((VCLinkerTool)Linking).ImportLibrary : ((VCLibrarianTool)Linking).OutputFile;
                    if (!String.IsNullOrEmpty(Lib))
                    {
                        if (OutputData.ContainsKey(Proj.UniqueName))
                        {
                            List<String> OldList = new List<string>();
                            OutputData.TryGetValue(Proj.UniqueName, out OldList);
                            OldList.Add(Path.GetFileNameWithoutExtension(Config.Evaluate(Lib)).ToUpperInvariant());
                            OutputData.Remove(Proj.UniqueName);
                            OutputData.Add(Proj.UniqueName, OldList);
                        }
                        else
                        {
                            List<String> NewList = new List<String>();
                            NewList.Add(Path.GetFileNameWithoutExtension(Config.Evaluate(Lib)).ToUpperInvariant());
                            OutputData.Add(Proj.UniqueName, NewList);
                        }
                    }
                    String Libs = (Linking is VCLinkerTool)?((VCLinkerTool)Linking).AdditionalDependencies:((VCLibrarianTool)Linking).AdditionalDependencies;
                    if (String.IsNullOrEmpty(Libs))
                        return;
                    List<String> IncLibs = new List<String>(Libs.Split(new char[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries));
                    if (IncLibs == null || IncLibs.Count == 0)
                        return;
                    IncLibs.Sort();
                    List<String> NewIncLibs = new List<string>();
                    for (int i = 0; i < IncLibs.Count; i++)
                    {
                        if (!NewIncLibs.Contains(Path.GetFileNameWithoutExtension(IncLibs[i]).ToUpperInvariant()))
                        {
                            NewIncLibs.Add(Path.GetFileNameWithoutExtension(IncLibs[i]).ToUpperInvariant());
                        }
                    }
                    if (!InputData.ContainsKey(Proj.UniqueName))
                    {
                        InputData.Add(Proj.UniqueName, NewIncLibs);
                    }
                    else
                    {
                        List<String> OldList = new List<string>();
                        InputData.TryGetValue(Proj.UniqueName, out OldList);
                        OldList.AddRange(NewIncLibs);
                        InputData.Remove(Proj.UniqueName);
                        InputData.Add(Proj.UniqueName, OldList);
                    }
                }
            }
            catch (Exception ex)
            {
                mLogger.PrintError("Gathering data for project '" + Project.Name + "' failed. Reason: " + ex.Message);
            }
        }
        private Logger mLogger;
    }
}
