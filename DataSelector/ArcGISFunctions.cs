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

using ArcGIS.Core.Data;
using ArcGIS.Core.Data.UtilityNetwork.Trace;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Core.Geoprocessing;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using System;
using System.IO;
using System.Threading.Tasks;
using ArcGIS.Core.Data.Exceptions;
using System.Linq;
using System.Collections.Generic;
using ArcGIS.Core.CIM;
using ArcGIS.Desktop.Framework.Contracts;

namespace DataTools
{
    /// <summary>
    /// This class provides ArcGIS Pro map functions.
    /// </summary>
    class MapFunctions
    {

        #region Fields

        Map _activeMap;
        MapView _activeMapView;

        #endregion

        #region Constructor

        // Class constructor.
        public MapFunctions()
        {
            // Get the active map view (if there is one).
            _activeMapView = GetActiveMapView();

            // Set the map currently displayed in the active map view.
            if (_activeMapView != null)
                _activeMap = _activeMapView.Map;
            else
                _activeMap = null;
        }

        #endregion

        #region Properties

        public string MapName
        {
            get
            {
                if (_activeMap == null)
                    return null;
                else
                    return _activeMap.Name;
            }
        }

        #endregion

        #region Map

        /// <summary>
        /// Get the active map view.
        /// </summary>
        /// <returns></returns>
        internal MapView GetActiveMapView()
        {
            // Get the active map view.
            MapView mapView = MapView.Active;
            if (mapView == null)
                return null;

            return mapView;
        }

        /// <summary>
        /// Create a new map.
        /// </summary>
        /// <param name="mapName"></param>
        /// <returns></returns>
        public async Task<string> CreateMapAsync(String mapName)
        {
            _activeMap = null;
            _activeMapView = null;

            await QueuedTask.Run(() =>
            {
                try
                {
                    // Create a new map without a basemap.
                    _activeMap = MapFactory.Instance.CreateMap(mapName, basemap:Basemap.None);

                    // Create and activate new map.
                    ProApp.Panes.CreateMapPaneAsync(_activeMap, MapViewingMode.Map);
                    //var paneTask = ProApp.Panes.CreateMapPaneAsync(_activeMap, MapViewingMode.Map);
                    //paneTask.Wait();

                    // Get the active map view;
                    //_activeMapView = GetActiveMapView();

                    //Pane pane = ProApp.Panes.ActivePane;
                    //pane.Activate();
                }
                catch
                {
                    // CreateMap throws an exception if the map view wasn't created.
                    // CreateMapPaneAsync throws an excrption if the map isn't created.
                }
            });

            // Get the active map view;
            _activeMapView = GetActiveMapView();

            return _activeMap.Name;
        }

        /// <summary>
        /// Add a layer to the active map.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task AddLayerToMap(string url)
        {
            try
            {
                await QueuedTask.Run(() =>
                {
                    Uri uri = new(url);

                    //var layerParams = new FeatureLayerCreationParams(fc);
                    var layer = LayerFactory.Instance.CreateLayer(uri, _activeMap);
                });

                // Redraw the active map.
                //_activeMapView.Redraw(false);
            }
            catch (Exception ex)
            {
            }
        }

        #endregion

        #region Layers

        /// <summary>
        /// Fine a layer by name in the active map.
        /// </summary>
        /// <param name="layerName"></param>
        /// <returns></returns>
        internal Layer FindLayer(String layerName)
        {
            //Finds layers by name and returns a read only list of Layers
            IReadOnlyList<Layer> layers = _activeMap.FindLayers(layerName, true);

            while (layers.Count > 0)
            {
                Layer layer = layers.First();

                if (layer.Map.Name == _activeMap.Name)
                    return layer;
            }

            return null;
        }

        #endregion

        #region Symbology

