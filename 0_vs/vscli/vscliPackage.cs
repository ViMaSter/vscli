using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using Microsoft.Win32;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.VCProjectEngine;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;

using System.Collections;
using System.Collections.Generic;

// Required for menu
using EnvDTE;
using EnvDTE80;

// Required for config
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;


namespace BohemiaInteractive.vscli
{
#pragma region Create menu
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the information needed to show this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(GuidList.guidvscliPkgString)]
    [ProvideOptionPage(typeof(OptionPageGrid),
    "SetArgs", "Available arguments", 0, 0, true)]
    public sealed class vscliPackage : Package
    {
        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public vscliPackage()
        {
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));
        }

        public Argument[] arguments
        {
            get
            {
                OptionPageGrid page = (OptionPageGrid)GetDialogPage(typeof(OptionPageGrid));
                return page.Arguments;
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Overridden Package Implementation
        #region Package Members

        private void RebuildMenu()
        {
            Debug.WriteLine("!!!!! Rebuild!");
        }

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            Debug.WriteLine (string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
            base.Initialize();
            OptionPageGrid page = (OptionPageGrid)GetDialogPage(typeof(OptionPageGrid));
            page.RebuildAction = this.RebuildMenu;

            // Add our command handlers for menu (commands must exist in the .vsct file)
            RebuildMenu();
            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if ( null != mcs )
            {
                // Create the command for the menu item.
                CommandID menuCommandID = new CommandID(GuidList.guidvscliCmdSet, (int)PkgCmdIDList.MyMenuGroup);
                MenuCommand menuItem = new MenuCommand(MenuItemCallback, menuCommandID);
                mcs.AddCommand( menuItem );
            }
            OptionPageGrid settings = GetDialogPage(typeof(OptionPageGrid)) as OptionPageGrid;
        }
        #endregion

        /// <summary>
        /// This function is the callback used to execute a command when the a menu item is clicked.
        /// See the Initialize method to see how the menu item is associated to this function using
        /// the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void MenuItemCallback(object sender, EventArgs e)
        {
            SetActiveCommandLineArguments("-window -x=1680 -y=1050 -nonavmesh -noPause  -nosplash -debug_steamapi -hidedebugger -scriptDebug \"-init=playMission ['', '\\C:\\Users\\mahnkevin\\Documents\\DayZ\\missions\\animal.SampleMap']\\\"");
        }

        private void SetActiveCommandLineArguments(string newArguments)
        {
            // grab startup project
            DTE2 dte2 = Package.GetGlobalService(typeof(DTE)) as DTE2;

            Property prop = dte2.Solution.Properties.Item("StartupProject");
            Project startupP = null;
            VCProject startupVCP = null;
            foreach (Project p in dte2.Solution.Projects)
            {
                VCProject vcPrj = (VCProject)p.Object;
                if (vcPrj == null)
                {
                    continue;
                }

                if (p.Name == (string)prop.Value)
                {
                    startupP = p;
                    startupVCP = vcPrj;
                    Debug.WriteLine(p.Name);
                }
            }


            Debug.WriteLine(startupP.Name);

            // grab debug config
            foreach (VCConfiguration vc in startupVCP.Configurations)
            {
                if (startupP.ConfigurationManager.ActiveConfiguration.ConfigurationName == vc.ConfigurationName)
                {
                    VCPlatform p = vc.Platform;
                    if (startupP.ConfigurationManager.ActiveConfiguration.PlatformName == p.Name)
                    {
                        VCDebugSettings ds = vc.DebugSettings;

                        Debug.WriteLine("---");

                        Debug.WriteLine("Old content:");
                        Debug.WriteLine(ds.CommandArguments);

                        ds.CommandArguments = newArguments;

                        Debug.WriteLine("New content:");
                        Debug.WriteLine(ds.CommandArguments);

                        Debug.WriteLine("Success!");
                        break;
                    }
                }
            }
        }

    }
#pragma endregion

#pragma region Create config
    public class Argument
    {
        [Display(Order = 0)]
        private string name;
        [Display(Order = 1)]
        private string command;

        [Display(Order = 2)]
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        [Display(Order = 3)]
        public string Command
        {
            get { return command; }
            set { command = value; }
        }
    }

    public class OptionPageGrid : DialogPage
    {
        private Argument[] arguments = { };

        [Category("SetArgs")]
        [DisplayName("Argument list")]
        [Description("List of possible commandline arguments - each will generate an entry inside the SetArgs-menu")]
        public Argument[] Arguments
        {
            get { return arguments; }
            set { arguments = value; }
        }

        private Action rebuildAction;
        public Action RebuildAction
        {
            set { rebuildAction = value; }
        }

        private void RebuildMenu()
        {
            if (rebuildAction != null)
            {
                rebuildAction();
            }
        }

        protected override void OnApply(PageApplyEventArgs e)
        {
            RebuildMenu();
        }

        protected override void OnClosed(EventArgs e)
        {
            RebuildMenu();
        }
    }

#pragma endregion
}
