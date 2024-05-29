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
using DataTools;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using DataSelector.Properties;
using System.Windows;
using MessageBox = ArcGIS.Desktop.Framework.Dialogs.MessageBox;
using System.Windows.Media;
using System.Windows.Markup;

namespace DataSelector.UI
{
    /// <summary>
    /// Load the XML file and prompt the user to select
    /// an XML profile to load.
    /// </summary>
    internal class PaneHeader1ViewModel : PanelViewModelBase, INotifyPropertyChanged
    {

        #region Fields

        private DockpaneMainViewModel _dockPane;

        private string _displayName = "DataSelector";

        #endregion

        #region ViewModelBase Members

        public override string DisplayName
        {
            get { return _displayName; }
        }

        #endregion

        #region Creator

        /// <summary>
        /// Set the global variables.
        /// </summary>
        /// <param name="xmlFilesList"></param>
        /// <param name="defaultXMLFile"></param>
        public PaneHeader1ViewModel(DockpaneMainViewModel dockPane)
        {
            _dockPane = dockPane;

            // Get the tool XML config file path and name from settings.
            _xmlFolder = Settings.Default.XMLFolder;

            InitializeComponent();
        }

        /// <summary>
        /// Initialise the profile pane.
        /// </summary>
        private void InitializeComponent()
        {
            _xmlError = false;

            string xmlConfigPath = null;

            // Open the tool XML config file and determine if the user will
            // choose which tool XML config file to load or if the default
            // file will be used.
            ToolConfig toolConfig;
            toolConfig = new(_xmlFolder, _displayName, false);

            // If the tool config file can't be found or hasn't been loaded.
            if ((!toolConfig.XMLFound) || (!toolConfig.XMLLoaded))
            {
                // Clear the list and selection.
                _availableXMLFiles = [];
                _selectedXMLProfile = null;

                // Update the fields and buttons in the form.
                OnPropertyChanged(nameof(XMLFolder));
                OnPropertyChanged(nameof(AvailableXMLFiles));
                OnPropertyChanged(nameof(SelectedXMLProfile));
                OnPropertyChanged(nameof(CanSelectXMLPath));
                OnPropertyChanged(nameof(CanLoadProfile));

                return;
            }

            // As the tool config file exists and has loaded ...

            // Set the help URL.
            _dockPane.HelpURL = toolConfig.GetHelpURL;

            // Set the default XML profile name.
            string defaultXML = toolConfig.GetDefaultXML;

            List<string> xmlFilesList = [];
            bool blOnlyDefault = false;
            bool blDefaultFound = false;

            // If the user is allowed to choose the XML profile then
            // check if there are multiple profiles to choose from.
            if (toolConfig.ChooseConfig)
            {
                // Get a list of all of the valid XML profiles in the folder.
                GetValidXMLFiles(_xmlFolder, toolConfig.GetDefaultXML, ref xmlFilesList, ref blDefaultFound, ref blOnlyDefault);

                // If no valid files were found.
                if (xmlFilesList is null || xmlFilesList.Count == 0)
                    return;
            }

            // If the user is allowed to choose the XML profile and there are
            // more then just the default profile in the folder, load the
            // list of files for the user to choose.
            if (toolConfig.ChooseConfig && !blOnlyDefault)
            {
                _availableXMLFiles = xmlFilesList;
                _selectedXMLProfile = defaultXML;
            }
            else
            {
                // If the user isn't allowed to choose, or if there is only the
                // default XML file in the directory, then use the default.
                xmlConfigPath = _xmlFolder + @"\" + defaultXML;

                // If the default XML file exists.
                if (FileFunctions.FileExists(xmlConfigPath))
                {
                    // Set the list to just the default XML file
                    // and select it.
                    xmlFilesList.Add(defaultXML);
                    _availableXMLFiles = xmlFilesList;
                    _selectedXMLProfile = defaultXML;
                }
                else
                {
                    // Clear the list and selection.
                    xmlConfigPath = null;
                    _availableXMLFiles = [];
                    _selectedXMLProfile = null;
                }
            }

            // Update the fields and buttons in the form.
            OnPropertyChanged(nameof(XMLFolder));
            OnPropertyChanged(nameof(AvailableXMLFiles));
            OnPropertyChanged(nameof(SelectedXMLProfile));
            OnPropertyChanged(nameof(CanSelectXMLPath));
            OnPropertyChanged(nameof(CanLoadProfile));

            // If the XML config file has been set (and it exists) then load it.
            if (xmlConfigPath != null)
            {
                // Load the default profile.
                LoadXMLProfile(xmlConfigPath, false);
            }
        }

