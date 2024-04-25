/*

   Copyright 2018 Esri

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.

   See the License for the specific language governing permissions and
   limitations under the License.

*/
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
//using ArcGIS.Core.Internal.CIM;
using ArcGIS.Desktop.Framework.Threading.Tasks;

namespace DataTools
{
    /// <summary>
    /// Illustrates how to get a Dataset's Definition from a geodatabase.
    /// </summary>
    /// 
    /// <remarks>
    /// <para>
    /// While it is true classes that are derived from the <see cref="ArcGIS.Core.CoreObjectsBase"/> super class 
    /// consumes native resources (e.g., <see cref="ArcGIS.Core.Data.Geodatabase"/> or <see cref="ArcGIS.Core.Data.FeatureClass"/>), 
    /// you can rest assured that the garbage collector will properly dispose of the unmanaged resources during 
    /// finalization.  However, there are certain workflows that require a <b>deterministic</b> finalization of the 
    /// <see cref="ArcGIS.Core.Data.Geodatabase"/>.  Consider the case of a file geodatabase that needs to be deleted 
    /// on the fly at a particular moment.  Because of the <b>indeterministic</b> nature of garbage collection, we can't
    /// count on the garbage collector to dispose of the Geodatabase object, thereby removing the <b>lock(s)</b> at the  
    /// moment we want. To ensure a deterministic finalization of important native resources such as a 
    /// <see cref="ArcGIS.Core.Data.Geodatabase"/> or <see cref="ArcGIS.Core.Data.FeatureClass"/>, you should declare 
    /// and instantiate said objects in a <b>using</b> statement.  Alternatively, you can achieve the same result by 
    /// putting the object inside a try block and then calling Dispose() in a finally block.
    /// </para>
    /// <para>
    /// In general, you should always call Dispose() on the following types of objects: 
    /// </para>
    /// <para>
    /// - Those that are derived from <see cref="ArcGIS.Core.Data.Datastore"/> (e.g., <see cref="ArcGIS.Core.Data.Geodatabase"/>).
    /// </para>
    /// <para>
    /// - Those that are derived from <see cref="ArcGIS.Core.Data.Dataset"/> (e.g., <see cref="ArcGIS.Core.Data.Table"/>).
    /// </para>
    /// <para>
    /// - <see cref="ArcGIS.Core.Data.RowCursor"/> and <see cref="ArcGIS.Core.Data.RowBuffer"/>.
    /// </para>
    /// <para>
    /// - <see cref="ArcGIS.Core.Data.Row"/> and <see cref="ArcGIS.Core.Data.Feature"/>.
    /// </para>
    /// <para>
    /// - <see cref="ArcGIS.Core.Data.Selection"/>.
    /// </para>
    /// <para>
    /// - <see cref="ArcGIS.Core.Data.VersionManager"/> and <see cref="ArcGIS.Core.Data.Version"/>.
    /// </para>
    /// </remarks>
    public class SQLServerFunctions
    {
        private readonly Task _initializeTask;

        readonly string _sdeFileName;
        List<String> _tableNames;
        List<String> _fieldNames;

        Geodatabase _geodatabase = null;

        #region Constructor

        public SQLServerFunctions(string sdeFileName)
        {
            _sdeFileName = sdeFileName;
        }

        #endregion

        #region Properties

        public Task InitializeTask
        {
            get { return _initializeTask; }
        }

        public List<String> TableNames
        {
            get { return _tableNames; }
        }

        public List<String> FieldNames
        {
            get { return _fieldNames; }
            set { _fieldNames = value; }
        }

        #endregion

        #region Geodatabase

        /// <summary>
        /// Get all of the feature class and table names from the geodatabase.
        /// </summary>
        /// <returns></returns>
        public async Task GeodatabaseGetTableNamesAsync()
        {
            // Open a connection to the geodatabase if not already open.
            _geodatabase ??= await OpenGeodatabaseUsingSDEFilePath(_sdeFileName);

            _tableNames = new();

            await QueuedTask.Run(() =>
            {
                try
                {
                    // Try and get all of the feature class definitions.
                    IReadOnlyList<FeatureClassDefinition> fcDefinitions = _geodatabase.GetDefinitions<FeatureClassDefinition>();

                    foreach (var definition in fcDefinitions)
                    {
                        // Get the name of the feature class and add it to the list.
                        _tableNames.Add(definition.GetName());
                    }
                }
                catch (Exception)
                {
                    // GetDefinitions throws an exception.
                    throw;
                }
            });

            await QueuedTask.Run(() =>
            {
                try
                {
                    // Try and get all of the table definitions.
                    IReadOnlyList<TableDefinition> tabDefinitions = _geodatabase.GetDefinitions<TableDefinition>();

                    foreach (var definition in tabDefinitions)
                    {
                        // Get the name of the table and add it to the list.
                        _tableNames.Add(definition.GetName());
                    }
                }
                catch (Exception)
                {
                    // GetDefinitions throws an exception.
                    throw;
                }
            });

            return;
        }

