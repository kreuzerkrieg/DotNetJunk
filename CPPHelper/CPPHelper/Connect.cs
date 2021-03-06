using System;
using Extensibility;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.CommandBars;
using System.Resources;
using System.Reflection;
using System.Globalization;
using CPPHelpers;
using Microsoft.VisualStudio.VCProjectEngine;
using System.Collections.Generic;

namespace CPPHelper
{
    /// <summary>The object for implementing an Add-in.</summary>
    /// <seealso class='IDTExtensibility2' />
    public class Connect : IDTExtensibility2, IDTCommandTarget
    {
        /// <summary>Implements the constructor for the Add-in object. Place your initialization code within this method.</summary>
        public Connect()
        {
        }

        /// <summary>Implements the OnConnection method of the IDTExtensibility2 interface. Receives notification that the Add-in is being loaded.</summary>
        /// <param term='application'>Root object of the host application.</param>
        /// <param term='connectMode'>Describes how the Add-in is being loaded.</param>
        /// <param term='addInInst'>Object representing this Add-in.</param>
        /// <seealso class='IDTExtensibility2' />
        public void OnConnection(object application, ext_ConnectMode connectMode, object addInInst, ref Array custom)
        {
            _applicationObject = (DTE2)application;
            _addInInstance = (AddIn)addInInst;
            if (connectMode == ext_ConnectMode.ext_cm_UISetup)
            {
                object[] contextGUIDS = new object[] { };
                Commands2 commands = (Commands2)_applicationObject.Commands;
                string toolsMenuName = "Tools";

                //Place the command on the tools menu.
                //Find the MenuBar command bar, which is the top-level command bar holding all the main menu items:
                Microsoft.VisualStudio.CommandBars.CommandBar menuBarCommandBar = ((Microsoft.VisualStudio.CommandBars.CommandBars)_applicationObject.CommandBars)["MenuBar"];

                //Find the Tools command bar on the MenuBar command bar:
                CommandBarControl toolsControl = menuBarCommandBar.Controls[toolsMenuName];
                CommandBarPopup toolsPopup = (CommandBarPopup)toolsControl;

                CommandBar SECommandBar = ((CommandBars)_applicationObject.CommandBars)["Context Menus"];
                CommandBarPopup SEPopUps = (CommandBarPopup)SECommandBar.Controls["Project and Solution Context Menus"];
                CommandBarPopup ProjectPopUp = (CommandBarPopup)SEPopUps.Controls["Project"];
                CommandBarPopup SolutionPopUp = (CommandBarPopup)SEPopUps.Controls["Solution"];
                CommandBarPopup ItemPopUp = (CommandBarPopup)SEPopUps.Controls["Item"];
                CommandBarPopup MultiselectPopUp = (CommandBarPopup)SEPopUps.Controls["Cross Project Multi Project"];

                //This try/catch block can be duplicated if you wish to add multiple commands to be handled by your Add-in,
                //  just make sure you also update the QueryStatus/Exec method to include the new command names.
                CommandBarPopup oSolutionPopup = (CommandBarPopup)SolutionPopUp.Controls.Add(MsoControlType.msoControlPopup, Missing.Value, Missing.Value, 1, true);
                CommandBarPopup oProjectPopup = (CommandBarPopup)ProjectPopUp.Controls.Add(MsoControlType.msoControlPopup, Missing.Value, Missing.Value, 1, true);
                CommandBarPopup oItemPopup = (CommandBarPopup)ItemPopUp.Controls.Add(MsoControlType.msoControlPopup, Missing.Value, Missing.Value, 1, true);
                CommandBarPopup oMultiProjectPopup = (CommandBarPopup)MultiselectPopUp.Controls.Add(MsoControlType.msoControlPopup, Missing.Value, Missing.Value, 1, true);
                // Set the caption of the menuitem
                oSolutionPopup.Caption = "C++ Helpers";
                oProjectPopup.Caption = "C++ Helpers";
                oItemPopup.Caption = "C++ Helpers";
                oMultiProjectPopup.Caption = "C++ Helpers";
                try
                {
#if RUNNING_ON_FW_4
                    Command includesOrg = commands.AddNamedCommand2(_addInInstance, "OrganizeIncludes", "Sort and Group Includes", "Sort and Group Include Files", false, Resources.Icons.Document, ref contextGUIDS, (int)vsCommandStatus.vsCommandStatusSupported + (int)vsCommandStatus.vsCommandStatusEnabled, (int)vsCommandStyle.vsCommandStylePictAndText, vsCommandControlType.vsCommandControlTypeButton);
                    Command includesCanon = commands.AddNamedCommand2(_addInInstance, "CanonicalizeIncludes", "Canonicalize Includes", "Canonicalize Includes", false, Resources.Icons.Event, ref contextGUIDS, (int)vsCommandStatus.vsCommandStatusSupported + (int)vsCommandStatus.vsCommandStatusEnabled, (int)vsCommandStyle.vsCommandStylePictAndText, vsCommandControlType.vsCommandControlTypeButton);
                    Command PCHing = commands.AddNamedCommand2(_addInInstance, "PCHing", "Move thirs parties to PCH", "Move thirs parties to PCH", false, Resources.Icons.Tools, ref contextGUIDS, (int)vsCommandStatus.vsCommandStatusSupported + (int)vsCommandStatus.vsCommandStatusEnabled, (int)vsCommandStyle.vsCommandStylePictAndText, vsCommandControlType.vsCommandControlTypeButton);
                    Command removeIncludes = commands.AddNamedCommand2(_addInInstance, "RemoveIncludes", "Remove Unused Includes", "Remove Unused Include Files", false, Resources.Icons.X, ref contextGUIDS, (int)vsCommandStatus.vsCommandStatusSupported + (int)vsCommandStatus.vsCommandStatusEnabled, (int)vsCommandStyle.vsCommandStylePictAndText, vsCommandControlType.vsCommandControlTypeButton);
                    Command addPCH = commands.AddNamedCommand2(_addInInstance, "AddPCH", "Add Precompiled Headers to the Project", "Add Precompiled Headers to the Project", false, Resources.Icons.Object, ref contextGUIDS, (int)vsCommandStatus.vsCommandStatusSupported + (int)vsCommandStatus.vsCommandStatusEnabled, (int)vsCommandStyle.vsCommandStylePictAndText, vsCommandControlType.vsCommandControlTypeButton);
                    Command removeLibs = commands.AddNamedCommand2(_addInInstance, "RemoveLibs", "Remove Unnesessary Libraries", "Remove Unnesessary Libraries", false, 4, ref contextGUIDS, (int)vsCommandStatus.vsCommandStatusSupported + (int)vsCommandStatus.vsCommandStatusEnabled, (int)vsCommandStyle.vsCommandStylePictAndText, vsCommandControlType.vsCommandControlTypeButton);
                    Command rebuildDependencies = commands.AddNamedCommand2(_addInInstance, "RebuildDepends", "Rebuild Solution Dependencies", "Rebuild Solution Dependencies", false, 4, ref contextGUIDS, (int)vsCommandStatus.vsCommandStatusSupported + (int)vsCommandStatus.vsCommandStatusEnabled, (int)vsCommandStyle.vsCommandStylePictAndText, vsCommandControlType.vsCommandControlTypeButton);
                    Command formatSource = commands.AddNamedCommand2(_addInInstance, "FromatSource", "Beautify code", "Beautify code", false, 4, ref contextGUIDS, (int)vsCommandStatus.vsCommandStatusSupported + (int)vsCommandStatus.vsCommandStatusEnabled, (int)vsCommandStyle.vsCommandStylePictAndText, vsCommandControlType.vsCommandControlTypeButton);
#else
                    Command includesOrg = commands.AddNamedCommand2(_addInInstance, "OrganizeIncludes", "Sort and Group Includes", "Sort and Group Include Files", false, 1, ref contextGUIDS, (int)vsCommandStatus.vsCommandStatusSupported + (int)vsCommandStatus.vsCommandStatusEnabled, (int)vsCommandStyle.vsCommandStylePictAndText, vsCommandControlType.vsCommandControlTypeButton);
                    Command includesCanon = commands.AddNamedCommand2(_addInInstance, "CanonicalizeIncludes", "Canonicalize Includes", "Canonicalize Includes", false, 2, ref contextGUIDS, (int)vsCommandStatus.vsCommandStatusSupported + (int)vsCommandStatus.vsCommandStatusEnabled, (int)vsCommandStyle.vsCommandStylePictAndText, vsCommandControlType.vsCommandControlTypeButton);
                    Command PCHing = commands.AddNamedCommand2(_addInInstance, "PCHing", "Move thirs parties to PCH", "Move thirs parties to PCH", false, 5, ref contextGUIDS, (int)vsCommandStatus.vsCommandStatusSupported + (int)vsCommandStatus.vsCommandStatusEnabled, (int)vsCommandStyle.vsCommandStylePictAndText, vsCommandControlType.vsCommandControlTypeButton);
                    Command removeIncludes = commands.AddNamedCommand2(_addInInstance, "RemoveIncludes", "Remove Unused Includes", "Remove Unused Include Files", false, 6, ref contextGUIDS, (int)vsCommandStatus.vsCommandStatusSupported + (int)vsCommandStatus.vsCommandStatusEnabled, (int)vsCommandStyle.vsCommandStylePictAndText, vsCommandControlType.vsCommandControlTypeButton);
                    Command addPCH = commands.AddNamedCommand2(_addInInstance, "AddPCH", "Add Precompiled Headers to the Project", "Add Precompiled Headers to the Project", false, 4, ref contextGUIDS, (int)vsCommandStatus.vsCommandStatusSupported + (int)vsCommandStatus.vsCommandStatusEnabled, (int)vsCommandStyle.vsCommandStylePictAndText, vsCommandControlType.vsCommandControlTypeButton);
                    Command removeLibs = commands.AddNamedCommand2(_addInInstance, "RemoveLibs", "Remove Unnesessary Libraries", "Remove Unnesessary Libraries", false, 3, ref contextGUIDS, (int)vsCommandStatus.vsCommandStatusSupported + (int)vsCommandStatus.vsCommandStatusEnabled, (int)vsCommandStyle.vsCommandStylePictAndText, vsCommandControlType.vsCommandControlTypeButton);
                    Command rebuildDependencies = commands.AddNamedCommand2(_addInInstance, "RebuildDepends", "Rebuild Solution Dependencies", "Rebuild Solution Dependencies", false, 4, ref contextGUIDS, (int)vsCommandStatus.vsCommandStatusSupported + (int)vsCommandStatus.vsCommandStatusEnabled, (int)vsCommandStyle.vsCommandStylePictAndText, vsCommandControlType.vsCommandControlTypeButton);
#endif
                    if (addPCH != null)
                    {
                        addPCH.AddControl(oProjectPopup.CommandBar, 1);
                        addPCH.AddControl(oSolutionPopup.CommandBar, 1);
                        addPCH.AddControl(oMultiProjectPopup.CommandBar, 1);
                    }

                    if (includesOrg != null)
                    {
                        includesOrg.AddControl(oItemPopup.CommandBar, 1);
                        includesOrg.AddControl(oProjectPopup.CommandBar, 1);
                        includesOrg.AddControl(oSolutionPopup.CommandBar, 1);
                        includesOrg.AddControl(oMultiProjectPopup.CommandBar, 1);
                    }
                    if (formatSource != null)
                    {
                        formatSource.AddControl(oItemPopup.CommandBar, 1);
                        formatSource.AddControl(oProjectPopup.CommandBar, 1);
                        formatSource.AddControl(oSolutionPopup.CommandBar, 1);
                        formatSource.AddControl(oMultiProjectPopup.CommandBar, 1);
                    }
                    if (includesCanon != null)
                    {
                        includesCanon.AddControl(oItemPopup.CommandBar, 1);
                        includesCanon.AddControl(oProjectPopup.CommandBar, 1);
                        includesCanon.AddControl(oSolutionPopup.CommandBar, 1);
                        includesCanon.AddControl(oMultiProjectPopup.CommandBar, 1);
                    }
                    if (PCHing != null)
                    {
                        PCHing.AddControl(oItemPopup.CommandBar, 1);
                        PCHing.AddControl(oProjectPopup.CommandBar, 1);
                        PCHing.AddControl(oSolutionPopup.CommandBar, 1);
                        PCHing.AddControl(oMultiProjectPopup.CommandBar, 1);
                    }
                    if (removeIncludes != null)
                    {
                        removeIncludes.AddControl(oItemPopup.CommandBar, 1);
                        removeIncludes.AddControl(oProjectPopup.CommandBar, 1);
                        removeIncludes.AddControl(oSolutionPopup.CommandBar, 1);
                        removeIncludes.AddControl(oMultiProjectPopup.CommandBar, 1);
                    }
                    if (removeLibs != null)
                    {
                        removeLibs.AddControl(oProjectPopup.CommandBar, 1);
                        removeLibs.AddControl(oSolutionPopup.CommandBar, 1);
                        removeLibs.AddControl(oMultiProjectPopup.CommandBar, 1);
                    }
                    if (rebuildDependencies != null)
                    {
                        rebuildDependencies.AddControl(oSolutionPopup.CommandBar, 1);
                    }
                }
                catch (System.ArgumentException)
                {
                    //If we are here, then the exception is probably because a command with that name
                    //  already exists. If so there is no need to recreate the command and we can 
                    //  safely ignore the exception.
                }
            }
        }

