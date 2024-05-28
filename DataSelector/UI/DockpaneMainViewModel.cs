﻿// The Data tools are a suite of ArcGIS Pro addins used to extract
// and manage biodiversity information from ArcGIS Pro and SQL Server
// based on pre-defined or user specified criteria.
//
// Copyright © 2024 Andy Foy Consulting.
//
// This file is part of DataSelector.
//
// DataSelector is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// DataSelector is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with DataSelector.  If not, see <http://www.gnu.org/licenses/>.

using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArcGIS.Desktop.Framework.Controls;
using DataSelector.Properties;
using DataTools;
using System.Windows;
using MessageBox = ArcGIS.Desktop.Framework.Dialogs.MessageBox;
using System.Diagnostics;
using System.Windows.Media;


namespace DataSelector.UI
{
    /// <summary>
    /// Build the DockPane.
    /// </summary>
    internal class DockpaneMainViewModel : DockPane
    {

        #region Fields

        private DockpaneMainViewModel _dockPane;

        private PaneHeader1ViewModel _paneH1VM;
        private PaneHeader2ViewModel _paneH2VM;

        #endregion

        #region ViewModelBase Members

        /// <summary>
        /// Set the global variables.
        /// </summary>
        protected DockpaneMainViewModel()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initialise the DockPane components.
        /// </summary>
        public async void InitializeComponent()
        {
            _dockPane = this;
            _initialised = false;
            _inError = false;

            // Setup the tab controls.
            PrimaryMenuList.Clear();

            PrimaryMenuList.Add(new TabControl() { Text = "Profile", Tooltip = "Select XML profile" });
            PrimaryMenuList.Add(new TabControl() { Text = "Query", Tooltip = "Build SQL query" });

            // Load the default XML profile (or let the user choose a profile.
            _paneH1VM = new PaneHeader1ViewModel(_dockPane);

            // If the profile was in error.
            if (_paneH1VM.XMLError)
            {
                _inError = true;
                return;
            }

            // If the default (and only) profile was loaded.
            if (_paneH1VM.XMLLoaded)
            {
                // Initialise the query pane.
                bool initialised = await InitialiseQueryPaneAsync();
                if (!initialised)
                    return;

                // Select the profile tab.
                SelectedPanelHeaderIndex = 1;
            }
            else
            {
                // Select the query tab.
                SelectedPanelHeaderIndex = 0;
            }

            // Indicate that the dockpane has been initialised.
            _initialised = true;
        }

        /// <summary>
        /// Show the DockPane.
        /// </summary>
        internal static void Show()
        {
            // Get the dockpane DAML id.
            DockPane pane = FrameworkApplication.DockPaneManager.Find(_dockPaneID);
            if (pane == null)
                return;

            // Get the ViewModel by casting the dockpane.
            DockpaneMainViewModel vm = pane as DockpaneMainViewModel;

            // If the ViewModel is uninitialised then initialise it.
            if (!vm.Initialised)
                vm.InitializeComponent();

            // If the ViewModel is in error then don't show the dockpane.
            if (vm.InError)
            {
                pane = null;
                return;
            }

            // Active the dockpane.
            pane.Activate();
        }

        #endregion

        #region Properties

        /// <summary>
        /// ID of the DockPane.
        /// </summary>
        private const string _dockPaneID = "DataSelector_UI_DockpaneMain";

        public static string DockPaneID
        {
            get => _dockPaneID;
        }

        /// <summary>
        /// Override the default behavior when the dockpane's help icon is clicked
        /// or the F1 key is pressed.
        /// </summary>
        protected override void OnHelpRequested()
        {
            if (_helpURL != null)
            {
                System.Diagnostics.Process.Start(new ProcessStartInfo
                {
                    FileName = _helpURL,
                    UseShellExecute = true
                });
            }
        }

        private List<TabControl> _primaryMenuList = new List<TabControl>();