        /// <summary>
        /// Get the fieldName names for the specified table in the geodatabase.
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public async Task<List<string>> GetFieldNamesListAsync(string tableName)
        {
            List<string> fieldNames = new();

            // Open a connection to the geodatabase if not already open.
            _geodatabase ??= await OpenGeodatabaseUsingSDEFilePath(_sdeFileName);

            // Open the SQLServer geodatabase via the .sde connection file.
            await QueuedTask.Run(() =>
            {
                try
                {
                    // Get the table definition.
                    using TableDefinition tableDefinition = _geodatabase.GetDefinition<TableDefinition>(tableName);
                    
                    // Get the fields in the table and add them to the list.
                    IReadOnlyList<Field> tableFields = tableDefinition.GetFields();

                    foreach (var field in tableFields)
                    {
                        fieldNames.Add(field.Name);
                    }
                }
                catch
                {
                    // GetDefinition throws an exception if the definition doesn't exist.
                }
            });

            return fieldNames;
        }

        /// <summary>
        /// Get the fields for the specified table in the geodatabase.
        /// </summary>
        /// <param name="fullPath"></param>
        /// <returns></returns>
        public async Task<IReadOnlyList<Field>> GetFieldsAsync(string fullPath)
        {
            string fileName = FileFunctions.GetFileName(fullPath);

            IReadOnlyList<Field> fields = null;

            // Open a connection to the geodatabase if not already open.
            _geodatabase ??= await OpenGeodatabaseUsingSDEFilePath(_sdeFileName);

            // Open the SQLServer geodatabase via the .sde connection file.
            await QueuedTask.Run(() =>
            {
                try
                {
                    // Get the table definition.
                    using TableDefinition tableDefinition = _geodatabase.GetDefinition<TableDefinition>(fileName);

                    // Get the fields in the table.
                    fields = tableDefinition.GetFields();
                }
                catch
                {
                    // GetDefinition throws an exception if the definition doesn't exist.
                }
            });

            return fields;
        }

        /// <summary>
        /// Get the fields for the specified table in the geodatabase.
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public static async Task<IReadOnlyList<Field>> GetFieldNamesAsync2(string filePath, string fileName)
        {
            IReadOnlyList<Field> fields = null;

            try
            {
                // Open the SQLServer geodatabase via the .sde connection file.
                await QueuedTask.Run(() =>
                {
                    // Open the file geodatabase. This will open the geodatabase if the folder exists and contains a valid geodatabase.
                    using Geodatabase geodatabase = new(new FileGeodatabaseConnectionPath(new Uri(filePath)));

                    // Get the feature class definition.
                    using FeatureClassDefinition featureClassDefinition = geodatabase.GetDefinition<FeatureClassDefinition>(fileName);
                    //using TableDefinition tableDefinition = geodatabase.GetDefinition<TableDefinition>(fileName);

                    // Get the fields in the table.
                    IReadOnlyList<Field> tableFields = featureClassDefinition.GetFields();
                });
            }
            catch
            {
                // GetDefinition throws an exception if the definition doesn't exist.
            }

            return fields;
        }

        /// <summary>
        /// Get the fields  for the specified table in the geodatabase.
        /// </summary>
        /// <param name="fullPath"></param>
        /// <returns></returns>
        public static async Task<IReadOnlyList<Field>> GetFieldNamesAsync2(string fullPath)
        {
            return await GetFieldNamesAsync2(FileFunctions.GetDirectoryName(fullPath), FileFunctions.GetFileName(fullPath));
        }

        /// <summary>
        /// Open a SQL Server database using a .sde connection file.
        /// </summary>
        /// <param name="sdeFileName"></param>
        /// <returns></returns>
        public static async Task<Geodatabase> OpenGeodatabaseUsingSDEFilePath(string sdeFileName)
        {
            Geodatabase geodatabase = null;

            await QueuedTask.Run(() =>
            {
                try
                {
                    // Try and open the geodatabase using the connection file.
                    geodatabase = new Geodatabase(new DatabaseConnectionFile(new Uri(sdeFileName)));
                }
                catch (Exception)
                {
                    // Geodatabase throws and exception.
                    throw;
                }
            });

            return geodatabase;
        }