        /// <summary>Implements the OnDisconnection method of the IDTExtensibility2 interface. Receives notification that the Add-in is being unloaded.</summary>
        /// <param term='disconnectMode'>Describes how the Add-in is being unloaded.</param>
        /// <param term='custom'>Array of parameters that are host application specific.</param>
        /// <seealso class='IDTExtensibility2' />
        public void OnDisconnection(ext_DisconnectMode disconnectMode, ref Array custom)
        {
        }

        /// <summary>Implements the OnAddInsUpdate method of the IDTExtensibility2 interface. Receives notification when the collection of Add-ins has changed.</summary>
        /// <param term='custom'>Array of parameters that are host application specific.</param>
        /// <seealso class='IDTExtensibility2' />		
        public void OnAddInsUpdate(ref Array custom)
        {
        }

        /// <summary>Implements the OnStartupComplete method of the IDTExtensibility2 interface. Receives notification that the host application has completed loading.</summary>
        /// <param term='custom'>Array of parameters that are host application specific.</param>
        /// <seealso class='IDTExtensibility2' />
        public void OnStartupComplete(ref Array custom)
        {
        }

        /// <summary>Implements the OnBeginShutdown method of the IDTExtensibility2 interface. Receives notification that the host application is being unloaded.</summary>
        /// <param term='custom'>Array of parameters that are host application specific.</param>
        /// <seealso class='IDTExtensibility2' />
        public void OnBeginShutdown(ref Array custom)
        {
        }

