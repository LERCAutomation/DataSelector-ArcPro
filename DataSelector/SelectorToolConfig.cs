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

using DataTools;


// This module reads the config XML file and stores the results.

namespace DataSelector
{
    class SelectorToolConfig
    {

        #region Fields

        // Declare all the variables
        private string _fileDSN;
        private string _connectionString;
        private string _logFilePath;
        private string _defaultExtractPath;
        private string _defaultQueryPath;
        private string _defaultFormat;
        private string _databaseSchema;
        private string _includeWildcard;
        private string _excludeWildcard;
        private string _recMax;
        private bool _defaultSetSymbology;
        private string _layerLocation;
        private string _enableSpatialPlotting; // do not currently need this but keeping for reference.
        private bool _defaultClearLogFile;
        private int _timeOutSeconds;

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

                // Get all of the detail into the object
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

                try
                {
                    _connectionString = xmlDataSelector["ConnectionString"].InnerText;
                }
                catch
                {
                    MessageBox.Show("Could not locate the item 'ConnectionString' in the XML file");
                    _xmlLoaded = false;
                    return;
                }

                try
                {
                    string strTimeout = xmlDataSelector["TimeoutSeconds"].InnerText;
                    bool blSuccess;

                    if (strTimeout != "")
                    {

                        blSuccess = int.TryParse(strTimeout, out _timeOutSeconds);
                        if (!blSuccess)
                        {
                            MessageBox.Show("The value entered for TimeoutSeconds in the XML file is not an integer number");
                            _xmlLoaded = false;
                        }
                        if (_timeOutSeconds < 0)
                        {
                            MessageBox.Show("The value entered for TimeoutSeconds in the XML file is negative");
                            _xmlLoaded = false;
                        }
                    }
                    else
                    {
                        _timeOutSeconds = 0; // None given.
                    }

                }
                catch
                {
                    _timeOutSeconds = 0; // We don't really care if it's not in because there's a default anyway.
                    return;
                }

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
                try
                {
                    _recMax = xmlDataSelector["RecMax"].InnerText;
                }
                catch
                {
                    MessageBox.Show("Could not locate the item 'RecMax' in the XML file");
                    _xmlLoaded = false;
                    return;
                }
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
                try
                {
                    _enableSpatialPlotting = xmlDataSelector["EnableSpatialPlotting"].InnerText;
                }
                catch
                {
                    MessageBox.Show("Could not locate the item 'EnableSpatialPlotting' in the XML file");
                    _xmlLoaded = false;
                }
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

        public string GetConnectionString
        {
            get
            {
                return _connectionString;
            }
        }

        public int GetTimeoutSeconds
        {
            get
            {
                return _timeOutSeconds;
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

        public string GetRecMax
        {
            get
            {
                return _recMax;
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

        public string GetEnableSpatialPlotting
        {
        get
        {
            return _enableSpatialPlotting;
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