        /// <summary>
        /// Get the list of dock panes.
        /// </summary>
        public List<TabControl> PrimaryMenuList
        {
            get { return _primaryMenuList; }
        }

        private int _selectedPanelHeaderIndex = 0;

        /// <summary>
        /// Get/Set the active pane.
        /// </summary>
        public int SelectedPanelHeaderIndex
        {
            get { return _selectedPanelHeaderIndex; }
            set
            {
                SetProperty(ref _selectedPanelHeaderIndex, value, () => SelectedPanelHeaderIndex);
                if (_selectedPanelHeaderIndex == 0)
                    CurrentPage = _paneH1VM;
                if (_selectedPanelHeaderIndex == 1)
                    CurrentPage = _paneH2VM;
            }
        }

        private PanelViewModelBase _currentPage;

        /// <summary>
        /// The currently active DockPane.
        /// </summary>
        public PanelViewModelBase CurrentPage
        {
            get { return _currentPage; }
            set
            {
                SetProperty(ref _currentPage, value, () => CurrentPage);
            }
        }

        private bool _initialised = false;

        /// <summary>
        /// Has the DockPane been initialised?
        /// </summary>
        public bool Initialised
        {
            get { return _initialised; }
            set
            {
                _initialised = value;
            }
        }

        private bool _inError = false;

        /// <summary>
        /// Is the DockPane in error?
        /// </summary>
        public bool InError
        {
            get { return _inError; }
            set
            {
                _inError = value;
            }
        }

        private bool _queryRunning;

        /// <summary>
        /// Is the SQL query running?
        /// </summary>
        public bool QueryRunning
        {
            get { return _queryRunning; }
            set { _queryRunning = value; }
        }

        private string _helpURL;

        /// <summary>
        /// The URL of the help page.
        /// </summary>
        public string HelpURL
        {
            get { return _helpURL; }
            set { _helpURL = value; }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Initialise the query pane.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> InitialiseQueryPaneAsync()
        {
            _paneH2VM = new PaneHeader2ViewModel(_dockPane, _paneH1VM.ToolConfig);

            string sdeFileName = _paneH1VM.ToolConfig.GetSDEName;

            // Check if the SDE file exists.
            if (!FileFunctions.FileExists(sdeFileName))
            {
                MessageBox.Show("SDE connection file '" + sdeFileName + "' not found.", "Data Selector", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // Open the SQL Server geodatabase.
            bool sdeConnectionValid;
            try
            {
                sdeConnectionValid = await SQLServerFunctions.CheckSDEConnection(sdeFileName);
            }
            catch (Exception)
            {
                _paneH2VM = null;
                return false;
            }

            // In the SDE connection is not valid.
            if (!sdeConnectionValid)
            {
                _paneH2VM = null;
                return false;
            }

            // Trigger getting the SQL Server table names (don't wait for the response).
            _paneH2VM.GetTableNames();

            return true;
        }

        /// <summary>
        /// Clear the query pane.
        /// </summary>
        public void ClearQueryPane()
        {
            _paneH2VM = null;
        }

        /// <summary>
        /// Event when the DockPane is hidden.
        /// </summary>
        protected override void OnHidden()
        {
            // Get the dockpane DAML id.
            DockPane pane = FrameworkApplication.DockPaneManager.Find(_dockPaneID);
            if (pane == null)
                return;

            // Get the ViewModel by casting the dockpane.
            DockpaneMainViewModel vm = pane as DockpaneMainViewModel;

            // Force the dockpane to be re-initialised next time it's shown.
            vm.Initialised = false;
        }

        public void RefreshPanel1Buttons()
        {
            _paneH1VM.RefreshButtons();
        }

        #endregion

    }

    /// <summary>
    /// Button implementation to show the DockPane.
    /// </summary>
    internal class DockpaneMain_ShowButton : Button
    {

        protected override void OnClick()
        {
            //string uri = System.Reflection.Assembly.GetExecutingAssembly().Location;

            // Show the dock pane.
            DockpaneMainViewModel.Show();
        }

    }
}