        /// <summary>Implements the QueryStatus method of the IDTCommandTarget interface. This is called when the command's availability is updated</summary>
        /// <param term='commandName'>The name of the command to determine state for.</param>
        /// <param term='neededText'>Text that is needed for the command.</param>
        /// <param term='status'>The state of the command in the user interface.</param>
        /// <param term='commandText'>Text requested by the neededText parameter.</param>
        /// <seealso class='Exec' />
        public void QueryStatus(string commandName, vsCommandStatusTextWanted neededText, ref vsCommandStatus status, ref object commandText)
        {
            if (neededText == vsCommandStatusTextWanted.vsCommandStatusTextWantedNone)
            {
                if (commandName == "CPPHelper.Connect.AddPCH")
                {
                    status = (vsCommandStatus)vsCommandStatus.vsCommandStatusSupported | vsCommandStatus.vsCommandStatusEnabled;
                    return;
                }
                else if (commandName == "CPPHelper.Connect.CodeOrganizer")
                {
                    status = (vsCommandStatus)vsCommandStatus.vsCommandStatusSupported | vsCommandStatus.vsCommandStatusEnabled;
                    return;
                }
                else if (commandName == "CPPHelper.Connect.OrganizeIncludes")
                {
                    status = (vsCommandStatus)vsCommandStatus.vsCommandStatusSupported | vsCommandStatus.vsCommandStatusEnabled;
                    return;
                }
                else if (commandName == "CPPHelper.Connect.RemoveIncludes")
                {
                    status = (vsCommandStatus)vsCommandStatus.vsCommandStatusSupported | vsCommandStatus.vsCommandStatusEnabled;
                    return;
                }
                else if (commandName == "CPPHelper.Connect.CanonicalizeIncludes")
                {
                    status = (vsCommandStatus)vsCommandStatus.vsCommandStatusSupported | vsCommandStatus.vsCommandStatusEnabled;
                    return;
                }
                else if (commandName == "CPPHelper.Connect.PCHing")
                {
                    status = (vsCommandStatus)vsCommandStatus.vsCommandStatusSupported | vsCommandStatus.vsCommandStatusEnabled;
                    return;
                }
                else if (commandName == "CPPHelper.Connect.RemoveLibs")
                {
                    status = (vsCommandStatus)vsCommandStatus.vsCommandStatusSupported | vsCommandStatus.vsCommandStatusEnabled;
                    return;
                }
                else if (commandName == "CPPHelper.Connect.RebuildDepends")
                {
                    status = (vsCommandStatus)vsCommandStatus.vsCommandStatusSupported | vsCommandStatus.vsCommandStatusEnabled;
                    return;
                }
                else if (commandName == "CPPHelper.Connect.FromatSource")
                {
                    status = (vsCommandStatus)vsCommandStatus.vsCommandStatusSupported | vsCommandStatus.vsCommandStatusEnabled;
                    return;
                }
            }
        }