        #endregion

        #region ExecuteSQL

        /// <summary>
        /// Execute a raw SQL on the underlying database management system.
        /// Any SQL is permitted (DDL or DML), but no results can be returned.
        /// </summary>
        /// <param name="sqlStatement"></param>
        public async Task ExecuteSQLOnGeodatabase(string sqlStatement)
        {
            // Open a connection to the geodatabase if not already open.
            _geodatabase ??= await OpenGeodatabaseUsingSDEFilePath(_sdeFileName);

            await QueuedTask.Run(() =>
            {
                try
                {
                    // Try and execute the SQL statement.
                    DatabaseClient.ExecuteStatement(_geodatabase, sqlStatement);
                }
                catch (Exception)
                {
                    // ExecuteStatement throws and exception.
                    throw;
                }
            });
        }


        #endregion

        #region Text Files

        public static bool WriteEmptyTextFile(string outFile, string header)
        {
            // Open the output file.
            using StreamWriter theOutput = new StreamWriter(outFile, false);

            // Write the headers to the file.
            theOutput.Write(header);

            // Close the file.
            theOutput.Close();

            return true;
        }

        public async Task<bool> CopyToCSVAsync(string inTable, string outFile, bool isSpatial, bool append, bool messages = false)
        {
            string separator = ",";
            return await CopyToTextFileAsync(inTable, outFile, separator, isSpatial, false, true);
        }

        public async Task<bool> CopyToTabAsync(string inTable, string outFile, bool isSpatial, bool append, bool messages = false)
        {
            string separator = "\t";
            return await CopyToTextFileAsync(inTable, outFile, separator, isSpatial, false, true);
        }

        public async Task<bool> CopyToTextFileAsync(string inTable, string outFile, string separator, bool isSpatial, bool append, bool messages = false)
        {
            // This sub copies the input table to tab delimited file.
            string filePath = FileFunctions.GetDirectoryName(inTable);
            string fileName = FileFunctions.GetFileName(inTable);

            string fieldName = null;
            string header = "";
            int ignoreField = -1;

            // Get the fields for the input file.
            IReadOnlyList<Field> fields = await GetFieldsAsync(inTable);

            int intFieldCount = fields.Count;

            if (!append)
            {
                // Iterate through the fields in the collection to create header.
                for (int i = 0; i < intFieldCount; i++)
                {
                    // Get the fieldName name.
                    fieldName = fields[i].Name;

                    Field field = fields[i];

                    // Get the fieldName type.
                    FieldType fieldType = field.FieldType;

                    string fieldTypeName = fieldType.ToString();

                    if (fieldName == "SP_GEOMETRY" || fieldName == "Shape")
                        ignoreField = i;
                    else
                        header = header + fieldName + separator;
                }

                // Remove the final separator from the header.
                header = header.Substring(0, header.Length - 1);

                // Write the header to the output file.
                WriteEmptyTextFile(outFile, header);
            }

            // Open output file.
            StreamWriter txtFile = new(outFile, true);

            // Open a connection to the geodatabase if not already open.
            _geodatabase ??= await OpenGeodatabaseUsingSDEFilePath(_sdeFileName);

            string tabName;
            string fcName;

            try
            {
                await QueuedTask.Run(() =>
                {
                    RowCursor rowCursor;

                    if (isSpatial)
                    {
                        // Open the feature class.
                        using FeatureClass featureClass = _geodatabase.OpenDataset<FeatureClass>(fileName);
                        fcName = featureClass.GetName();

                        // Create a cursor on the table.
                        rowCursor = featureClass.Search(null);
                    }
                    else
                    {
                        // Open the table.
                        using Table table = _geodatabase.OpenDataset<Table>(fileName);
                        tabName = table.GetName();

                        // Create a cursor on the table.
                        rowCursor = table.Search(null);
                    }

                    // Loopt through the feature class/table using the cursor.
                    while (rowCursor.MoveNext())
                    {
                        // Get the current row.
                        using Row row = rowCursor.Current;

                        // Loop through the fields.
                        string rowStr = "";
                        for (int i = 0; i < intFieldCount; i++)
                        {
                            // String the column values together (if they are not to be ignored).
                            if (i != ignoreField)
                            {
                                // Get the column value.
                                var colValue = row.GetOriginalValue(i);

                                // Wrap the value if quotes if it is a string that contains a comma
                                string colStr = null;
                                if (colValue != null)
                                {
                                    if ((colValue is string) && (colValue.ToString().Contains(",")))
                                            colStr = "\"" + colValue.ToString() + "\"";
                                    else
                                        colStr = colValue.ToString();
                                }

                                // Add the column string to the row string.
                                rowStr += colStr;

                                // Add the column separator (if not the last column).
                                if (i < intFieldCount - 1)
                                    rowStr += separator;
                            }
                        }

                        // Write the row string to the output file.
                        txtFile.WriteLine(rowStr);
                    }
                });
            }
            catch (Exception exception)
            {
                // logger.Error(exception.Message);
            }

            // Close the output file and dispose of the object.
            txtFile.Close();
            txtFile.Dispose();

            return true;
        }
        #endregion

