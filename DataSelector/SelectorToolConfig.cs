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

using System;
using System.Windows.Forms;
using System.Xml;

namespace DataSelector
{
    /// <summary>
    /// This class reads the config XML file and stores the results.
    /// </summary>
    class SelectorToolConfig
    {

        #region Fields

        // Declare all the variables
        private string _logFilePath;
        private string _fileDSN;
        private string _selectStoredProcedure;
        private string _clearStoredProcedure;
        private string _defaultExtractPath;
        private string _defaultQueryPath;
        private string _defaultFormat;
        private string _databaseSchema;
        private string _includeWildcard;
        private string _excludeWildcard;
        private bool _defaultSetSymbology;
        private string _layerLocation;
        private bool _defaultClearLogFile;

        private bool _xmlFound;
        private bool _xmlLoaded;

        // Initialise component to read XML
        private XmlElement xmlDataSelector;

        #endregion

        #region Constructor

        public SelectorToolConfig(string anXMLProfile)
        {

            string strXMLFile = anXMLProfile; // The user has specified this and we've checked it exists.

            _xmlFound = true;   // We have already checked that it exists.
            _xmlLoaded = true;

            // Now get the variables from the XML file.
            if (_xmlFound)
            {
                // Load the XML file into memory.
                XmlDocument xmlConfig = new XmlDocument();
                try
                {
                    xmlConfig.Load(strXMLFile);
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
                xmlDataSelector = (XmlElement)currNode;

                // The existing file location where log files will be saved with output messages.
                try
                {
                    _logFilePath = xmlDataSelector["LogFilePath"].InnerText;
                }
                catch
                {
                    MessageBox.Show("Could not locate the item 'LogFilePath' in the XML file");
                    _xmlLoaded = false;
                    return;
                }

                // The location of the SDE file that specifies which SQL Server database to connect to.
                try
                {
                    _fileDSN = xmlDataSelector["FileDSN"].InnerText;
                }
                catch
                {
                    MessageBox.Show("Could not locate the item 'FileDSN' in the XML file");
                    _xmlLoaded = false;
                    return;
                }

                // Stored procedure to execute selection in SQL Server.
                try
                {
                    _selectStoredProcedure = xmlDataSelector["SelectStoredProcedure"].InnerText;
                }
                catch
                {
                    MessageBox.Show("Could not locate the item 'SelectStoredProcedure' in the XML file");
                    _xmlLoaded = false;
                    return;
                }

                // Stored procedure to clear selection in SQL Server.
                try
                {
                    _clearStoredProcedure = xmlDataSelector["ClearStoredProcedure"].InnerText;
                }
                catch
                {
                    MessageBox.Show("Could not locate the item 'ClearStoredProcedure' in the XML file");
                    _xmlLoaded = false;
                    return;
                }

                // The existing file location where extracts will be saved by default.
                try
                {
                    _defaultExtractPath = xmlDataSelector["DefaultExtractPath"].InnerText;
                }
                catch
                {
                    MessageBox.Show("Could not locate the item 'DefaultExtractPath' in the XML file");
                    _xmlLoaded = false;
                    return;
                }

                // The existing file location where queries will be saved and loaded by default.
                try
                {
                    _defaultQueryPath = xmlDataSelector["DefaultQueryPath"].InnerText;
                }
                catch
                {
                    MessageBox.Show("Could not locate the item 'DefaultQueryPath' in the XML file");
                    _xmlLoaded = false;
                    return;
                }

                // The default format of the output files to be created.
                try
                {
                    _defaultFormat = xmlDataSelector["DefaultFormat"].InnerText;
                }
                catch
                {
                    MessageBox.Show("Could not locate the item 'DefaultFormat' in the XML file");
                    _xmlLoaded = false;
                    return;
                }

                // The schema used in the SQL Server database.
                try
                {
                    _databaseSchema = xmlDataSelector["DatabaseSchema"].InnerText;
                }
                catch
                {
                    MessageBox.Show("Could not locate the item 'DatabaseSchema' in the XML file");
                    _xmlLoaded = false;
                    return;
                }

                // The Include wildcard for table names to list all the species tables
                // in SQL Server that can be selected by the user to extract from.
                try
                {
                    _includeWildcard = xmlDataSelector["IncludeWildcard"].InnerText;
                }
                catch
                {
                    MessageBox.Show("Could not locate the item 'IncludeWildcard' in the XML file");
                    _xmlLoaded = false;
                    return;
                }

                // The Exclude wildcard for table names that should NOT be used
                // for species tables in SQL Server that can be selected by the
                // user to extract from.
                try
                {
                    _excludeWildcard = xmlDataSelector["ExcludeWildcard"].InnerText;
                }
                catch
                {
                    MessageBox.Show("Could not locate the item 'ExcludeWildcard' in the XML file");
                    _xmlLoaded = false;
                    return;
                }

                // The default for whether the symbology should be set for feature classes or not.
                try
                {
                    _defaultSetSymbology = false;
                    strRawText = xmlDataSelector["DefaultSetSymbology"].InnerText;
                    if (strRawText.ToLower() == "yes" || strRawText.ToLower() == "y")
                        _defaultSetSymbology = true;
                }
                catch
                {
                    MessageBox.Show("Could not locate the item 'DefaultSetSymbology' in the XML file");
                    _xmlLoaded = false;
                    return;
                }

                // The location of layer files.
                try
                {
                    _layerLocation = xmlDataSelector["LayerLocation"].InnerText;
                }
                catch
                {
                    MessageBox.Show("Could not locate the item 'LayerLocation' in the XML file");
                    _xmlLoaded = false;
                    return;
                }

                // The default for whether an existing log file should be cleared by default.
                try
                {
                    _defaultClearLogFile = false;
                    strRawText = xmlDataSelector["DefaultClearLogFile"].InnerText;
                    if (strRawText.ToLower() == "yes" || strRawText.ToLower() == "y")
                        _defaultClearLogFile = true;
                }
                catch
                {
                    MessageBox.Show("Could not locate the item 'DefaultClearLogFile' in the XML file", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _xmlLoaded = false;
                    return;
                }

            }
            else
            {
                _xmlFound = false; // this has to be checked first; all other properties are empty.
            }

        }

        #endregion

        #region Members

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

        public string GetSDEName
        {
            get
            {
                return _fileDSN;
            }
        }

        public string GetSelectStoredProcedure
        {
            get
            {
                return _selectStoredProcedure;
            }
        }

        public string GetClearStoredProcedure
        {
            get
            {
                return _clearStoredProcedure;
            }
        }

        public string GetLogFilePath
        {
            get
            {
                return _logFilePath;
            }
        }

        public string GetDefaultExtractPath
        {
            get
            {
                return _defaultExtractPath;
            }
        }

        public string GetDefaultQueryPath
        {
            get
            {
                return _defaultQueryPath;
            }
        }

        public string GetDefaultFormat
        {
            get
            {
                return _defaultFormat;
            }
        }

        public string GetDatabaseSchema
        {
            get
            {
                return _databaseSchema;
            }
        }

        public string GetIncludeWildcard
        {
            get
            {
                return _includeWildcard;
            }
        }

        public string GetExcludeWildcard
        {
            get
            {
                return _excludeWildcard;
            }
        }

        public bool GetDefaultSetSymbology
        {
            get
            {
                return _defaultSetSymbology;
            }
        }

        public string GetLayerLocation
        {
            get
            {
                return _layerLocation;
            }
        }

        public bool GetDefaultClearLogFile
        {
            get
            {
                return _defaultClearLogFile;
            }
        }

        #endregion

    }
}