        /// <summary>Implements the Exec method of the IDTCommandTarget interface. This is called when the command is invoked.</summary>
        /// <param term='commandName'>The name of the command to execute.</param>
        /// <param term='executeOption'>Describes how the command should be run.</param>
        /// <param term='varIn'>Parameters passed from the caller to the command handler.</param>
        /// <param term='varOut'>Parameters passed from the command handler to the caller.</param>
        /// <param term='handled'>Informs the caller if the command was handled or not.</param>
        /// <seealso class='Exec' />
        public void Exec(string commandName, vsCommandExecOption executeOption, ref object varIn, ref object varOut, ref bool handled)
        {
            handled = false;
            mLogger = new OutputWindowLogger(_applicationObject);
            mLogger.OpenLog();
            if (executeOption == vsCommandExecOption.vsCommandExecOptionDoDefault)
            {
                if (commandName == "CPPHelper.Connect.OrganizeIncludes")
                {
                    try
                    {
                        mLogger.PrintHeaderMessage("Organizing include derectives");
                        IncludesSorter sorter = new IncludesSorter(mLogger, _applicationObject);
                        List<ProjectItem> oProjectItems = new List<ProjectItem>();
                        GetSelectedFiles(ref oProjectItems);
                        foreach (ProjectItem oItem in oProjectItems)
                        {
                            sorter.SortIncludes((VCFile)oItem.Object);
                            handled = true;
                        }
                        mLogger.PrintHeaderMessage("Finished organizing include derectives");
                        return;
                    }
                    catch (Exception ex)
                    {
                        mLogger.PrintMessage("Failed! Reason: " + ex.Message);
                    }
                }
                else if (commandName == "CPPHelper.Connect.CanonicalizeIncludes")
                {
                    try
                    {
                        mLogger.PrintHeaderMessage("Canonicalizing includes");
                        IncludesCanonicalizer canonicalizer = new IncludesCanonicalizer(mLogger, _applicationObject);

                        List<ProjectItem> oProjectItems = new List<ProjectItem>();
                        GetSelectedFiles(ref oProjectItems);
                        foreach (ProjectItem oItem in oProjectItems)
                        {
                            canonicalizer.CanonicalizeIncludes((VCProject)oItem.ContainingProject.Object, (VCFile)oItem.Object);
                            handled = true;
                        }
                        mLogger.PrintHeaderMessage("Finished canonicalizing includes");
                        return;
                    }
                    catch (Exception ex)
                    {
                        mLogger.PrintMessage("Failed! Reason: " + ex.Message);
                    }
                }
                else if (commandName == "CPPHelper.Connect.PCHing")
                {
                    try
                    {
                        mLogger.PrintHeaderMessage("Moving headers to precompiled headers");
                        PrecompiledHeadersOrganizer PCHMover = new PrecompiledHeadersOrganizer(mLogger, _applicationObject);
                        List<ProjectItem> oProjectItems = new List<ProjectItem>();
                        GetSelectedFiles(ref oProjectItems);
                        foreach (ProjectItem oItem in oProjectItems)
                        {
                            PCHMover.CleanFiles((VCFile)oItem.Object);
                            handled = true;
                        }
                        mLogger.PrintHeaderMessage("Finished moving headers to precompiled headers");
                        return;
                    }
                    catch (Exception ex)
                    {
                        mLogger.PrintMessage("Failed! Reason: " + ex.Message);
                    }
                }
                else if (commandName == "CPPHelper.Connect.RemoveIncludes")
                {
                    try
                    {
                        mLogger.PrintHeaderMessage("Removing unnessesary headers");
                        IncludesRemover oRemover = new IncludesRemover(mLogger, _applicationObject);

                        List<ProjectItem> oProjectItems = new List<ProjectItem>();
                        GetSelectedFiles(ref oProjectItems);
                        foreach (ProjectItem oItem in oProjectItems)
                        {
                            oRemover.RemoveIncludes((VCFile)oItem.Object);
                            handled = true;
                        }
                        mLogger.PrintHeaderMessage("Finished removing unnessesary headers");
                        return;
                    }
                    catch (Exception ex)
                    {
                        mLogger.PrintMessage("Failed! Reason: " + ex.Message);
                    }
                }
                else if (commandName == "CPPHelper.Connect.AddPCH")
                {
                    try
                    {
                        mLogger.PrintHeaderMessage("Adding Precompiled Header to the Project");
                        AddPCHtoProject oPCHer = new AddPCHtoProject(mLogger);
                        List<Project> oProjectItems = new List<Project>();
                        GetSelectedProjects(ref oProjectItems);
                        foreach (Project oItem in oProjectItems)
                        {
                            VCProject oProject = (VCProject)oItem.Object;
                            oPCHer.PCHize(oProject);
                            handled = true;
                        }
                        mLogger.PrintHeaderMessage("Finished adding PCH");
                        return;
                    }
                    catch (Exception ex)
                    {
                        mLogger.PrintMessage("Failed! Reason: " + ex.Message);
                    }
                }
                else if (commandName == "CPPHelper.Connect.RemoveLibs")
                {
                    try
                    {
                        mLogger.PrintHeaderMessage("Removing unnesessary dependency libraries");
                        LinkCleaner LibCleaner = new LinkCleaner(mLogger);
                        List<Project> oProjectItems = new List<Project>();
                        GetSelectedProjects(ref oProjectItems);
                        foreach (Project oItem in oProjectItems)
                        {
                            VCProject oProject = (VCProject)oItem.Object;
                            LibCleaner.RemoveUnnesessaryLibraries(oProject);
                            handled = true;
                        }
                        mLogger.PrintHeaderMessage("Finished removing unnesessary dependency libraries");
                        return;
                    }
                    catch (Exception ex)
                    {
                        mLogger.PrintMessage("Failed! Reason: " + ex.Message);
                    }
                }
                else if (commandName == "CPPHelper.Connect.RebuildDepends")
                {
                    try
                    {
                        mLogger.PrintHeaderMessage("Rebuilding project dependencies");
                        ProjectDependencyRebuilder RebuildingDep = new ProjectDependencyRebuilder(mLogger);
                        RebuildingDep.RebuildDependecies(GetSelectedSolution());
                        handled = true;
                        mLogger.PrintHeaderMessage("Finished rebuilding project dependencies");
                        return;
                    }
                    catch (Exception ex)
                    {
                        mLogger.PrintMessage("Failed! Reason: " + ex.Message);
                    }
                }
                else if (commandName == "CPPHelper.Connect.FromatSource")
                {
                    try
                    {
                        mLogger.PrintHeaderMessage("Beautifying source code");
                        CodeBeautifier Beautifier = new CodeBeautifier(mLogger);
                         List<ProjectItem> oProjectItems = new List<ProjectItem>();
                        GetSelectedFiles(ref oProjectItems);
                        foreach (ProjectItem oItem in oProjectItems)
                        {
                            Beautifier.Beautify(oItem);
                        }
                        handled = true;
                        mLogger.PrintHeaderMessage("Finished beautifying source code");
                        return;
                    }
                    catch (Exception ex)
                    {
                        mLogger.PrintMessage("Failed! Reason: " + ex.Message);
                    }
                }
            }
        }

