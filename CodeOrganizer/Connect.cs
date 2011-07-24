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
//using System.Threading.Tasks;
using System.Collections.Generic;

namespace CodeOrganizer
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
                CommandBarPopup ReferencePopUp = (CommandBarPopup)SEPopUps.Controls["Reference Root"];
                CommandBarPopup ItemPopUp = (CommandBarPopup)SEPopUps.Controls["Item"];

                //This try/catch block can be duplicated if you wish to add multiple commands to be handled by your Add-in,
                //  just make sure you also update the QueryStatus/Exec method to include the new command names.
                CommandBarPopup oSolutionPopup = (CommandBarPopup)SolutionPopUp.Controls.Add(MsoControlType.msoControlPopup,
  System.Reflection.Missing.Value,
  System.Reflection.Missing.Value, 1, true);
                CommandBarPopup oProjectPopup = (CommandBarPopup)ProjectPopUp.Controls.Add(MsoControlType.msoControlPopup,
 System.Reflection.Missing.Value,
 System.Reflection.Missing.Value, 1, true);
                CommandBarPopup oItemPopup = (CommandBarPopup)ItemPopUp.Controls.Add(MsoControlType.msoControlPopup,
 System.Reflection.Missing.Value,
 System.Reflection.Missing.Value, 1, true);
                // Set the caption of the menuitem
                oSolutionPopup.Caption = "C++ Helpers";
                oProjectPopup.Caption = "C++ Helpers";
                oItemPopup.Caption = "C++ Helpers";
                try
                {
                    //Add a command to the Commands collection:
                    Command command = commands.AddNamedCommand2(_addInInstance, "CodeOrganizer", "CodeOrganizer", "Executes the command for CodeOrganizer", true, 59, ref contextGUIDS, (int)vsCommandStatus.vsCommandStatusSupported + (int)vsCommandStatus.vsCommandStatusEnabled, (int)vsCommandStyle.vsCommandStylePictAndText, vsCommandControlType.vsCommandControlTypeButton);
                    Command libOrg = commands.AddNamedCommand2(_addInInstance, "LibOrganize", "Organize Additional Import Libraries", "Sort and Remove Unused Libraries", true, 59, ref contextGUIDS, (int)vsCommandStatus.vsCommandStatusSupported + (int)vsCommandStatus.vsCommandStatusEnabled, (int)vsCommandStyle.vsCommandStylePictAndText, vsCommandControlType.vsCommandControlTypeButton);
                    Command includesOrg = commands.AddNamedCommand2(_addInInstance, "OrganizeIncludes", "Sort and Group Includes", "Sort and Group Include Files", true, 59, ref contextGUIDS, (int)vsCommandStatus.vsCommandStatusSupported + (int)vsCommandStatus.vsCommandStatusEnabled, (int)vsCommandStyle.vsCommandStylePictAndText, vsCommandControlType.vsCommandControlTypeButton);
                    Command includesCanon = commands.AddNamedCommand2(_addInInstance, "CanonicalizeIncludes", "Canonicalize Includes", "Canonicalize Includes", true, 59, ref contextGUIDS, (int)vsCommandStatus.vsCommandStatusSupported + (int)vsCommandStatus.vsCommandStatusEnabled, (int)vsCommandStyle.vsCommandStylePictAndText, vsCommandControlType.vsCommandControlTypeButton);
                    Command PCHing = commands.AddNamedCommand2(_addInInstance, "PCHing", "Move thirs parties to PCH", "Move thirs parties to PCH", true, 59, ref contextGUIDS, (int)vsCommandStatus.vsCommandStatusSupported + (int)vsCommandStatus.vsCommandStatusEnabled, (int)vsCommandStyle.vsCommandStylePictAndText, vsCommandControlType.vsCommandControlTypeButton);
                    Command removeIncludes = commands.AddNamedCommand2(_addInInstance, "RemoveIncludes", "Remove Unused Includes", "Remove Unused Include Files", true, 59, ref contextGUIDS, (int)vsCommandStatus.vsCommandStatusSupported + (int)vsCommandStatus.vsCommandStatusEnabled, (int)vsCommandStyle.vsCommandStylePictAndText, vsCommandControlType.vsCommandControlTypeButton);
                    Command addPCH = commands.AddNamedCommand2(_addInInstance, "AddPCH", "Add Precompiled Headers to the Project", "Add Precompiled Headers to the Project", true, 59, ref contextGUIDS, (int)vsCommandStatus.vsCommandStatusSupported + (int)vsCommandStatus.vsCommandStatusEnabled, (int)vsCommandStyle.vsCommandStylePictAndText, vsCommandControlType.vsCommandControlTypeButton);
                    //Add a control for the command to the tools menu:
                    //if ((command != null) && (toolsPopup != null))
                    //{
                    //    command.AddControl(toolsPopup.CommandBar, 1);
                    //    command.AddControl(ReferencePopUp.CommandBar, 1);
                    //}

                    if (addPCH != null)
                    {
                        addPCH.AddControl(oProjectPopup.CommandBar, 1);
                        addPCH.AddControl(oSolutionPopup.CommandBar, 1);
                    }

                    if (libOrg != null)
                    {
                        libOrg.AddControl(oProjectPopup.CommandBar, 1);
                        libOrg.AddControl(oSolutionPopup.CommandBar, 1);
                    }

                    if (includesOrg != null)
                    {
                        includesOrg.AddControl(oItemPopup.CommandBar, 1);
                        includesOrg.AddControl(oProjectPopup.CommandBar, 1);
                        includesOrg.AddControl(oSolutionPopup.CommandBar, 1);
                    }
                    if (includesCanon != null)
                    {
                        includesCanon.AddControl(oItemPopup.CommandBar, 1);
                        includesCanon.AddControl(oProjectPopup.CommandBar, 1);
                        includesCanon.AddControl(oSolutionPopup.CommandBar, 1);
                    }
                    if (PCHing != null)
                    {
                        PCHing.AddControl(oItemPopup.CommandBar, 1);
                        PCHing.AddControl(oProjectPopup.CommandBar, 1);
                        PCHing.AddControl(oSolutionPopup.CommandBar, 1);
                    }
                    if (removeIncludes != null)
                    {
                        removeIncludes.AddControl(oItemPopup.CommandBar, 1);
                        removeIncludes.AddControl(oProjectPopup.CommandBar, 1);
                        removeIncludes.AddControl(oSolutionPopup.CommandBar, 1);
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
                if (commandName == "CodeOrganizer.Connect.AddPCH")
                {
                    status = (vsCommandStatus)vsCommandStatus.vsCommandStatusSupported | vsCommandStatus.vsCommandStatusEnabled;
                    return;
                }
                else if (commandName == "CodeOrganizer.Connect.CodeOrganizer")
                {
                    status = (vsCommandStatus)vsCommandStatus.vsCommandStatusSupported | vsCommandStatus.vsCommandStatusEnabled;
                    return;
                }
                else if (commandName == "CodeOrganizer.Connect.LibOrganize")
                {
                    status = (vsCommandStatus)vsCommandStatus.vsCommandStatusSupported | vsCommandStatus.vsCommandStatusEnabled;
                    return;
                }
                else if (commandName == "CodeOrganizer.Connect.OrganizeIncludes")
                {
                    status = (vsCommandStatus)vsCommandStatus.vsCommandStatusSupported | vsCommandStatus.vsCommandStatusEnabled;
                    return;
                }
                else if (commandName == "CodeOrganizer.Connect.RemoveIncludes")
                {
                    status = (vsCommandStatus)vsCommandStatus.vsCommandStatusSupported | vsCommandStatus.vsCommandStatusEnabled;
                    return;
                }
                else if (commandName == "CodeOrganizer.Connect.CanonicalizeIncludes")
                {
                    status = (vsCommandStatus)vsCommandStatus.vsCommandStatusSupported | vsCommandStatus.vsCommandStatusEnabled;
                    return;
                }
                else if (commandName == "CodeOrganizer.Connect.PCHing")
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
                if (commandName == "CodeOrganizer.Connect.CodeOrganizer")
                {
                    handled = true;
                    return;
                }
                else if (commandName == "CodeOrganizer.Connect.OrganizeIncludes")
                {
                    try
                    {
                        mLogger.PrintHeaderMessage("Organizing include derectives");
                        IncludesSorter sorter = new IncludesSorter(mLogger, _applicationObject);
                        SelectedItems selectedItems = _applicationObject.SelectedItems;
                        if (selectedItems != null)
                        {
                            for (int i = 1; i <= selectedItems.Count; i++)
                            {
                                SelectedItem selectedItem = selectedItems.Item(i);
                                List<ProjectItem> oProjectItems = new List<ProjectItem>();
                                GetSelectedProjectItems(selectedItem, ref oProjectItems);
                                foreach (ProjectItem oItem in oProjectItems)
                                {
                                    sorter.SortIncludes((VCFile)oItem.Object);
                                    handled = true;
                                }
                            }
                        }
                        mLogger.PrintHeaderMessage("Finished organizing include derectives");
                        return;
                    }
                    catch (Exception ex)
                    {
                        mLogger.PrintMessage("Failed! Reason: " + ex.Message);
                    }
                }
                else if (commandName == "CodeOrganizer.Connect.CanonicalizeIncludes")
                {
                    try
                    {
                        mLogger.PrintHeaderMessage("Canonicalizing includes");
                        IncludesCanonicalizer canonicalizer = new IncludesCanonicalizer(mLogger, _applicationObject);
                        SelectedItems selectedItems = _applicationObject.SelectedItems;
                        if (selectedItems != null)
                        {
                            for (int i = 1; i <= selectedItems.Count; i++)
                            {
                                SelectedItem selectedItem = selectedItems.Item(i);
                                List<ProjectItem> oProjectItems = new List<ProjectItem>();
                                GetSelectedProjectItems(selectedItem, ref oProjectItems);
                                foreach (ProjectItem oItem in oProjectItems)
                                {
                                    canonicalizer.CanonicalizeIncludes((VCProject)oItem.ContainingProject.Object, (VCFile)oItem.Object);
                                    handled = true;
                                }
                            }
                        }
                        mLogger.PrintHeaderMessage("Finished canonicalizing includes");
                        return;
                    }
                    catch (Exception ex)
                    {
                        mLogger.PrintMessage("Failed! Reason: " + ex.Message);
                    }
                }
                else if (commandName == "CodeOrganizer.Connect.PCHing")
                {
                    try
                    {
                        mLogger.PrintHeaderMessage("Moving headers to precompiled headers");
                        PrecompiledHeadersOrganizer PCHMover = new PrecompiledHeadersOrganizer(mLogger, _applicationObject);
                        SelectedItems selectedItems = _applicationObject.SelectedItems;
                        if (selectedItems != null)
                        {
                            for (int i = 1; i <= selectedItems.Count; i++)
                            {
                                SelectedItem selectedItem = selectedItems.Item(i);
                                List<ProjectItem> oProjectItems = new List<ProjectItem>();
                                GetSelectedProjectItems(selectedItem, ref oProjectItems);
                                foreach (ProjectItem oItem in oProjectItems)
                                {
                                    PCHMover.CleanFiles((VCFile)oItem.Object);
                                    handled = true;
                                }
                            }
                        }
                        mLogger.PrintHeaderMessage("Finished moving headers to precompiled headers");
                        return;
                    }
                    catch (Exception ex)
                    {
                        mLogger.PrintMessage("Failed! Reason: " + ex.Message);
                    }
                }
                else if (commandName == "CodeOrganizer.Connect.RemoveIncludes")
                {
                    try
                    {
                        mLogger.PrintHeaderMessage("Removing unnessesary headers");
                        IncludesRemover oRemover = new IncludesRemover(mLogger, _applicationObject);
                        SelectedItems selectedItems = _applicationObject.SelectedItems;
                        if (selectedItems != null)
                        {
                            for (int i = 1; i <= selectedItems.Count; i++)
                            {
                                SelectedItem selectedItem = selectedItems.Item(i);
                                List<ProjectItem> oProjectItems = new List<ProjectItem>();
                                GetSelectedProjectItems(selectedItem, ref oProjectItems);
                                foreach (ProjectItem oItem in oProjectItems)
                                {
                                    oRemover.RemoveIncludes((VCFile)oItem.Object);
                                    handled = true;
                                }
                            }
                        }
                        mLogger.PrintHeaderMessage("Finished removing unnessesary headers");
                        return;
                    }
                    catch (Exception ex)
                    {
                        mLogger.PrintMessage("Failed! Reason: " + ex.Message);
                    }
                }
                else if (commandName == "CodeOrganizer.Connect.AddPCH")
                {
                    try
                    {
                        mLogger.PrintHeaderMessage("Adding Precompiled Header to the Project");
                        AddPCHtoProject oPCHer = new AddPCHtoProject(mLogger, _applicationObject);
                        SelectedItems selectedItems = _applicationObject.SelectedItems;
                        if (selectedItems != null)
                        {
                            List<Project> oProjectItems = new List<Project>();
                            GetSelectedProjects(selectedItems, ref oProjectItems);
                            foreach (Project oItem in oProjectItems)
                            {
                                oPCHer.PCHize((VCProject)oItem.Object);
                                handled = true;
                            }
                        }
                        mLogger.PrintHeaderMessage("Finished adding PCH");
                        return;
                    }
                    catch (Exception ex)
                    {
                        mLogger.PrintMessage("Failed! Reason: " + ex.Message);
                    }
                }
            }
        }

        void GetSelectedProjectItems(SelectedItem item, ref List<ProjectItem> oProjectItems)
        {
            if (item.Project != null)
            {
                Project oProject = item.Project;
                foreach (ProjectItem oProjectItem in oProject.ProjectItems)
                {
                    GetProjectItems(oProjectItem, ref oProjectItems);
                }
            }
            else if (item.ProjectItem != null)
            {
                if (!oProjectItems.Contains(item.ProjectItem))
                {
                    oProjectItems.Add(item.ProjectItem);
                }
            }
        }

        void GetProjectItems(ProjectItem item, ref List<ProjectItem> oProjectItems)
        {
            if (item.ProjectItems != null)
            {
                foreach (ProjectItem oProjectItem in item.ProjectItems)
                {
                    GetProjectItems(oProjectItem, ref oProjectItems);
                }
            }
            if (item.FileCodeModel != null &&
                item.FileCodeModel.Language == "{B5E9BD32-6D3E-4B5D-925E-8A43B79820B4}")
            {
                if (!oProjectItems.Contains(item))
                {
                    oProjectItems.Add(item);
                }
            }
        }

        void GetSelectedProjects(SelectedItems items, ref List<Project> oProjectItems)
        {
            UIHierarchy hier = (UIHierarchy)_applicationObject.Windows.Item(Constants.vsWindowKindSolutionExplorer).Object;
            foreach (UIHierarchyItem item in (Array)hier.SelectedItems)
            {
                String TypeName = item.Object.GetType().Name;
                if (TypeName == "__ComObject")
                {
                    Project oProject = (Project)item.Object;
                    if (oProject.Kind == "{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}")
                    {
                            oProjectItems.Add(oProject);
                    }
                }
                else if (TypeName == "SolutionClass")
                {
                    Solution2 oSolution = (Solution2)item.Object;
                    foreach (Project oProj in oSolution.Projects)
                    {
                        if (oProj.Kind == "{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}")
                        {
                            oProjectItems.Add(oProj);
                        }
                    }
                }
            }
        }

        private DTE2 _applicationObject;
        private AddIn _addInInstance;
        private OutputWindowLogger mLogger;
    }
}