        #endregion

        #region SelectXMLPath Command

        private ICommand _selectXMLPathCommand;

        /// <summary>
        /// Create the SelectXMLPath button command.
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public ICommand SelectXMLPathCommand
        {
            get
            {
                if (_selectXMLPathCommand == null)
                {
                    Action<object> selectXMLAction = new Action<object>(this.SelectXMLPathCommandClick);
                    _selectXMLPathCommand = new RelayCommand(selectXMLAction, param => this.CanSelectXMLPath);
                }

                return _selectXMLPathCommand;
            }
        }

        /// <summary>
        /// Handles the event when the SelectXMLPath button is clicked.
        /// </summary>
        /// <param name="param"></param>
        /// <remarks></remarks>
        private void SelectXMLPathCommandClick(object param)
        {
            // Load the selected config file.
            LoadToolConfig();
        }

        /// <summary>
        /// Can the SelectXMLPath button be pressed?
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public bool CanSelectXMLPath
        {
            get
            {
                return (!_dockPane.QueryRunning);
            }
        }

        #endregion

        #region Select XML Profile

        private List<string> _availableXMLFiles;

        /// <summary>
        /// List of valid XML profiles that the user can choose from.
        /// </summary>
        public List<string> AvailableXMLFiles
        {
            get
            {
                return _availableXMLFiles;
            }
            set => SetProperty(ref _availableXMLFiles, value);
        }

        private string _selectedXMLProfile;

        /// <summary>
        /// The XML profile that the user has chosen.
        /// </summary>
        public string SelectedXMLProfile
        {
            get
            {
                return _selectedXMLProfile;
            }
            set => SetProperty(ref _selectedXMLProfile, value);
        }

        /// <summary>
        /// Can the user select an XML profile?
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public bool CanSelectXMLProfile
        {
            get
            {
                return (!string.IsNullOrEmpty(XMLFolder));
            }
        }

        #endregion

        #region Load Profile Command

        private ICommand _loadProfileCommand;

        /// <summary>
        /// Create the Open XML button command.
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public ICommand LoadProfileCommand
        {
            get
            {
                if (_loadProfileCommand == null)
                {
                    Action<object> openXMLAction = new Action<object>(this.LoadProfileCommandClick);
                    _loadProfileCommand = new RelayCommand(openXMLAction, param => this.CanLoadProfile);
                }

                return _loadProfileCommand;
            }
        }

