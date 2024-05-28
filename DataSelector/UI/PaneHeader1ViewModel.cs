// The Data tools are a suite of ArcGIS Pro addins used to extract
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

        private string _configFile = null;

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

            InitializeComponent();
        }

        /// <summary>
        /// Initialise the profile pane.
        /// </summary>
        private void InitializeComponent()
        {
            _xmlError = false;

            // Get the app XML config file path and name from settings.
            _xmlFolder = Settings.Default.XMLFolder;

            // Open the app XML config file and determine if the user will
            // choose which tool XML config file to load or if the default
            // file will be used.
            LaunchConfig launchConfig;
            launchConfig = new(_xmlFolder, _displayName, false);

            // If the app config file can't be found.
            if (!launchConfig.XMLFound)
            {
                //MessageBox.Show("XML file 'DataSelector.xml' not found in folder.", "Data Selector", MessageBoxButton.OK, MessageBoxImage.Error);
                //_xmlError = true;
                return;
            }
            // If the app config file hasn't been loaded.
            else if (!launchConfig.XMLLoaded)
            {
                //MessageBox.Show("Error loading XML File 'DataSelector.xml'.", "Data Selector", MessageBoxButton.OK, MessageBoxImage.Error);
                //_xmlError = true;
                return;
            }

            // Set the help URL.
            _dockPane.HelpURL = launchConfig.GetHelpURL;

            List<string> xmlFilesList = new();
            bool blOnlyDefault = false;
            bool blDefaultFound = false;

            // If the user is allowed to choose the XML profile then
            // check if there are multiple profiles to choose from.
            if (launchConfig.ChooseConfig)
            {
                // Get a list of all of the valid XML profiles in the folder.
                GetValidXMLFiles(_xmlFolder, launchConfig.GetDefaultXML, ref xmlFilesList, ref blDefaultFound, ref blOnlyDefault);

                if (xmlFilesList is null || xmlFilesList.Count() == 0)
                {
                    //MessageBox.Show("No valid XML files found in the XML directory.", "Data Selector", MessageBoxButton.OK, MessageBoxImage.Error);
                    //_xmlError = true;
                    return;
                }
            }

            // If the user is allowed to choose the XML profile and there are
            // more then just the default profile in the folder, load the
            // list of files for the user to choose.
            if (launchConfig.ChooseConfig && !blOnlyDefault)
            {
                _availableXMLFiles = xmlFilesList;
                _selectedXMLProfile = launchConfig.GetDefaultXML;
            }
            else
            {
                // If the user isn't allowed to choose, or if there is only the
                // default XML file in the directory, then use the default.
                _configFile = _xmlFolder + @"\" + launchConfig.GetDefaultXML;

                // Check the default XML file exists.
                if (!FileFunctions.FileExists(_configFile))
                {
                    //MessageBox.Show("The default XML file '" + launchConfig.GetDefaultXML + "' was not found in the XML directory.", "Data Selector", MessageBoxButton.OK, MessageBoxImage.Error);
                    //_xmlError = true;
                    return;
                }

                // Add the default file to the list and select it.
                xmlFilesList = new();
                xmlFilesList.Add(launchConfig.GetDefaultXML);
                _availableXMLFiles = xmlFilesList;
                _selectedXMLProfile = launchConfig.GetDefaultXML;
            }

            // Update the fields and buttons in the form.
            OnPropertyChanged(nameof(XMLFolder));
            OnPropertyChanged(nameof(AvailableXMLFiles));
            OnPropertyChanged(nameof(SelectedXMLProfile));
            OnPropertyChanged(nameof(CanSelectXMLPath));
            OnPropertyChanged(nameof(CanLoadProfile));

            // Cancel if no XML config file has been selected.
            if (_configFile == null)
                return;

            // Load the default profile.
            LoadConfig(_configFile);

            // Exit if the XML wasn't loaded.
            if (!XMLLoaded)
                return;
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
            // Cancel if unable to get the name of the tool XML
            // config file to load.
            if (!GetConfigFileName()) return;

            // Cancel if no XML config file has been selected.
            if (_configFile == null) return;

            // Update the fields and buttons in the form.
            OnPropertyChanged(nameof(XMLFolder));
            OnPropertyChanged(nameof(AvailableXMLFiles));
            OnPropertyChanged(nameof(SelectedXMLProfile));

            OnPropertyChanged(nameof(CanSelectXMLPath));
            OnPropertyChanged(nameof(CanLoadProfile));

            //// Load the default profile.
            //LoadConfig(_configFile);
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
            string configFile = _xmlFolder + @"\" + SelectedXMLProfile;

            // Check the file (still) exists.
            if (!FileFunctions.FileExists(configFile))
            {
                MessageBox.Show("The selected XML file '" + SelectedXMLProfile + "' was not found in the XML directory.", "Data Selector", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Load the selected profile.
            LoadConfig(configFile);

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

        private SelectorToolConfig _toolConfig;

        public SelectorToolConfig ToolConfig
        {
            get
            {
                return _toolConfig;
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
        /// Read the app config file to see if a default XML file is
        /// found or if the user will be prompted to choose one, and
        /// if the user is allowed to choose the XML profile.
        /// </summary>
        private bool GetConfigFileName()
        {
            // Open the app XML config file and determine if the user will
            // choose which tool XML config file to load or if the default
            // file will be used.
            LaunchConfig launchConfig;
            launchConfig = new(_xmlFolder, _displayName, true);

            // If the user didn't select a folder when prompted.
            if (launchConfig.GetSelectCancelled)
                return false;

            // If the app config file can't be found.
            if (!launchConfig.XMLFound)
            {
                MessageBox.Show("XML file 'DataSelector.xml' not found in folder.", "Data Selector", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            // If the app config file hasn't been loaded.
            else if (!launchConfig.XMLLoaded)
            {
                MessageBox.Show("Error loading XML File 'DataSelector.xml'.", "Data Selector", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // Set the folder path containing the app config file.
            _xmlFolder = launchConfig.XMLFolder;

            // As the app config file exists and has loaded then
            // save the folder path to settings for the future.
            Settings.Default.XMLFolder = _xmlFolder;
            Settings.Default.Save();

            List<string> xmlFilesList = new();
            bool blOnlyDefault = false;
            bool blDefaultFound = false;

            // If the user is allowed to choose the XML profile then
            // check if there are multiple profiles to choose from.
            if (launchConfig.ChooseConfig)
            {
                // Get a list of all of the valid XML profiles in the folder.
                GetValidXMLFiles(_xmlFolder, launchConfig.GetDefaultXML, ref xmlFilesList, ref blDefaultFound, ref blOnlyDefault);

                if (xmlFilesList is null || xmlFilesList.Count() == 0)
                {
                    MessageBox.Show("No valid XML files found in the XML directory.", "Data Selector", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }

            // If the user is allowed to choose the XML profile and there are
            // more then just the default profile in the folder, load the
            // list of files for the user to choose.
            if (launchConfig.ChooseConfig && !blOnlyDefault)
            {
                _availableXMLFiles = xmlFilesList;
                _selectedXMLProfile = launchConfig.GetDefaultXML;
            }
            else
            {
                // If the user isn't allowed to choose, or if there is only the
                // default XML file in the directory, then use the default.
                _configFile = _xmlFolder + @"\" + launchConfig.GetDefaultXML;

                // Check the default XML file exists.
                if (!FileFunctions.FileExists(_configFile))
                {
                    MessageBox.Show("The default XML file '" + launchConfig.GetDefaultXML + "' was not found in the XML directory.", "Data Selector", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                // Add the default file to the list and select it.
                xmlFilesList = new();
                xmlFilesList.Add(launchConfig.GetDefaultXML);
                _availableXMLFiles = xmlFilesList;
                _selectedXMLProfile = launchConfig.GetDefaultXML;
            }

            return true;
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
            xmlFilesList = new();
            foreach (string strFile in allFilesList)
            {
                // Add if it's not the app XML file.
                string strFileName = FileFunctions.GetFileName(strFile);
                if (FileFunctions.GetFileNameWithoutExtension(strFileName).ToLower() != _displayName.ToLower()
                && FileFunctions.GetExtension(strFile).ToLower() == ".xml")
                {
                    // Add file to list of XML files.
                    xmlFilesList.Add(strFileName);
                    if (strFileName.ToLower() == strDefaultXMLName.ToLower())
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
        /// <param name="configFile"></param>
        public void LoadConfig(string configFile)
        {
            _configFile = configFile;

            // Read the selected XML config file.
            _toolConfig = new(_configFile);

            // If the XML config file can't be found then close
            // the app.
            if (!_toolConfig.XMLFound)
            {
                MessageBox.Show(string.Format("XML file '{0}' not found.", _configFile), "Data Selector", MessageBoxButton.OK, MessageBoxImage.Error);
                _xmlLoaded = false;
                return;
            }
            // If the tool XML config file hasn't been loaded.
            else if (!_toolConfig.XMLLoaded)
            {
                MessageBox.Show(string.Format("Error loading XML File '{0}'.", _configFile), "Data Selector", MessageBoxButton.OK, MessageBoxImage.Error);
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
