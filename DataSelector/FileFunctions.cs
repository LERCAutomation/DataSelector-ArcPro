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
using System.Collections.Generic;
using System.IO;

namespace DataTools
{
    /// <summary>
    /// This class provides basic file and folder functions.
    /// </summary>
    static class FileFunctions
    {

        #region Directories

        /// <summary>
        /// Check if a directory exists.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static bool DirExists(string filePath)
        {
            // Check input first.
            if (filePath == null) return false;

            // Check if the directory exists.
            DirectoryInfo myDir = new(filePath);
            if (myDir.Exists == false) return false;
            return true;
        }

        /// <summary>
        /// Get the name of a directory from a full path.
        /// </summary>
        /// <param name="fullPath"></param>
        /// <returns></returns>
        public static string GetDirectoryName(string fullPath)
        {
            // Check input first.
            if (String.IsNullOrEmpty(fullPath)) return null;

            // Get the directory name.
            FileInfo fileInfo = new(fullPath);
            string dirName = fileInfo.DirectoryName;
            return dirName;
        }

        #endregion

        #region Files

        /// <summary>
        /// Check if a file exists from a file path and name.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static bool FileExists(string filePath, string fileName)
        {
            // If the directory exists.
            if (DirExists(filePath))
            {
                string strFileName;
                string pathEnd = filePath.Substring(filePath.Length - 1, 1);
                if (pathEnd != @"\")
                {
                    strFileName = filePath + @"\" + fileName;
                }
                else
                {
                    strFileName = filePath + fileName;
                }

                FileInfo fileInfo = new(strFileName);

                if (fileInfo.Exists) return true;
                else return false;
            }
            return false;
        }

        /// <summary>
        /// Check if a file exists from a full path.
        /// </summary>
        /// <param name="fullPath"></param>
        /// <returns></returns>
        public static bool FileExists(string fullPath)
        {
            // Check input first.
            if (fullPath == null) return false;

            // Check if the file exists.
            FileInfo fileInfo = new(fullPath);
            if (fileInfo.Exists) return true;
            return false;
        }

        /// <summary>
        /// Get the name of a file from a full path.
        /// </summary>
        /// <param name="fullPath"></param>
        /// <returns></returns>
        public static string GetFileName(string fullPath)
        {
            // Check input first.
            if (fullPath == null) return null;

            // Get the file name.
            FileInfo fileInfo = new(fullPath);
            string fileName = fileInfo.Name;
            return fileName;
        }

        /// <summary>
        /// Get a file extension from a full path.
        /// </summary>
        /// <param name="fullPath"></param>
        /// <returns></returns>
        public static string GetExtension(string fullPath)
        {
            // Check input first.
            if (fullPath == null) return null;

            // Get the file extension.
            FileInfo fileInfo = new(fullPath);
            string aExt = fileInfo.Extension;
            return aExt;
        }

        /// <summary>
        /// Get all files in a directory.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static List<string> GetAllFilesInDirectory(string filePath)
        {
            List<string> myFileList = [];
            if (DirExists(filePath))
            {
                string[] fileEntries = Directory.GetFiles(filePath);
                foreach (string aFile in fileEntries)
                {
                    myFileList.Add(aFile);
                }
            }
            return myFileList;
        }

        /// <summary>
        /// Get a full file name without the extension.
        /// </summary>
        /// <param name="fullName"></param>
        /// <returns></returns>
        public static string GetFullNameWithoutExtension(string fullName)
        {
            // Check input first.
            if (fullName == null) return null;

            // Get the directory name.
            string filePath = GetDirectoryName(fullName);

            // Get the file name without the extension.
            string fileName = Path.GetFileNameWithoutExtension(fullName);

            return filePath + @"\" + fileName;
        }

        /// <summary>
        /// Get a full file name without the extension.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static string GetFileNameWithoutExtension(string fileName)
        {
            // Check input first.
            if (fileName == null) return null;

            // Get the file name without the extension.
            fileName = Path.GetFileNameWithoutExtension(fileName);

            return fileName;
        }

        /// <summary>
        /// Delete a file.
        /// </summary>
        /// <param name="fullPath"></param>
        /// <returns></returns>
        public static bool DeleteFile(string fullPath)
        {
            if (FileFunctions.FileExists(fullPath))
            {
                try
                {
                    File.Delete(fullPath);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            else
                return true;

        }

        #endregion

        #region Logfile

        /// <summary>
        /// Create a log file.
        /// </summary>
        /// <param name="logFile"></param>
        /// <returns></returns>
        public static bool CreateLogFile(string logFile)
        {
            StreamWriter myWriter = new(logFile, false);

            myWriter.WriteLine("Log file for Data Selector, started on " + DateTime.Now.ToString());
            myWriter.Close();
            myWriter.Dispose();
            return true;
        }

        /// <summary>
        /// Write to the end of a log file.
        /// </summary>
        /// <param name="logFile"></param>
        /// <param name="logLine"></param>
        /// <returns></returns>
        public static bool WriteLine(string logFile, string logLine)
        {
            try
            {
                // Add the date and time to the start of the text.
                logLine = DateTime.Now.ToString() + " : " + logLine;

                // Open the log file.
                StreamWriter myWriter = new(logFile, true);

                // Write the line to the end of the log file.
                myWriter.WriteLine(logLine);

                // Close the log file and dispose of the object.
                myWriter.Close();
                myWriter.Dispose();
            }
            catch
            {
                return false;
            }

            return true;
        }

        #endregion

    }
}