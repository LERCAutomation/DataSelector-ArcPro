// DataSelector is an ArcGIS add-in used to extract biodiversity
// information from SQL Server based on any selection criteria.
//
// Copyright © 2016-2017 SxBRC, 2017-2018 TVERC
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

// This configuration file reader defines how the tool behaves at start up:
// Does it show a dropdown list to choose a configuration file, or does it launch
// a default profile straight away?

namespace DataTools
{
    class LaunchConfig
    {

        #region Fields

        private static string _toolName;

        private bool blChooseConfig;

        // Initialise components to read XML
        XmlElement xmlToolNode;

        #endregion

        #region Constructor

        public LaunchConfig(string xmlFolder, string toolName, bool promptFilePath)
        {
            _toolName = toolName;
            _xmlFound = false;
            _xmlLoaded = true;
            _selectCancelled = false;

            string xmlFile = xmlFolder + String.Format(@"\{0}.xml", toolName);

            // Open and read the app XML file.
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
                        return;
                    }

                    xmlFolder = xmlFilePath;
                    xmlFile = xmlFolder + String.Format(@"\{0}.xml", toolName);
                }
                // If the app XML file path is blank or doesn't exist.
                else if (String.IsNullOrEmpty(xmlFile) || !FileFunctions.FileExists(xmlFile))
                {
                    _xmlLoaded = false;
                    return;
                }

                // Check the app XML file path exists.
                if (!String.IsNullOrEmpty(xmlFile) && (FileFunctions.FileExists(xmlFile)))
                {
                    _xmlFolder = xmlFolder;
                    _xmlFile = xmlFile;
                    _xmlFound = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error " + ex.ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // Now get the variables from the XML file.
            if (_xmlFound)
            {
                // Load the XML file into memory.
                XmlDocument xmlConfig = new XmlDocument();
                try
                {
                    xmlConfig.Load(xmlFile);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error in XML file; cannot load. System error message: " + ex.Message, "XML Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _xmlLoaded = false;
                    return;
                }

                // Get the InitialConfig node (the first node).
                string strRawText;
                XmlNode currNode = xmlConfig.DocumentElement.FirstChild;
                xmlToolNode = (XmlElement)currNode;

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
                    MessageBox.Show("Could not locate the item 'ChooseXML' in the XML file", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _xmlLoaded = false;
                    return;
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
                    MessageBox.Show("Could not locate the item 'DefaultProfile' in the XML file", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _xmlLoaded = false;
                    return;
                }
            }
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
        ///  Has the XML file been loaded.
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

        private string _defaultXML = "DefaultProfile.xml";

        /// <summary>
        /// The name of the default XML profile.
        /// </summary>
        public string DefaultXML
        {
            get
            {
                return _defaultXML;
            }
        }

        private bool _selectCancelled = false;

        public bool SelectCancelled
        {
            get
            {
                return _selectCancelled;
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
            //OpenFolderDialog folderDialog = new OpenFolderDialog
            //{
            //    Title = "Select folder containing 'DataSelector.xml' file ...",
            //    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
            //};

            //if (folderDialog.ShowDialog() == true)
            //{
            //    var folderName = folderDialog.FolderName;
            //    return folderName;
            //}
            //else
            //    return null;

            // Create folder dialog.
            FolderBrowserDialog xmlFolder = new FolderBrowserDialog();

            // Set the folder dialog title.
            xmlFolder.Description = String.Format("Select folder containing '{0}.xml' file ...", _toolName) ;
            xmlFolder.ShowNewFolderButton = false;

            // Show folder dialog.
            if (xmlFolder.ShowDialog() == DialogResult.OK)
            {
                // Return the selected path.
                return xmlFolder.SelectedPath;
            }
            else
                return null;
        }

        #endregion

    }
}