        /// <summary>
        /// Apply symbology to a layer by name using a layer file.
        /// </summary>
        /// <param name="layerName"></param>
        /// <param name="layerFile"></param>
        /// <returns></returns>
        public async Task<bool> ApplySymbologyFromLayerFileAsync(string layerName, string layerFile)
        {
            // Check the layer file exists.
            if (!FileFunctions.FileExists(layerFile))
                return false;

            // Find the layer in the active map.
            Layer layer = FindLayer(layerName);

            if (layer is FeatureLayer featureLayer)
            {

                // Apply the layer file symbology to the feature layer.
                try
                {
                    await QueuedTask.Run(() =>
                    {
                        // Get the Layer Document from the lyrx file.
                        var lyrDocFromLyrxFile = new LayerDocument(layerFile);
                        var cimLyrDoc = lyrDocFromLyrxFile.GetCIMLayerDocument();

                        // Get the renderer from the layer file.
                        var rendererFromLayerFile = ((CIMFeatureLayer)cimLyrDoc.LayerDefinitions[0]).Renderer as CIMUniqueValueRenderer;

                        // Apply the renderer to the feature layer.
                        featureLayer?.SetRenderer(rendererFromLayerFile);
                    });
                }
                catch (GeodatabaseNotFoundOrOpenedException exception)
                {
                    // Handle Exception.
                    return false;
                }

            }

            return true;
        }

        #endregion

    }

    /// <summary>
    /// This class provides ArcGIS Pro feature class and table functions.
    /// </summary>
    static class ArcGISFunctions
    {
        #region Feature Class