        /// <summary>
        /// Handles the event when the Open XML button is clicked.
        /// </summary>
        /// <param name="param"></param>
        /// <remarks></remarks>
        private async void LoadProfileCommandClick(object param)
        {
            // Skip if no profile selected (shouldn't be possible).
            if (SelectedXMLProfile == null)
                return;

            // Set the full path for the profile file.
            string xmlConfigFile = _xmlFolder + @"\" + SelectedXMLProfile;

            // Check the file (still) exists.
            if (!FileFunctions.FileExists(xmlConfigFile))
            {
                MessageBox.Show("The selected XML file '" + SelectedXMLProfile + "' was not found in the XML directory.", "Data Selector", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Load the selected profile.
            LoadXMLProfile(xmlConfigFile, true);

            // Clear the query pane if the XML wasn't loaded.
            if (!XMLLoaded)
            {
                // Clear the query pane.
                _dockPane.ClearQueryPane();
                return;
            }

            // Initialise the query pane.
            bool initialised = await _dockPane.InitialiseQueryPaneAsync();
            if (!initialised)
            {
                MessageBox.Show("SDE connection file not valid.", "Data Selector", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Select the query pane.
            _dockPane.SelectedPanelHeaderIndex = 1;
        }

        /// <summary>
        /// Can the Load Profile button be pressed (has a profile been selected)?
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public bool CanLoadProfile
        {
            get
            {
                return (!string.IsNullOrEmpty(SelectedXMLProfile)
                    && !_dockPane.QueryRunning);
            }
        }

        #endregion

        #region Properties

        private DataSelectorConfig _xmlConfig;

        public DataSelectorConfig ToolConfig
        {
            get
            {
                return _xmlConfig;
            }
        }

        private bool _xmlError = false;

        public bool XMLError
        {
            get
            {
                return _xmlError;
            }
        }

        private bool _xmlLoaded = false;

        public bool XMLLoaded
        {
            get
            {
                return _xmlLoaded;
            }
        }

        private string _xmlFolder = null;

        public string XMLFolder
        {
            get
            {
                return _xmlFolder;
            }
            set => SetProperty(ref _xmlFolder, value);
        }

        public ImageSource ButtonXMLFilePathImg
        {
            get
            {
                var imageSource = System.Windows.Application.Current.Resources["FolderOpenState16"] as ImageSource;
                return imageSource;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Read the tool config file to see if a default XML file is
        /// found or if the user will be prompted to choose one, and
        /// if the user is allowed to choose the XML profile.
        /// </summary>
        private void LoadToolConfig()
        {
            string xmlConfigPath = null;

            // Open the tool XML config file and determine if the user will
            // choose which tool XML config file to load or if the default
            // file will be used.
            ToolConfig toolConfig;
            toolConfig = new(_xmlFolder, _displayName, true);

            // If the user didn't select a folder when prompted.
            if (toolConfig.SelectCancelled)
                return;

            // If the tool config file can't be found or hasn't been loaded.
            if ((!toolConfig.XMLFound) || (!toolConfig.XMLLoaded))
            {
                // Clear the list and selection.
                _availableXMLFiles = [];
                _selectedXMLProfile = null;

                // Update the fields and buttons in the form.
                OnPropertyChanged(nameof(XMLFolder));
                OnPropertyChanged(nameof(AvailableXMLFiles));
                OnPropertyChanged(nameof(SelectedXMLProfile));
                OnPropertyChanged(nameof(CanSelectXMLPath));
                OnPropertyChanged(nameof(CanLoadProfile));

                return;
            }

            // As the tool config file exists and has loaded ...

            // Set the help URL.
            _dockPane.HelpURL = toolConfig.GetHelpURL;

            // Set the default XML profile name.
            string defaultXML = toolConfig.GetDefaultXML;

            // Set the folder path containing the tool config file.
            _xmlFolder = toolConfig.XMLFolder;

            // Save the folder path to settings for the future.
            Settings.Default.XMLFolder = _xmlFolder;
            Settings.Default.Save();

            List<string> xmlFilesList = [];
            bool blOnlyDefault = false;
            bool blDefaultFound = false;

            // If the user is allowed to choose the XML profile then
            // check if there are multiple profiles to choose from.
            if (toolConfig.ChooseConfig)
            {
                // Get a list of all of the valid XML profiles in the folder.
                GetValidXMLFiles(_xmlFolder, toolConfig.GetDefaultXML, ref xmlFilesList, ref blDefaultFound, ref blOnlyDefault);

                // If no valid files were found.
                if (xmlFilesList is null || xmlFilesList.Count == 0)
                {
                    MessageBox.Show("No valid XML files found in the XML directory.", "Data Selector", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            // If the user is allowed to choose the XML profile and there are
            // more then just the default profile in the folder, load the
            // list of files for the user to choose.
            if (toolConfig.ChooseConfig && !blOnlyDefault)
            {
                _availableXMLFiles = xmlFilesList;
                _selectedXMLProfile = defaultXML;
            }
            else
            {
                // If the user isn't allowed to choose, or if there is only the
                // default XML file in the directory, then use the default.
                xmlConfigPath = _xmlFolder + @"\" + defaultXML;

                // If the default XML file exists.
                if (FileFunctions.FileExists(xmlConfigPath))
                {
                    // Set the list to just the default XML file
                    // and select it.
                    xmlFilesList.Add(defaultXML);
                    _availableXMLFiles = xmlFilesList;
                    _selectedXMLProfile = defaultXML;
                }
                else
                {
                    // Clear the list and selection.
                    xmlConfigPath = null;
                    _availableXMLFiles = [];
                    _selectedXMLProfile = null;
                }
            }

            // Update the fields and buttons in the form.
            OnPropertyChanged(nameof(XMLFolder));
            OnPropertyChanged(nameof(AvailableXMLFiles));
            OnPropertyChanged(nameof(SelectedXMLProfile));
            OnPropertyChanged(nameof(CanSelectXMLPath));
            OnPropertyChanged(nameof(CanLoadProfile));

            // If the XML config file has been set (and it exists) then load it.
            if (xmlConfigPath != null)
            {
                // Load the default profile.
                LoadXMLProfile(xmlConfigPath, false);
            }
        }

        /// <summary>
        /// Get a list of valid XML files from the specified folder, and check
        /// if any of the files is the default profile and if only the
        /// </summary>
        /// <param name="strXMLFolder"></param>
        /// <param name="strDefaultXMLName"></param>
        /// <param name="xmlFilesList"></param>
        /// <param name="blDefaultFound"></param>
        /// <param name="blOnlyDefault"></param>
        private void GetValidXMLFiles(string strXMLFolder, string strDefaultXMLName, ref List<string> xmlFilesList, ref bool blDefaultFound, ref bool blOnlyDefault)
        {
            blDefaultFound = false;
            blOnlyDefault = true;

            // Get a list of all of the files in the XML directory.
            List<string> allFilesList = FileFunctions.GetAllFilesInDirectory(strXMLFolder);

            // Loop through the list for valid XML files.
            xmlFilesList = [];
            foreach (string strFile in allFilesList)
            {
                // Add if it's not the tool XML file.
                string strFileName = FileFunctions.GetFileName(strFile);
                if (!FileFunctions.GetFileNameWithoutExtension(strFileName).Equals(_displayName, StringComparison.CurrentCultureIgnoreCase)
                && FileFunctions.GetExtension(strFile).Equals(".xml", StringComparison.CurrentCultureIgnoreCase))
                {
                    // Add file to list of XML files.
                    xmlFilesList.Add(strFileName);
                    if (strFileName.Equals(strDefaultXMLName, StringComparison.CurrentCultureIgnoreCase))
                        blDefaultFound = true;
                    else
                        blOnlyDefault = false;
                }
            }

            // Sort the list of XML files.
            xmlFilesList.Sort();
        }

        /// <summary>
        /// Load the selected XML profile.
        /// </summary>
        /// <param name="xmlConfigPath"></param>
        public void LoadXMLProfile(string xmlConfigPath, bool msgErrors)
        {
            // Load the selected XML config file.
            _xmlConfig = new(xmlConfigPath, _displayName, msgErrors);

            // If the XML config file can't be found.
            if (!_xmlConfig.XMLFound)
            {
                if (msgErrors)
                    MessageBox.Show(string.Format("XML file '{0}' not found.", xmlConfigPath), "Data Selector", MessageBoxButton.OK, MessageBoxImage.Error);

                _xmlLoaded = false;
                return;
            }

            // If the XML config file can't be loaded.
            if (!_xmlConfig.XMLLoaded)
            {
                //MessageBox.Show(string.Format("Error loading XML File '{0}'.", _xmlConfigPath), "Data Selector", MessageBoxButton.OK, MessageBoxImage.Error);
                _xmlLoaded = false;
                return;
            }

            // Indicate the XML has been loaded.
            _xmlLoaded = true;
        }

        /// <summary>
        /// Refresh the buttons on the pane (before/after the
        /// query runs from the second pane).
        /// </summary>
        public void RefreshButtons()
        {
            // Update the fields and buttons in the form.
            OnPropertyChanged(nameof(CanSelectXMLPath));
            OnPropertyChanged(nameof(CanLoadProfile));
        }

        #endregion

        #region Debugging Aides

        /// <summary>
        /// Warns the developer if this object does not have
        /// a public property with the specified name. This
        /// method does not exist in a Release build.
        /// </summary>
        [Conditional("DEBUG")]
        [DebuggerStepThrough]
        public void VerifyPropertyName(string propertyName)
        {
            // Verify that the property name matches a real,
            // public, instance property on this object.
            if (TypeDescriptor.GetProperties(this)[propertyName] == null)
            {
                string msg = "Invalid property name: " + propertyName;

                if (this.ThrowOnInvalidPropertyName)
                    throw new(msg);
                else
                    Debug.Fail(msg);
            }
        }

        /// <summary>
        /// Returns whether an exception is thrown, or if a Debug.Fail() is used
        /// when an invalid property name is passed to the VerifyPropertyName method.
        /// The default value is false, but subclasses used by unit tests might
        /// override this property's getter to return true.
        /// </summary>
        protected virtual bool ThrowOnInvalidPropertyName { get; private set; }

        #endregion // Debugging Aides

        #region INotifyPropertyChanged Members

        /// <summary>
        /// Raised when a property on this object has a new value.
        /// </summary>
        public new event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises this object's PropertyChanged event.
        /// </summary>
        /// <param name="propertyName">The property that has a new value.</param>
        internal virtual void OnPropertyChanged(string propertyName)
        {
            this.VerifyPropertyName(propertyName);

            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                PropertyChangedEventArgs e = new(propertyName);
                handler(this, e);
            }
        }

        #endregion

    }
}