        void GetSelectedFiles(ref List<ProjectItem> oProjectItems)
        {
            UIHierarchy hier = (UIHierarchy)_applicationObject.Windows.Item(Constants.vsWindowKindSolutionExplorer).Object;
            foreach (UIHierarchyItem item in (Array)hier.SelectedItems)
            {
                if (item.Object is Project)
                {
                    Project oProject = (Project)item.Object;
                    AddProjectItemsFromProject(oProject, ref oProjectItems);
                }
                else if (item.Object is Solution2)
                {
                    Solution2 oSolution = (Solution2)item.Object;
                    foreach (Project oProj in oSolution.Projects)
                    {
                        if (oProj.Kind == ProjectKinds.vsProjectKindSolutionFolder)
                        {
                            foreach (ProjectItem Item in oProj.ProjectItems)
                            {
                                if (Item.Object is Project)
                                {
                                    AddProjectItemsFromProject((Project)Item.Object, ref oProjectItems);
                                }
                            }

                        }
                        else 
                        {
                            AddProjectItemsFromProject(oProj, ref oProjectItems);
                        }
                    }
                }
                else if (item.Object is ProjectItem)
                {
                    oProjectItems.Add((ProjectItem)item.Object);
                }
                Utilities.Sleep(10);
            }
        }