        /// <summary>
        /// Check if the feature class exists in the file path.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static async Task<bool> FeatureClassExistsAsync(string filePath, string fileName)
        {
            if (fileName.Substring(fileName.Length - 4, 1) == ".")
            {
                // It's a file.
                if (FileFunctions.FileExists(filePath + @"\" + fileName))
                    return true;
                else
                    return false;
            }
            else if (filePath.Substring(filePath.Length - 3, 3) == "sde")
            {
                // It's an SDE class
                // Not handled. We know the table exists.
                return true;
            }
            else // it is a geodatabase class.
            {
                try
                {
                    bool exists = await FeatureClassExistsGDBAsync(filePath, fileName);

                    return exists;
                }
                catch
                {
                    // GetDefinition throws an exception if the definition doesn't exist
                    return false;
                }
            }
        }

        /// <summary>
        /// Check if the feature class exists.
        /// </summary>
        /// <param name="fullPath"></param>
        /// <returns></returns>
        public static async Task<bool> FeatureClassExistsAsync(string fullPath)
        {
            return await FeatureClassExistsAsync(FileFunctions.GetDirectoryName(fullPath), FileFunctions.GetFileName(fullPath));
        }

        /// <summary>
        /// Check if the feature class exists in the file path.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static async Task<bool> FeatureClassExistsAsyncOLD(string filePath, string fileName)
        {
            if (fileName.Substring(fileName.Length - 4, 1) == ".")
            {
                // It's a file.
                if (FileFunctions.FileExists(filePath + @"\" + fileName))
                    return true;
                else
                    return false;
            }
            else if (filePath.Substring(filePath.Length - 3, 3) == "sde")
            {
                // It's an SDE class
                // Not handled. We know the table exists.
                return true;
            }
            else // it is a geodatabase class.
            {
                try
                {
                    bool exists = await FeatureClassExistsGDBAsync(filePath, fileName);

                    return exists;
                }
                catch
                {
                    // GetDefinition throws an exception if the definition doesn't exist
                    return false;
                }
            }
        }

        #endregion

        #region Geodatabase

        /// <summary>
        /// Check if the feature class exists in a geodatabase.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static async Task<bool> FeatureClassExistsGDBAsync(string filePath, string fileName)
        {
            bool exists = false;

            try
            {
                await QueuedTask.Run(() =>
                {
                    // Open the file geodatabase. This will open the geodatabase if the folder exists and contains a valid geodatabase.
                    using Geodatabase geodatabase = new(new FileGeodatabaseConnectionPath(new Uri(filePath)));

                    using FeatureClassDefinition featureClassDefinition = geodatabase.GetDefinition<FeatureClassDefinition>(fileName);

                    exists = true;
                    //if (featureClassDefinition != null)
                    //    exists = true;

                    //featureClassDefinition.Dispose();  // FeatureClass found, dispose of object
                    //geodatabase.Dispose();
                });
            }
            catch (GeodatabaseNotFoundOrOpenedException exception)
            {
                // Handle Exception.
                return false;
            }

            return exists;
        }

        #endregion

        #region TableExists

        /// <summary>
        /// Check the table exists in the file path.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static bool TableExists(string filePath, string fileName)
        {

            if (fileName.Substring(fileName.Length - 4, 1) == ".")
            {
                // It's a file.
                if (FileFunctions.FileExists(filePath + @"\" + fileName))
                    return true;
                else
                    return false;
            }
            else if (filePath.Substring(filePath.Length - 3, 3) == "sde")
            {
                // It's an SDE class
                // Not handled. We know the table exists.
                return true;
            }
            else // it is a geodatabase class.
            {
                //IWorkspaceFactory pWSF = GetWorkspaceFactory(filePath);
                //IWorkspace2 pWS = (IWorkspace2)pWSF.OpenFromFile(filePath, 0);
                //if (pWS.get_NameExists(ESRI.ArcGIS.Geodatabase.esriDatasetType.esriDTTable, fileName))
                //    return true;
                //else
                //    return false;
                return false;
            }
        }

        /// <summary>
        /// Check if the table exists.
        /// </summary>
        /// <param name="aFullPath"></param>
        /// <returns></returns>
        public static bool TableExists(string aFullPath)
        {
            return TableExists(FileFunctions.GetDirectoryName(aFullPath), FileFunctions.GetFileName(aFullPath));
        }

        #endregion

        //public static void RefreshFolders()
        //{
        //    foreach (var item in Project.Current.Items.OfType<FolderConnectionProjectItem>())
        //    {
        //        string itemName = item.Name;
        //        ((ArcGIS.Desktop.Internal.Core.IItemInternal)item).RefreshChildren();
        //    }
        //}

        #region Outputs

        /// <summary>
        /// Prompt the user to specify an output file in the required format.
        /// </summary>
        /// <param name="fileType"></param>
        /// <param name="initialDirectory"></param>
        /// <returns></returns>
        public static string GetOutputFileName(string fileType, string initialDirectory = @"C:\")
        {
            BrowseProjectFilter bf;

            //string saveItemDlg;
            switch (fileType)
            {
                case "Geodatabase FC":
                    bf = BrowseProjectFilter.GetFilter("esri_browseDialogFilters_featureClasses_all");
                    break;
                case "Geodatabase Table":
                    bf = BrowseProjectFilter.GetFilter("esri_browseDialogFilters_geodatabaseItems_tables");
                    break;
                case "Shapefile":
                    bf = BrowseProjectFilter.GetFilter("esri_browseDialogFilters_shapefiles");
                    break;
                case "CSV file (comma delimited)":
                    bf = BrowseProjectFilter.GetFilter("esri_browseDialogFilters_textFiles_csv");
                    break;
                case "Text file (tab delimited)":
                    bf = BrowseProjectFilter.GetFilter("esri_browseDialogFilters_textFiles_txt");
                    break;
                default:
                    bf = BrowseProjectFilter.GetFilter("esri_browseDialogFilters_all");
                    break;
            }

            // Display the saveItemDlg in an Open Item dialog.
            SaveItemDialog saveItemDlg = new()
            {
                Title = "Save Output As...",
                InitialLocation = initialDirectory,
                //Filter = ItemFilters.Files_All,
                OverwritePrompt = true,
                BrowseFilter = bf
            };

            bool? ok = saveItemDlg.ShowDialog();

            string strOutFile = null;
            if (ok.HasValue)
                strOutFile = saveItemDlg.FilePath;

            return strOutFile; // Null if user pressed exit
        }

        #endregion

        #region CopyFeatures

        /// <summary>
        /// Copy the input feature class to the output feature class.
        /// </summary>
        /// <param name="inFeatureClass"></param>
        /// <param name="outFeatureClass"></param>
        /// <param name="Messages"></param>
        /// <returns></returns>
        public static async Task<bool> CopyFeaturesAsync(string inFeatureClass, string outFeatureClass, bool Messages = false)
        {
            //// prepare input parameter values to CopyFeaturesAsync tool
            //string input_data = @"C:\data\california.gdb\ca_highways";
            //string out_workspace = ArcGIS.Desktop.Core.Project.Current.DefaultGeodatabasePath;
            //string out_data = System.IO.Path.Combine(out_workspace, "ca_highways2");

            // Make a value array of strings to be passed to the tool.
            var parameters = Geoprocessing.MakeValueArray(inFeatureClass, outFeatureClass);

            // Make a value array of the environments to be passed to the tool.
            var environments = Geoprocessing.MakeEnvironmentArray(overwriteoutput: true);

            // Execute the tool.
            try
            {
                IGPResult gp_result = await Geoprocessing.ExecuteToolAsync("management.CopyFeatures", parameters, environments);

                if (gp_result.IsFailed)
                {
                    var messages = gp_result.Messages;
                    var errMessages = gp_result.ErrorMessages;
                }
            }
            catch (Exception)
            {
                throw;
            }

            return true;
        }

        /// <summary>
        /// Copy the input dataset name to the output feature class.
        /// </summary>
        /// <param name="InWorkspace"></param>
        /// <param name="InDatasetName"></param>
        /// <param name="OutFeatureClass"></param>
        /// <param name="Messages"></param>
        /// <returns></returns>
        public static async Task<bool> CopyFeaturesAsync(string InWorkspace, string InDatasetName, string OutFeatureClass, bool Messages = false)
        {
            string inFeatureClass = InWorkspace + @"\" + InDatasetName;
            return await CopyFeaturesAsync(inFeatureClass, OutFeatureClass, Messages);
        }

        /// <summary>
        /// Copy the input dataset to the output dataset.
        /// </summary>
        /// <param name="InWorkspace"></param>
        /// <param name="InDatasetName"></param>
        /// <param name="OutWorkspace"></param>
        /// <param name="OutDatasetName"></param>
        /// <param name="Messages"></param>
        /// <returns></returns>
        public static async Task<bool> CopyFeaturesAsync(string InWorkspace, string InDatasetName, string OutWorkspace, string OutDatasetName, bool Messages = false)
        {
            string inFeatureClass = InWorkspace + @"\" + InDatasetName;
            string outFeatureClass = OutWorkspace + @"\" + OutDatasetName;
            return await CopyFeaturesAsync(inFeatureClass, outFeatureClass, Messages);
        }

        /// <summary>
        /// Export the input table to the output table.
        /// </summary>
        /// <param name="InTable"></param>
        /// <param name="OutFile"></param>
        /// <param name="Messages"></param>
        /// <returns></returns>
        public static async Task<bool> ExportFeaturesAsync(string inTable, string outTable, bool Messages = false)
        {
            //// prepare input parameter values to CopyFeaturesAsync tool
            //string input_data = @"C:\data\california.gdb\ca_highways";
            //string out_workspace = ArcGIS.Desktop.Core.Project.Current.DefaultGeodatabasePath;
            //string out_data = System.IO.Path.Combine(out_workspace, "ca_highways2");

            // Make a value array of strings to be passed to the tool.
            var parameters = Geoprocessing.MakeValueArray(inTable, outTable);

            // Make a value array of the environments to be passed to the tool.
            var environments = Geoprocessing.MakeEnvironmentArray(overwriteoutput: true);

            // Execute the tool.
            try
            {
                IGPResult gp_result = await Geoprocessing.ExecuteToolAsync("conversion.ExportTable", parameters, environments);

                if (gp_result.IsFailed)
                {
                    var messages = gp_result.Messages;
                    var errMessages = gp_result.ErrorMessages;
                }
            }
            catch (Exception)
            {
                throw;
            }

            return true;
        }

        #endregion

    }
}