        #region FeatureClass

        /// <summary>
        /// Check if a feature class exists in the database.
        /// </summary>
        /// <param name="featureClassName"></param>
        /// <returns></returns>
        public async Task<bool> FeatureClassExistsAsync(string featureClassName)
        {
            bool exists = false;

            // Open a connection to the geodatabase if not already open.
            _geodatabase ??= await OpenGeodatabaseUsingSDEFilePath(_sdeFileName);

            // Check to see if the feature class is a feature class.
            await QueuedTask.Run(() =>
            {
                try
                {
                    // Try and get the feature class definition.
                    using FeatureClassDefinition featureClassDefinition = _geodatabase.GetDefinition<FeatureClassDefinition>(featureClassName);

                    exists = true;

                    // Dispose of the feature class definition.
                    //featureClassDefinition.Dispose();
                }
                catch
                {
                    // GetDefinition throws an exception if the definition doesn't exist.
                    exists = false;
                }
            });

            return exists;
        }

        /// <summary>
        /// Count the rows in a feature class within the database.
        /// </summary>
        /// <param name="featureClassName"></param>
        /// <returns></returns>
        public async Task<long> FeatureClassCountRowsAsync(string featureClassName)
        {
            long rows = 0;

            // Open a connection to the geodatabase if not already open.
            _geodatabase ??= await OpenGeodatabaseUsingSDEFilePath(_sdeFileName);

            // Check to see if the table is a feature class.
            await QueuedTask.Run(() =>
            {
                try
                {
                    // Try and get the feature class definition.
                    using FeatureClassDefinition featureClassDefinition = _geodatabase.GetDefinition<FeatureClassDefinition>(featureClassName);

                    // Open the feature class.
                    using FeatureClass featureClass = _geodatabase.OpenDataset<FeatureClass>(featureClassName);

                    // Count the rows in the feature class.
                    rows = featureClass.GetCount();

                    // Dispose of the feature class and definition.
                    //featureClass.Dispose();
                    //featureClassDefinition.Dispose();
                }
                catch
                {
                    // GetDefinition or open dataset throw an exception if the definition doesn't exist.
                    rows = 0;
                }
            });

            return rows;
        }

        #endregion

        #region Table

        /// <summary>
        /// Check if a table exists in the database.
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public async Task<bool> TableExistsAsync(string tableName)
        {
            bool exists = false;

            // Open a connection to the geodatabase if not already open.
            _geodatabase ??= await OpenGeodatabaseUsingSDEFilePath(_sdeFileName);

            await QueuedTask.Run(() =>
            {
                try
                {
                    // Try and get the table definition.
                    using TableDefinition tableDefinition = _geodatabase.GetDefinition<TableDefinition>(tableName);

                    exists = true;

                    // Dispose of the table definition.
                    //tableDefinition.Dispose();
                }
                catch
                {
                    // GetDefinition throws an exception if the definition doesn't exist.
                    exists = false;
                }
            });

            return exists;
        }

        /// <summary>
        /// Count the rows in a table within the database.
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public async Task<long> TableCountRowsAsync(string tableName)
        {
            long rows = 0;

            // Open a connection to the geodatabase if not already open.
            _geodatabase ??= await OpenGeodatabaseUsingSDEFilePath(_sdeFileName);

            // Check to see if the table is a table.
            await QueuedTask.Run(() =>
            {
                try
                {
                    // Try and get the table definition.
                    using TableDefinition tableDefinition = _geodatabase.GetDefinition<TableDefinition>(tableName);

                    // Open the table.
                    using Table table = _geodatabase.OpenDataset<Table>(tableName);

                    // Count the rows in the table.
                    rows = table.GetCount();

                    // Dispose of the feature class and definition.
                    //table.Dispose();
                    //tableDefinition.Dispose();
                }
                catch
                {
                    // GetDefinition or open dataset throw an exception if the definition doesn't exist.
                    rows = 0;
                }
            });

            return rows;
        }

        #endregion

    }
}