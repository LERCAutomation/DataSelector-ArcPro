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
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

using HLSelectorToolConfig;
using HLESRISQLServerFunctions;
using HLStringFunctions;
using HLArcMapModule;
using HLFileFunctions;
using HLSelectorToolLaunchConfig;
using DataSelector.Properties;

using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.GeoDatabaseUI;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Framework;

using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.DataSourcesGDB;

using ESRI.ArcGIS.ArcMapUI;
using ESRI.ArcGIS.Geoprocessing;
using ESRI.ArcGIS.Geoprocessor;

// Unfortunately we also need an SQL client in order to run the stored procedures with parameters...
using System.Data.SqlClient;


namespace DataSelector
{
    public partial class frmDataSelector : Form
    {
        SelectorToolConfig myConfig;
        FileFunctions FileFunctions;
        StringFunctions myStringFuncs;
        ArcMapFunctions myArcMapFuncs;
        ArcSDEFunctions myArcSDEFuncs;
        SQLServerFunctions mySQLServerFuncs;
        SelectorToolLaunchConfig myLaunchConfig;
        bool blOpenForm; // This tracks all the way through whether the form is initialising correctly.
        string m_strSaveFile;

        public frmDataSelector()
        {
            blOpenForm = true;
            InitializeComponent();
            myLaunchConfig = new SelectorToolLaunchConfig();
            FileFunctions = new FileFunctions();
            string strConfigFile = "";
            if (!myLaunchConfig.XMLFound)
            {
                MessageBox.Show("XML file 'DataSelector.xml' not found; form cannot load.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                blOpenForm = false;
            }
            if (!myLaunchConfig.XMLLoaded)
            {
                MessageBox.Show("Error loading XML File 'DataSelector.xml'; form cannot load.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                blOpenForm = false;
            }

            if (blOpenForm)
            {
                string strXMLFolder = FileFunctions.GetDirectoryName(Settings.Default.XMLFile);
                bool blOnlyDefault = true;
                int intCount = 0;
                if (myLaunchConfig.ChooseConfig) // If we are allowed to choose, check if there are multiple profiles. 
                // If there is only the default XML file in the directory, launch the form. Otherwise the user has to choose.
                {
                    foreach (string strFileName in FileFunctions.GetAllFilesInDirectory(strXMLFolder))
                    {
                        if (FileFunctions.GetFileName(strFileName).ToLower() != "dataselector.xml" && FileFunctions.GetExtension(strFileName).ToLower() == "xml")
                        {
                            // is it the default?
                            intCount++;
                            if (FileFunctions.GetFileName(strFileName) != myLaunchConfig.DefaultXML)
                            {
                                blOnlyDefault = false;
                            }
                        }
                    }
                    if (intCount > 1)
                    {
                        blOnlyDefault = false;
                    }
                }
                if (myLaunchConfig.ChooseConfig && !blOnlyDefault)
                {
                    // User has to choose the configuration file first.

                    using (var myConfigForm = new frmChooseConfig(strXMLFolder, myLaunchConfig.DefaultXML))
                    {
                        var result = myConfigForm.ShowDialog();
                        if (result == System.Windows.Forms.DialogResult.OK)
                        {
                            strConfigFile = strXMLFolder + @"\" + myConfigForm.ChosenXMLFile;
                        }
                        else
                        {
                            MessageBox.Show("No XML file was chosen; form cannot load.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            blOpenForm = false;
                        }
                    }

                }
                else
                {
                    strConfigFile = strXMLFolder + @"\" + myLaunchConfig.DefaultXML; // don't allow the user to choose, just use the default.
                    // Just check it exists, though.
                    if (!FileFunctions.FileExists(strConfigFile))
                    {
                        MessageBox.Show("The default XML file '" + myLaunchConfig.DefaultXML + "' was not found in the XML directory; form cannot load.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        blOpenForm = false;
                    }
                }
            }

            if (blOpenForm)
            {
                myConfig = new SelectorToolConfig(strConfigFile); // Must now pass the correct XML name.
                IApplication pApp = ArcMap.Application;
                myArcMapFuncs = new ArcMapFunctions(pApp);
                myStringFuncs = new StringFunctions();

                // Get the relevant from the Config file.
                if (myConfig.GetFoundXML() == false)
                {
                    MessageBox.Show("XML file not found; form cannot load.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    blOpenForm = false;
                }
                else if (myConfig.GetLoadedXML() == false)
                {
                    MessageBox.Show("Error loading XML File; form cannot load.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    blOpenForm = false;
                }
            }

            // Close the form if there are any errors at this point.
            if (!blOpenForm)
            {
                Load += (s, e) => Close();
                return;
            }

            m_strSaveFile = "";

            myArcSDEFuncs = new ArcSDEFunctions();
            mySQLServerFuncs = new SQLServerFunctions();
            FileFunctions = new FileFunctions();
            // fill the list box with SQL tables
            string strSDE = myConfig.GetSDEName();

            if (!FileFunctions.FileExists(strSDE) && blOpenForm)
            {
                MessageBox.Show("ArcSDE connection file " + strSDE + " not found. Cannot load Data Selector", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                blOpenForm = false;
            }

            IWorkspace wsSQLWorkspace = null;

            if (blOpenForm)
            {
                try
                {
                    wsSQLWorkspace = myArcSDEFuncs.OpenArcSDEConnection(strSDE);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Cannot open ArcSDE connection " + strSDE + ". Error is " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    blOpenForm = false;
                }
            }
            if (blOpenForm)
            {

                string strIncludeWildcard = myConfig.GetIncludeWildcard();
                string strExcludeWildcard = myConfig.GetExcludeWildcard();
                string strDefaultFormat = myConfig.GetDefaultFormat();
                bool chkClearLog = myConfig.GetDefaultClearLogFile();

                cmbOutFormat.Text = strDefaultFormat;
                chkLogFile.Checked = chkClearLog;

                List<string> strTableList = myArcSDEFuncs.GetTableNames(wsSQLWorkspace, strIncludeWildcard, strExcludeWildcard);
                foreach (string strItem in strTableList)
                {
                    lstTables.Items.Add(strItem);
                }
                // Close the SQL connection
                wsSQLWorkspace = null;
                // However keep the Config and SQLFuncs objects alive for use later in the form.
            }
            else // Something has gone wrong during initialisation; don't load form.
            {
                Load += (s, e) => Close();
                return;
            }

        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            // Pull up Save As dialog.
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();

            saveFileDialog1.Filter = "Query files (*.qsf)|*.qsf";
            saveFileDialog1.InitialDirectory = myConfig.GetDefaultQueryPath();
            saveFileDialog1.FileName = m_strSaveFile;

            bool blDone = false;
            string strFileName = "";
            while (blDone == false)
            {
                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    strFileName = saveFileDialog1.FileName;
                    // Save the query name ready for future saves
                    m_strSaveFile = FileFunctions.GetFileName(strFileName);

                    string strExtension = strFileName.Substring(strFileName.Length - 4, 4);
                    if (strExtension.Substring(0, 1) != ".")
                        strFileName = strFileName + ".qsf";
                    else if (strExtension != ".qsf") // Wrong extension.
                    {
                        MessageBox.Show("File name has incorrect extension. Save cancelled");
                        return;
                    }
                    blDone = true; // New file

                }
                else // User pressed Cancel
                {
                    MessageBox.Show("Please select an output file");
                    return;
                }

            }
            StreamWriter qryFile = File.CreateText(strFileName);
            // Write query

            string strColumns = "Fields {" + txtColumns.Text.Replace("\r\n", "$$") + "}";
            string strWhere = "Where {" + txtWhere.Text.Replace("\r\n", "$$") + "}";
            string strGroupBy = "Group By {" + txtGroupBy.Text.Replace("\r\n", "$$") + "}";
            string strOrderBy = "Order By {" + txtOrderBy.Text.Replace("\r\n", "$$") + "}";
            qryFile.WriteLine(strColumns);
            qryFile.WriteLine(strWhere);
            qryFile.WriteLine(strGroupBy);
            qryFile.WriteLine(strOrderBy);
            qryFile.Close();
            qryFile.Dispose();
            MessageBox.Show("Query file saved");

        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            // Open file dialog appears
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.Filter = "Query files (*.qsf)|*.qsf";
            openFileDialog1.InitialDirectory = myConfig.GetDefaultQueryPath();

            string strFileName;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                // firstly clear the form.
                txtColumns.Text = "";
                txtWhere.Text = "";
                txtGroupBy.Text = "";
                txtOrderBy.Text = "";

                strFileName = openFileDialog1.FileName;
                // Save the query name ready for future saves
                m_strSaveFile = FileFunctions.GetFileName(strFileName);

                StreamReader qryFile = new StreamReader(strFileName);
                // read query
                string qryLine = "";
                while ((qryLine = qryFile.ReadLine()) != null)
                {
                    if (qryLine.Length > 7 && qryLine.Substring(0, 8).ToUpper() == "FIELDS {" && qryLine.ToUpper() != "FIELDS {}")
                    {
                        qryLine = qryLine.Substring(8, qryLine.Length - 9);
                        txtColumns.Text = qryLine.Replace("$$", "\r\n");
                    }
                    else if (qryLine.Length > 6 && qryLine.Substring(0, 7).ToUpper() == "WHERE {" && qryLine.ToUpper() != "WHERE {}")
                    {
                        qryLine = qryLine.Substring(7, qryLine.Length - 8);
                        txtWhere.Text = qryLine.Replace("$$", "\r\n");
                    }
                    else if (qryLine.Length > 9 && qryLine.Substring(0, 10).ToUpper() == "GROUP BY {" && qryLine.ToUpper() != "GROUP BY {}")
                    {
                        qryLine = qryLine.Substring(10, qryLine.Length - 11);
                        txtGroupBy.Text = qryLine.Replace("$$", "\r\n");
                    }
                    else if (qryLine.Length > 9 && qryLine.Substring(0, 10).ToUpper() == "ORDER BY {" && qryLine.ToUpper() != "ORDER BY {}")
                    {
                        qryLine = qryLine.Substring(10, qryLine.Length - 11);
                        txtOrderBy.Text = qryLine.Replace("$$", "\r\n");
                    }
                }
                qryFile.Close();
                qryFile.Dispose();

            }


        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;

            // Run the query. Everything else is allowed to be null.
            string sDefaultSchema = myConfig.GetDatabaseSchema();
            string sTableName = lstTables.Text;
            string sColumnNames = txtColumns.Text;
            string sWhereClause = txtWhere.Text;
            string sGroupClause = txtGroupBy.Text;
            string sOrderClause = txtOrderBy.Text;
            string sOutputFormat = cmbOutFormat.Text;
            string sOutputFile;
            // fix any illegal characters in the user name string
            string sUserID = myStringFuncs.StripIllegals(Environment.UserName, "_", false);

            string strLogFile = myConfig.GetLogFilePath() + @"\DataSelector_" + sUserID + ".log";
            if (chkLogFile.Checked)
            {
                bool blDeleted = FileFunctions.DeleteFile(strLogFile);
                if (!blDeleted)
                {
                    MessageBox.Show("Cannot delete log file. Please make sure it is not open in another window");
                    return;
                }
                //FileFunctions.CreateLogFile(strLogFile);
            }
            FileFunctions.WriteLine(strLogFile, "-----------------------------------------------------------------------");
            FileFunctions.WriteLine(strLogFile, "Process started");
            FileFunctions.WriteLine(strLogFile, "-----------------------------------------------------------------------");

            myArcMapFuncs.ToggleDrawing();
            myArcMapFuncs.ToggleTOC();


            // Do some basic checks and fix as required.
            // User ID should be something at least
            if (string.IsNullOrEmpty(sUserID))
            {
                sUserID = "Temp";
                FileFunctions.WriteLine(strLogFile, "Please note user ID is 'Temp'");
            }


            if (string.IsNullOrEmpty(sColumnNames))
            {
                myArcMapFuncs.ToggleDrawing();
                myArcMapFuncs.ToggleTOC();
                MessageBox.Show("Please specify which columns you wish to select");
                this.BringToFront();
                this.Cursor = Cursors.Default;
                return;
            }

            // Table name should always be selected
            if (string.IsNullOrEmpty(sTableName))
            {
                myArcMapFuncs.ToggleDrawing();
                myArcMapFuncs.ToggleTOC();
                MessageBox.Show("Please select a table to query from");
                this.BringToFront();
                this.Cursor = Cursors.Default;

                return;
            }

            SqlConnection dbConn = mySQLServerFuncs.CreateSQLConnection(myConfig.GetConnectionString());


            // Decide whether or not there is a geometry field in the returned data.
            // Select the stored procedure accordingly
            string[] strGeometryFields = { "SP_GEOMETRY", "Shape" }; // Expand as required.
            bool blSpatial = false;
            foreach (string strField in strGeometryFields)
            {
                if (sColumnNames.ToLower().Contains(strField.ToLower()))
                    blSpatial = true;
            }

            // If "*" is used check for the existence of a geometry field in the table.
            if (sColumnNames == "*")
            {
                string strCheckTable = sDefaultSchema + "." + sTableName;
                dbConn.Open();
                foreach (string strField in strGeometryFields)
                {
                    if (mySQLServerFuncs.FieldExists(ref dbConn, strCheckTable, strField))
                        blSpatial = true;
                }
                dbConn.Close();
            }

            // Set the temporary table names and the stored procedure names. Adjust output formats if required.
            bool blFlatTable = !blSpatial; // to start with
            string strStoredProcedure = "AFSelectSppSubset"; // Default for all data
            string strPolyFC = sDefaultSchema + "." + sTableName + "_poly_" + sUserID; ;
            string strPointFC = sDefaultSchema + "." + sTableName + "_point_" + sUserID;
            string strTable = sDefaultSchema + "." + sTableName + "_" + sUserID;
            string strSplit = "0";

            if (blSpatial)
            {
                strSplit = "1";
                if (sOutputFormat == "Geodatabase") sOutputFormat = "Geodatabase FC";
            }
            else
            {
                if (sOutputFormat == "Geodatabase") sOutputFormat = "Geodatabase Table";
                if (sOutputFormat == "Shapefile") sOutputFormat = "dBASE file";
                FileFunctions.WriteLine(strLogFile, "Output is not spatial. Output file type is " + sOutputFormat);
            }

            // Get the output file name taking account of adjusted output formats.
            sOutputFile = "None";
            bool blDone = false;
            bool blHasExtension = false;
            while (!blDone)
            {
                sOutputFile = myArcMapFuncs.GetOutputFileName(sOutputFormat, myConfig.GetDefaultExtractPath());
                if (sOutputFile != "None")
                {
                    // firstly check extensions are as should be.
                    string strExtensionTest = sOutputFile.Substring(sOutputFile.Length - 4, 4).Substring(0, 1);
                    if (strExtensionTest == ".") blHasExtension = true;

                    // if there isn't, put one one.
                    if (sOutputFormat == "CSV file" && !blHasExtension)
                    {
                        sOutputFile = sOutputFile + ".csv";
                    }
                    else if (sOutputFormat == "dBASE file" && !blHasExtension)
                    {
                        sOutputFile = sOutputFile + ".dbf";
                    }
                    else if (sOutputFormat.Contains("Text file") & !blHasExtension)
                    {
                        sOutputFile = sOutputFile + ".txt";
                    }
                    else if (sOutputFormat == "Shapefile" && !blHasExtension)
                    {
                        sOutputFile = sOutputFile + ".shp";
                    }
                    else if ((sOutputFormat.Contains("Geodatabase")) && (blHasExtension || !sOutputFile.Contains(".gdb"))) // It is a geodatabase file and should not have an extension.
                    {
                        MessageBox.Show("Please select a file geodatabase output file");
                        myArcMapFuncs.ToggleDrawing();
                        myArcMapFuncs.ToggleTOC();
                        this.Cursor = Cursors.Default;
                        this.BringToFront();
                        return;
                    }
                    else if ((!sOutputFormat.Contains("Geodatabase")) && (sOutputFile.Contains(".gdb"))) // Trying to store a non-geoDB in a gdb
                    {
                        MessageBox.Show("Cannot store " + sOutputFormat + " inside a geodatabase. Please choose a different output location");
                        myArcMapFuncs.ToggleDrawing();
                        myArcMapFuncs.ToggleTOC();
                        this.Cursor = Cursors.Default;
                        this.BringToFront();
                        return;
                    }
                }
                else
                    blDone = true; // user pressed cancel.


                if (blSpatial && blDone != true)
                {
                    // Check if the outputfile_point or outputfile_poly already exists. For dBase and text output the dialog does its own check.

                    string sTest1 = "";
                    string sTest2 = "";
                    if (sOutputFormat.Contains("Geodatabase"))
                    {
                        sTest1 = sOutputFile + "_Point";
                        sTest2 = sOutputFile + "_Poly";

                    }
                    else if (sOutputFormat == "Shapefile")
                    {
                        string strExtensionTest1 = sOutputFile.Substring(sOutputFile.Length - 4, 4).Substring(0, 1);
                        if (strExtensionTest1 == ".")
                        {
                            sTest1 = sOutputFile.Substring(0, sOutputFile.Length - 4) + "_Point.shp";
                            sTest2 = sOutputFile.Substring(0, sOutputFile.Length - 4) + "_Poly.shp";
                        }
                        else
                        {
                            sTest1 = sOutputFile + "_Point.shp";
                            sTest2 = sOutputFile + "_Poly.shp";
                        }
                    }
                    if (sOutputFormat.Contains("Geodatabase") || sOutputFormat == "Shapefile")
                    {
                        if (myArcMapFuncs.FeatureclassExists(sTest1) || myArcMapFuncs.FeatureclassExists(sTest2))
                        {
                            DialogResult dlResult1 = MessageBox.Show("The output file already exists. Do you want to overwrite it?", "Data Selector", MessageBoxButtons.YesNo);
                            if (dlResult1 == System.Windows.Forms.DialogResult.Yes)
                                blDone = true;
                        }
                        else
                            blDone = true;
                    }
                    else
                    {
                        // Check for dBase and CSV
                        if (sOutputFormat == "dBASE file" || ((sOutputFormat == "CSV file" || sOutputFormat.Contains("Text file")))) //& !blHasExtension
                        // Basically if the user chose a text file with an extension, the dialog will already have given her feedback and we don't need to do this again.
                        {
                            if (FileFunctions.FileExists(sOutputFile))
                            {
                                DialogResult dlResult1 = MessageBox.Show("The output file already exists. Do you want to overwrite it?", "Data Selector", MessageBoxButtons.YesNo);
                                if (dlResult1 == System.Windows.Forms.DialogResult.Yes)
                                    blDone = true;
                            }
                            else
                                blDone = true;
                        }
                        else
                            blDone = true; // Text file; already checked by dialog.

                    }
                }
                else if (blDone != true) // non-spatial, not done yet.
                {
                    // Test for the types of flat output.
                    if (sOutputFormat.Contains("Geodatabase"))
                    {
                        if (myArcMapFuncs.TableExists(sOutputFile))
                        {
                            DialogResult dlResult1 = MessageBox.Show("The output file already exists. Do you want to overwrite it?", "Data Selector", MessageBoxButtons.YesNo);
                            if (dlResult1 == System.Windows.Forms.DialogResult.Yes)
                                blDone = true;
                        }
                        else
                            blDone = true;
                    }
                    else if (sOutputFormat == "dBASE file" || ((sOutputFormat == "CSV file" || sOutputFormat.Contains("Text file")) & !blHasExtension))
                    // Basically if the user chose a text file, the dialog will already have given her feedback and we don't need to do this again.
                    {
                        if (FileFunctions.FileExists(sOutputFile))
                        {
                            DialogResult dlResult1 = MessageBox.Show("The output file already exists. Do you want to overwrite it?", "Data Selector", MessageBoxButtons.YesNo);
                            if (dlResult1 == System.Windows.Forms.DialogResult.Yes)
                                blDone = true;
                        }
                        else
                            blDone = true;
                    }
                    else
                        blDone = true; // Text file; already checked by dialog.

                }

            }
            this.BringToFront();

            if (sOutputFile == "None")
            {
                // User has pressed Cancel. Bring original menu to the front.
                MessageBox.Show("Please select an output file");
                myArcMapFuncs.ToggleDrawing();
                myArcMapFuncs.ToggleTOC();
                this.Cursor = Cursors.Default;
                return;
            }
            this.Focus();

            FileFunctions.WriteLine(strLogFile, "Output format is " + sOutputFormat);
            FileFunctions.WriteLine(strLogFile, "Output file is " + sOutputFile);
            FileFunctions.WriteLine(strLogFile, "Note that spatial output (Shapefile, Geodatabase) is split into _point and _poly components");

            ////////////////////////////////////////////////////// INPUT ALL CHECKED AND OK, START PROCESS ////////////////////////////////////////////////////////


            string strLayerName = FileFunctions.GetFileName(sOutputFile);

            if (!sOutputFormat.Contains("Geodatabase"))
            {
                strLayerName = FileFunctions.ReturnWithoutExtension(strLayerName);
            }

            // Now we are all set to go - do the process.
            // Set up all required parameters.
            //SqlConnection dbConn = myADOFuncs.CreateSQLConnection(myConfig.GetConnectionString());
            SqlCommand sqlCmd = null;

            int iTimeOutSeconds = myConfig.GetTimeoutSeconds();
            if (iTimeOutSeconds == 0)
            {
                // No timeout given so go with default.
                sqlCmd = mySQLServerFuncs.CreateSQLCommand(ref dbConn, strStoredProcedure, CommandType.StoredProcedure); // Note pass connection by ref here.
            }
            else
            {
                sqlCmd = mySQLServerFuncs.CreateSQLCommand(ref dbConn, strStoredProcedure, CommandType.StoredProcedure, iTimeOutSeconds);
            }

            mySQLServerFuncs.AddSQLParameter(ref sqlCmd, "Schema", sDefaultSchema);
            mySQLServerFuncs.AddSQLParameter(ref sqlCmd, "SpeciesTable", sTableName);
            mySQLServerFuncs.AddSQLParameter(ref sqlCmd, "ColumnNames", sColumnNames);
            mySQLServerFuncs.AddSQLParameter(ref sqlCmd, "WhereClause", sWhereClause);
            mySQLServerFuncs.AddSQLParameter(ref sqlCmd, "GroupByClause", sGroupClause);
            mySQLServerFuncs.AddSQLParameter(ref sqlCmd, "OrderByClause", sOrderClause);
            mySQLServerFuncs.AddSQLParameter(ref sqlCmd, "UserID", sUserID);
            mySQLServerFuncs.AddSQLParameter(ref sqlCmd, "Split", strSplit);

            FileFunctions.WriteLine(strLogFile, "Species table is " + sTableName);
            FileFunctions.WriteLine(strLogFile, "Column names are " + sColumnNames.Replace("\r\n", " "));
            if (sWhereClause.Length > 0)
                FileFunctions.WriteLine(strLogFile, "Where clause is " + sWhereClause.Replace("\r\n", " "));
            else
                FileFunctions.WriteLine(strLogFile, "No where clause was used");
            if (sGroupClause.Length > 0)
                FileFunctions.WriteLine(strLogFile, "Group by clause is " + sGroupClause.Replace("\r\n", " "));
            else
                FileFunctions.WriteLine(strLogFile, "No group by clause was used");
            if (sOrderClause.Length > 0)
                FileFunctions.WriteLine(strLogFile, "Order by clause is " + sOrderClause.Replace("\r\n", " "));
            else
                FileFunctions.WriteLine(strLogFile, "No order by clause was used");
            if (strSplit == "1")
                FileFunctions.WriteLine(strLogFile, "Data is spatial and will be split into a point and a polygon layer");
            else
                FileFunctions.WriteLine(strLogFile, "Data is not spatial and will not be split");


            // Open SQL connection to database and
            // Run the stored procedure.
            bool blSuccess = true;
            int intCount = 0;
            int intPolyCount = 0;
            int intPointCount = 0;
            try
            {
                FileFunctions.WriteLine(strLogFile, "Opening SQL Connection");
                dbConn.Open();
                FileFunctions.WriteLine(strLogFile, "Executing stored procedure");
                string strRowsAffect = sqlCmd.ExecuteNonQuery().ToString();
                if (blSpatial)
                {
                    blSuccess = mySQLServerFuncs.TableHasRows(ref dbConn, strPointFC);
                    if (!blSuccess)
                        blSuccess = mySQLServerFuncs.TableHasRows(ref dbConn, strPolyFC);
                }
                else
                    blSuccess = mySQLServerFuncs.TableHasRows(ref dbConn, strTable);

                if (blSuccess && blSpatial)
                {
                    intPolyCount = mySQLServerFuncs.CountRows(ref dbConn, strPolyFC);
                    intPointCount = mySQLServerFuncs.CountRows(ref dbConn, strPointFC);
                    FileFunctions.WriteLine(strLogFile, "Procedure returned " + intPointCount.ToString() + " point and " + intPolyCount.ToString() +
                        " polygon records");
                }
                else if (blSuccess)
                {
                    intCount = mySQLServerFuncs.CountRows(ref dbConn, strTable);
                    FileFunctions.WriteLine(strLogFile, "Procedure returned " + intCount.ToString() + " records");
                }


                FileFunctions.WriteLine(strLogFile, "Closing SQL Connection");
                dbConn.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not execute stored procedure. System returned the following message: " +
                    ex.Message);
                FileFunctions.WriteLine(strLogFile, "Could not execute stored procedure. System returned the following message: " +
                    ex.Message);
                this.Cursor = Cursors.Default;
                dbConn.Close();
                myArcMapFuncs.ToggleDrawing();
                myArcMapFuncs.ToggleTOC();
                this.BringToFront();
                return;
            }

            // convert the results to the designated output file.
            string strPointOutTab = myConfig.GetSDEName() + @"\" + strPointFC;
            string strPolyOutTab = myConfig.GetSDEName() + @"\" + strPolyFC;
            string strOutTab = myConfig.GetSDEName() + @"\" + strTable;

            string strOutPoints = "";
            string strOutPolys = "";


            bool blResult = false;
            if (blSpatial && blSuccess)
            {

                // export points and polygons
                // How is the data to be exported?
                if (sOutputFormat == "Geodatabase FC")
                {
                    // Easy, export without further ado.
                    strOutPoints = sOutputFile + "_Point";
                    strOutPolys = sOutputFile + "_Poly";

                    if (intPointCount > 0)
                    {
                        FileFunctions.WriteLine(strLogFile, "Copying point results to point geodatabase file");
                        blResult = myArcMapFuncs.CopyFeatures(strPointOutTab, strOutPoints);
                        if (!blResult)
                        {
                            MessageBox.Show("Error exporting point geodatabase file");
                            FileFunctions.WriteLine(strLogFile, "Error exporting point geodatabase file");
                            this.Cursor = Cursors.Default;
                            myArcMapFuncs.ToggleDrawing();
                            myArcMapFuncs.ToggleTOC();
                            this.BringToFront();
                            return;
                        }
                    }
                    if (intPolyCount > 0)
                    {
                        blResult = myArcMapFuncs.CopyFeatures(strPolyOutTab, strOutPolys);
                        FileFunctions.WriteLine(strLogFile, "Copying polygon results to polygon geodatabase file");
                        if (!blResult)
                        {
                            MessageBox.Show("Error exporting polygon geodatabase file");
                            FileFunctions.WriteLine(strLogFile, "Error exporting polygon geodatabase file");
                            this.Cursor = Cursors.Default;
                            myArcMapFuncs.ToggleDrawing();
                            myArcMapFuncs.ToggleTOC();
                            this.BringToFront();
                            return;
                        }
                    }

                }
                else if (sOutputFormat == "Shapefile" & blSuccess)
                {
                    // Create file names first.
                    sOutputFile = FileFunctions.ReturnWithoutExtension(sOutputFile);
                    strOutPoints = sOutputFile + "_Point.shp";
                    strOutPolys = sOutputFile + "_Poly.shp";

                    if (intPointCount > 0)
                    {
                        FileFunctions.WriteLine(strLogFile, "Copying point results to point shapefile");
                        blResult = myArcMapFuncs.CopyFeatures(strPointOutTab, strOutPoints);
                        if (!blResult)
                        {
                            MessageBox.Show("Error exporting point shapefile");
                            FileFunctions.WriteLine(strLogFile, "Error exporting point shapefile");
                            this.Cursor = Cursors.Default;
                            myArcMapFuncs.ToggleDrawing();
                            myArcMapFuncs.ToggleTOC();
                            this.BringToFront();
                            return;
                        }
                    }
                    if (intPolyCount > 0)
                    {
                        FileFunctions.WriteLine(strLogFile, "Copying polygon results to polygon shapefile");
                        blResult = myArcMapFuncs.CopyFeatures(strPolyOutTab, strOutPolys);
                        if (!blResult)
                        {
                            MessageBox.Show("Error exporting polygon shapefile");
                            FileFunctions.WriteLine(strLogFile, "Error exporting polygon shapefile");
                            this.Cursor = Cursors.Default;
                            myArcMapFuncs.ToggleDrawing();
                            myArcMapFuncs.ToggleTOC();
                            this.BringToFront();
                            return;
                        }
                    }
                }

                else if (sOutputFormat.Contains("Text file"))
                {
                    // Not a spatial export, but it is a spatial layer so there are two files.
                    // Function pulls them back together again.

                    // if schema.ini file exists delete it.
                    string strIniFile = FileFunctions.GetDirectoryName(sOutputFile) + "\\schema.ini";
                    if (FileFunctions.FileExists(strIniFile))
                    {
                        bool blDeleted = FileFunctions.DeleteFile(strIniFile); // Not checking for success at the moment.
                    }
                    blFlatTable = true;
                    bool blAppend = false;
                    if (intPointCount > 0)
                    {
                        FileFunctions.WriteLine(strLogFile, "Copying point results to text file");
                        blResult = myArcMapFuncs.CopyToTabDelimitedFile(strPointOutTab, sOutputFile, true, false, true);
                        if (!blResult)
                        {
                            MessageBox.Show("Error exporting output table to text file " + sOutputFile);
                            FileFunctions.WriteLine(strLogFile, "Error exporting output table to text file " + sOutputFile);
                            this.Cursor = Cursors.Default;
                            myArcMapFuncs.ToggleDrawing();
                            myArcMapFuncs.ToggleTOC();
                            this.BringToFront();
                            return;
                        }
                        blAppend = true;
                    }
                    // Also export the second table - append if necessary
                    if (intPolyCount > 0)
                    {
                        FileFunctions.WriteLine(strLogFile, "exporting polygon results to text file");
                        blResult = myArcMapFuncs.CopyToTabDelimitedFile(strPolyOutTab, sOutputFile, true, blAppend, true);
                        if (!blResult)
                        {
                            MessageBox.Show("Error appending output table to text file " + sOutputFile);
                            FileFunctions.WriteLine(strLogFile, "Error appending output table to text file " + sOutputFile);
                            this.Cursor = Cursors.Default;
                            myArcMapFuncs.ToggleDrawing();
                            myArcMapFuncs.ToggleTOC();
                            this.BringToFront();
                        }
                    }
                    // Add the output to ArcMap
                    FileFunctions.WriteLine(strLogFile, "Adding output to ArcMap view");
                    myArcMapFuncs.AddTableLayerFromString(sOutputFile, strLayerName);
                }

                else if (sOutputFormat == "CSV file" || sOutputFormat == "dBASE file")
                {
                    // Not a spatial export, but it is a spatial layer so there are two files.
                    // Function pulls them back together again.

                    // if schema.ini file exists delete it.
                    string strIniFile = FileFunctions.GetDirectoryName(sOutputFile) + "\\schema.ini";
                    if (FileFunctions.FileExists(strIniFile))
                    {
                        bool blDeleted = FileFunctions.DeleteFile(strIniFile); // Not checking for success at the moment.
                    }

                    blFlatTable = true;
                    string sFinalFile = "";
                    if (sOutputFormat == "dBASE file")
                    {
                        sFinalFile = sOutputFile;
                        sOutputFile = FileFunctions.GetDirectoryName(sOutputFile) + "\\Temp.csv";
                    }
                    bool blAppend = false;
                    if (intPointCount > 0)
                    {
                        FileFunctions.WriteLine(strLogFile, "Copying point results to CSV file");
                        blResult = myArcMapFuncs.CopyToCSV(strPointOutTab, sOutputFile, true, false, true);
                        if (!blResult)
                        {
                            MessageBox.Show("Error exporting output table to CSV file " + sOutputFile);
                            FileFunctions.WriteLine(strLogFile, "Error exporting output table to CSV file " + sOutputFile);
                            this.Cursor = Cursors.Default;
                            myArcMapFuncs.ToggleDrawing();
                            myArcMapFuncs.ToggleTOC();
                            this.BringToFront();
                            return;
                        }
                        blAppend = true;
                    }
                    // Also export the second table - append if necessary.
                    if (intPolyCount > 0)
                    {
                        FileFunctions.WriteLine(strLogFile, "Appending polygon results to CSV file");
                        blResult = myArcMapFuncs.CopyToCSV(strPolyOutTab, sOutputFile, true, blAppend, true);
                        if (!blResult)
                        {
                            MessageBox.Show("Error appending output table to CSV file " + sOutputFile);
                            FileFunctions.WriteLine(strLogFile, "Error appending output table to CSV file " + sOutputFile);
                            this.Cursor = Cursors.Default;
                            myArcMapFuncs.ToggleDrawing();
                            myArcMapFuncs.ToggleTOC();
                            this.BringToFront();
                        }
                    }

                    // If the end output is a dBASE file, export the resulting csv to dBASE.
                    if (sOutputFormat == "dBASE file")
                    {
                        FileFunctions.WriteLine(strLogFile, "Converting text file to dBASE file");
                        blResult = myArcMapFuncs.CopyTable(sOutputFile, sFinalFile);
                        // Delete csv file.
                        try
                        {
                            FileFunctions.WriteLine(strLogFile, "Deleting temporary CSV file");
                            File.Delete(sOutputFile);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Error deleting temporary CSV file: " + ex.Message);
                            FileFunctions.WriteLine(strLogFile, "Error deleting temporary CSV file: " + ex.Message);
                            this.Cursor = Cursors.Default;
                            myArcMapFuncs.ToggleDrawing();
                            myArcMapFuncs.ToggleTOC();
                            this.BringToFront();
                        }
                        if (!blResult)
                        {
                            MessageBox.Show("Error exporting output table to dBASE file " + sFinalFile);
                            FileFunctions.WriteLine(strLogFile, "Error exporting output table to dBASE file " + sFinalFile);
                            this.Cursor = Cursors.Default;
                            myArcMapFuncs.ToggleDrawing();
                            myArcMapFuncs.ToggleTOC();
                            this.BringToFront();
                            return;
                        }
                        sOutputFile = sFinalFile;
                    }
                    else
                    {
                        FileFunctions.WriteLine(strLogFile, "Adding output to ArcMap view");
                        myArcMapFuncs.AddTableLayerFromString(sOutputFile, strLayerName);
                    }
                }
            }
            else if (blSuccess) // Non-spatial query, successfully run.
            {
                if (sOutputFormat == "CSV file")
                {
                    // We are exporting a non-spatial output to CSV file.
                    FileFunctions.WriteLine(strLogFile, "Copying results to CSV file");
                    blResult = myArcMapFuncs.CopyToCSV(strOutTab, sOutputFile, false, false, true);
                    if (!blResult)
                    {
                        MessageBox.Show("Error exporting output table to CSV file " + sOutputFile);
                        FileFunctions.WriteLine(strLogFile, "Error exporting output table to CSV file " + sOutputFile);
                        this.Cursor = Cursors.Default;
                        myArcMapFuncs.ToggleDrawing();
                        myArcMapFuncs.ToggleTOC();
                        this.BringToFront();
                        return;
                    }
                    FileFunctions.WriteLine(strLogFile, "Adding output to ArcMap view");
                    myArcMapFuncs.AddTableLayerFromString(sOutputFile, strLayerName);
                }

                else if (sOutputFormat.Contains("Text file"))
                {
                    // We are exporting a non-spatial output to text file.
                    FileFunctions.WriteLine(strLogFile, "Copying results to text file");
                    blResult = myArcMapFuncs.CopyToTabDelimitedFile(strOutTab, sOutputFile, false, false, true);
                    if (!blResult)
                    {
                        MessageBox.Show("Error exporting output table to text file " + sOutputFile);
                        FileFunctions.WriteLine(strLogFile, "Error exporting output table to text file " + sOutputFile);
                        this.Cursor = Cursors.Default;
                        myArcMapFuncs.ToggleDrawing();
                        myArcMapFuncs.ToggleTOC();
                        this.BringToFront();
                        return;
                    }
                    FileFunctions.WriteLine(strLogFile, "Adding output to ArcMap view");
                    myArcMapFuncs.AddTableLayerFromString(sOutputFile, strLayerName);
                }
                else
                {
                    // We are exporting any non-spatial output to dbf or geodatabase.
                    blResult = myArcMapFuncs.CopyTable(strOutTab, sOutputFile);
                    if (!blResult)
                    {
                        MessageBox.Show("Error exporting output table");
                        FileFunctions.WriteLine(strLogFile, "Error exporting output table");
                        this.Cursor = Cursors.Default;
                        myArcMapFuncs.ToggleDrawing();
                        myArcMapFuncs.ToggleTOC();
                        this.BringToFront();
                        return;
                    }
                }
            }
            else if (!blSuccess)
            {
                if (sOutputFormat == "CSV file")
                {
                    if (sColumnNames == "*")
                    {
                        dbConn.Open();
                        string[] strColumnNames = mySQLServerFuncs.GetFieldNames(ref dbConn, sTableName);
                        dbConn.Close();
                        sColumnNames = "";
                        foreach (string strField in strColumnNames)
                        {
                            sColumnNames = sColumnNames + strField + ",";
                        }
                        // Remove last comma
                        sColumnNames = sColumnNames.Substring(0, sColumnNames.Length - 1);
                        myArcMapFuncs.WriteEmptyCSV(sOutputFile, sColumnNames);
                        MessageBox.Show("There were no results for the query. An empty text file has been created");
                        FileFunctions.WriteLine(strLogFile, "There were no results for the query. An empty text file has been created");
                    }
                }
                else
                {
                    MessageBox.Show("There were no results for this query. No output has been created");
                    FileFunctions.WriteLine(strLogFile, "There were no results for the query. No output has been created");
                }
            }

            // Delete the temporary tables in the SQL database
            strStoredProcedure = "AFClearSppSubset";
            SqlCommand sqlCmd2 = mySQLServerFuncs.CreateSQLCommand(ref dbConn, strStoredProcedure, CommandType.StoredProcedure); // Note pass connection by ref here.
            mySQLServerFuncs.AddSQLParameter(ref sqlCmd2, "Schema", sDefaultSchema);
            mySQLServerFuncs.AddSQLParameter(ref sqlCmd2, "SpeciesTable", sTableName);
            mySQLServerFuncs.AddSQLParameter(ref sqlCmd2, "UserId", sUserID);
            try
            {
                FileFunctions.WriteLine(strLogFile, "Opening SQL connection");
                dbConn.Open();
                FileFunctions.WriteLine(strLogFile, "Deleting temporary tables");
                string strRowsAffect = sqlCmd2.ExecuteNonQuery().ToString();
                FileFunctions.WriteLine(strLogFile, "Closing SQL connection");
                dbConn.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not execute stored procedure. System returned the following message: " +
                    ex.Message);
                FileFunctions.WriteLine(strLogFile, "Could not execute stored procedure. System returned the following message: " +
                    ex.Message);
                this.Cursor = Cursors.Default;
                myArcMapFuncs.ToggleDrawing();
                myArcMapFuncs.ToggleTOC();
                this.BringToFront();
                return;
            }

            // Move the results to the right location or show as appropriate.
            if (!blFlatTable && blSuccess) // Only truly spatial output has two files.
            {
                FileFunctions.WriteLine(strLogFile, "Adding output to ArcMap project in group layer " + strLayerName);
                if (intPointCount > 0)
                {
                    ILayer lyrPoints = myArcMapFuncs.GetLayer(strLayerName + "_Point");
                    myArcMapFuncs.MoveToGroupLayer(strLayerName, lyrPoints);
                }
                if (intPolyCount > 0)
                {
                    ILayer lyrPolys = myArcMapFuncs.GetLayer(strLayerName + "_Poly");
                    myArcMapFuncs.MoveToGroupLayer(strLayerName, lyrPolys);
                }

            }
            else if (blSuccess)
            {
                FileFunctions.WriteLine(strLogFile, "Showing table output on screen");
                myArcMapFuncs.ShowTable(strLayerName);
            }

            FileFunctions.WriteLine(strLogFile, "---------------------------------------------------------------------------");
            FileFunctions.WriteLine(strLogFile, "Process complete");
            FileFunctions.WriteLine(strLogFile, "---------------------------------------------------------------------------");

            this.Cursor = Cursors.Default;
            myArcMapFuncs.ToggleTOC();
            if (!blFlatTable && blSuccess)
            {
                myArcMapFuncs.ZoomToFullExtent();
            }
            myArcMapFuncs.ToggleDrawing();
            DialogResult dlResult = MessageBox.Show("Process complete. Do you wish to close the form?", "Data Selector", MessageBoxButtons.YesNo);
            if (dlResult == System.Windows.Forms.DialogResult.Yes)
                this.Close();
            else this.BringToFront();

            Process.Start("notepad.exe", strLogFile);

            // Tidy up
            sqlCmd.Dispose();
            sqlCmd2.Dispose();
            dbConn.Dispose();


        }

        private void lstTables_DoubleClick(object sender, EventArgs e)
        {
            // On double click, the columns belonging to the table name that was double clicked are filled in
            // in the Columns textbox.
            // Hester, 19 July 2017.
            if (lstTables.SelectedItem != null)
            {
                if (txtColumns.Text != "")
                {
                    DialogResult dlResult1 = MessageBox.Show("There is already text in the Column field. Do you want to overwrite it?", "Data Selector", MessageBoxButtons.YesNo);
                    if (dlResult1 == System.Windows.Forms.DialogResult.No)
                        return; //User clicked by accident; leave routine.
                }
                string strSelectedTable = lstTables.SelectedItem.ToString();
                // Open the connection, get the relevant field names as array.
                SqlConnection dbConn = mySQLServerFuncs.CreateSQLConnection(myConfig.GetConnectionString());
                dbConn.Open();
                string[] strFieldNames = mySQLServerFuncs.GetFieldNames(ref dbConn, strSelectedTable);
                dbConn.Close();
                // Convert the field names to a single string.
                string strFieldNamesText = "";
                foreach (string strFieldName in strFieldNames)
                {
                    strFieldNamesText = strFieldNamesText + strFieldName + ",\r\n";
                }
                strFieldNamesText = strFieldNamesText.Substring(0, strFieldNamesText.Length - 3);
                // Add the string to the text box.
                txtColumns.Text = strFieldNamesText;
            }

        }

    }
}
