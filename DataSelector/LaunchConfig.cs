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

using System;
using System.Windows.Forms;
using System.Xml;
using System.Windows;
using MessageBox = ArcGIS.Desktop.Framework.Dialogs.MessageBox;

// This configuration file reader defines how the tool behaves at start up:
// Does it show a dropdown list to choose a configuration file, or does it launch
// a default profile straight away?

namespace DataTools
{
    /// <summary>
    /// This class reads the tool XML file and stores the results.
    /// </summary>
    class LaunchConfig
    {

        #region Fields

        private static string _toolName;

        private bool blChooseConfig;

        // Initialise components to read XML
        XmlElement xmlToolNode;

        #endregion

        #region Constructor

        /// <summary>
        /// Get the tool XML file and read the variables.
        /// </summary>
        /// <param name="xmlFolder"></param>
        /// <param name="toolName"></param>
        /// <param name="promptFilePath"></param>
        public LaunchConfig(string xmlFolder, string toolName, bool promptFilePath)
        {
            _toolName = toolName;

            _xmlFound = false;
            _xmlLoaded = false;
            _selectCancelled = false;

            // Check the XML file exists (or prompt the user)
            _xmlFound = XMLFileFound(xmlFolder, toolName, promptFilePath);

            // If the user didn't select a folder when prompted.
            if (GetSelectCancelled)
                return;

            if (!_xmlFound)
            {
                MessageBox.Show("Error loading XML file. '" + toolName + ".xml' was not found in the XML directory.", toolName, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Load the XML file into memory.
            XmlDocument xmlConfig = new XmlDocument();
            try
            {
                xmlConfig.Load(_xmlFile);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading XML file. System error message: " + ex.Message, toolName, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Get the InitialConfig node (the first node).
            XmlNode currNode = xmlConfig.DocumentElement.FirstChild;
            xmlToolNode = (XmlElement)currNode;

            // Get the mandatory variables.
            try
            {
                if (!GetMandatoryVariables())
                    return;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading XML file. System error message: " + ex.Message, toolName, MessageBoxButton.OK, MessageBoxImage.Error);
            }

            _xmlLoaded = true;
        }

        /// <summary>
        /// Check the XML file exists and prompt the user if not.
        /// </summary>
        /// <param name="xmlFolder"></param>
        /// <param name="toolName"></param>
        /// <param name="promptFilePath"></param>
        /// <returns></returns>
        private bool XMLFileFound(string xmlFolder, string toolName, bool promptFilePath)
        {
            string xmlFile = xmlFolder + String.Format(@"\{0}.xml", _toolName);

            try
            {
                // If the user is to be prompted for a file path.
                if (promptFilePath)
                {
                    // Prompt the user for the correct file path.
                    string xmlFilePath = GetConfigFilePath();

                    if (String.IsNullOrEmpty(xmlFilePath))
                    {
                        _selectCancelled = true;
                        return false;
                    }

                    xmlFolder = xmlFilePath;
                    xmlFile = xmlFolder + String.Format(@"\{0}.xml", _toolName);
                }
                // If the app XML file path is blank or doesn't exist.
                else if (String.IsNullOrEmpty(xmlFile) || !FileFunctions.FileExists(xmlFile))
                {
                    _xmlLoaded = false;
                    return false;
                }

                // Check the app XML file path exists.
                if (!String.IsNullOrEmpty(xmlFile) && (FileFunctions.FileExists(xmlFile)))
                {
                    _xmlFolder = xmlFolder;
                    _xmlFile = xmlFile;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading XML file: " + ex.Message, toolName, MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return true;
        }

        /// <summary>
        /// Get the mandatory variables from the XML file.
        /// </summary>
        /// <returns></returns>
        public bool GetMandatoryVariables()
        {
            string strRawText;

            // Get the user choice variable.
            try
            {
                blChooseConfig = false;
                strRawText = xmlToolNode["ChooseXML"].InnerText;
                if (strRawText.ToLower() == "yes" || strRawText.ToLower() == "y")
                {
                    blChooseConfig = true;
                }
            }
            catch
            {
                MessageBox.Show("Could not locate item 'ChooseXML' in the XML file.", "XML Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // Get the default XML file name.
            try
            {
                strRawText = xmlToolNode["DefaultProfile"].InnerText;
                if (strRawText != "")
                    _defaultXML = strRawText; // If there is an entry; otherwise use the default.
            }
            catch
            {
                MessageBox.Show("Could not locate item 'DefaultProfile' in the XML file.", "XML Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // Get the URL of the help page.
            try
            {
                _helpURL = xmlToolNode["HelpURL"].InnerText;
            }
            catch
            {
                MessageBox.Show("Could not locate item 'HelpURL' in the XML file.", "XML Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // All mandatory variables were loaded successfully.
            return true;
        }

        #endregion

        #region Members

        private bool _xmlFound;

        /// <summary>
        /// Has the XML file been found.
        /// </summary>
        public bool XMLFound
        {
            get
            {
                return _xmlFound;
            }
        }

        private bool _xmlLoaded;

        /// <summary>
        /// Has the XML file been loaded.
        /// </summary>
        public bool XMLLoaded
        {
            get
            {
                return _xmlLoaded;
            }
        }

        private string _xmlFolder;

        /// <summary>
        /// The XML folder path.
        /// </summary>
        public string XMLFolder
        {
            get
            {
                return _xmlFolder;
            }
        }

        private string _xmlFile;

        /// <summary>
        /// The XML file path.
        /// </summary>
        public string XMLFile
        {
            get
            {
                return _xmlFile;
            }
        }

        /// <summary>
        /// Can the user choose the profile XML file.
        /// </summary>
        public bool ChooseConfig
        {
            get
            {
                return blChooseConfig;
            }
        }

        #endregion

        #region Variables

        private string _defaultXML = "DefaultProfile.xml";

        public string GetDefaultXML
        {
            get
            {
                return _defaultXML;
            }
        }

        private bool _selectCancelled = false;

        public bool GetSelectCancelled
        {
            get
            {
                return _selectCancelled;
            }
        }

        private string _helpURL;

        public string GetHelpURL
        {
            get
            {
                return _helpURL;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Prompt the user for the file path containing the app XML file.
        /// </summary>
        /// <returns></returns>
        private static string GetConfigFilePath()
        {
            // Create folder dialog.
            FolderBrowserDialog xmlFolder = new FolderBrowserDialog();

            // Set the folder dialog title.
            xmlFolder.Description = String.Format("Select folder containing '{0}.xml' file ...", _toolName);
            xmlFolder.UseDescriptionForTitle = true;
            xmlFolder.ShowNewFolderButton = false;

            // Show folder dialog.
            if (xmlFolder.ShowDialog() == DialogResult.OK)
            {
                // Return the selected path.
                xmlFolder.Dispose();
                return xmlFolder.SelectedPath;
            }
            else
            {
                xmlFolder.Dispose();
                return null;
            }
        }

        #endregion

    }
}