        private void AddProjectItemsFromProject(Project oProject, ref List<ProjectItem> oProjectItems)
        {
            if (oProject.Kind == "{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}")
            {
                foreach (ProjectItem oItem in oProject.ProjectItems)
                {
                    if (oItem.FileCodeModel != null &&
                        oItem.FileCodeModel.Language == CodeModelLanguageConstants.vsCMLanguageVC)
                    {
                        oProjectItems.Add(oItem);
                    }
                    else if (oItem.ProjectItems != null)
                    {
                        AddSubprojectItems(oItem.ProjectItems, oProjectItems);
                    }
                }
            }
        }

        private void AddSubprojectItems(ProjectItems projectItems, List<ProjectItem> oProjectItems)
        {
            foreach (ProjectItem oItem in projectItems)
            {
                if (oItem.FileCodeModel != null &&
                                    oItem.FileCodeModel.Language == CodeModelLanguageConstants.vsCMLanguageVC)
                {
                    oProjectItems.Add(oItem);
                }
                else if (oItem.ProjectItems != null)
                {
                    AddSubprojectItems(oItem.ProjectItems, oProjectItems);
                }
                Utilities.Sleep(10);
            }
        }

        void GetSelectedProjects(ref List<Project> oProjectItems)
        {
            UIHierarchy hier = (UIHierarchy)_applicationObject.Windows.Item(Constants.vsWindowKindSolutionExplorer).Object;
            foreach (UIHierarchyItem item in (Array)hier.SelectedItems)
            {
                if (item.Object is Project)
                {
                    Project oProject = (Project)item.Object;
                    if (oProject.Kind == "{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}")
                    {
                        oProjectItems.Add(oProject);
                    }
                }
                else if (item.Object is Solution2)
                {
                    Solution2 oSolution = (Solution2)item.Object;
                    Projects SolutionProjects = oSolution.Projects;

                    foreach (Project Proj in SolutionProjects)
                    {
                        List<VCProject> Projects = Utilities.GetProjects(Proj);
                        oProjectItems.AddRange(Projects.ConvertAll(new Converter<VCProject, Project>(VCProj2Project)));
                    }
                }
                Utilities.Sleep(10);
            }
        }

        private Project VCProj2Project(VCProject proj)
        {
            return (Project)proj.Object;
        }

        Solution2 GetSelectedSolution()
        {
            UIHierarchy hier = (UIHierarchy)_applicationObject.Windows.Item(Constants.vsWindowKindSolutionExplorer).Object;
            foreach (UIHierarchyItem item in (Array)hier.SelectedItems)
            {
                if (item.Object is Solution2)
                {
                    Solution2 oSolution = (Solution2)item.Object;
                    return oSolution;
                }
            }
            return null;
        }

        private DTE2 _applicationObject;
        private AddIn _addInInstance;
        private OutputWindowLogger mLogger;
    }
}