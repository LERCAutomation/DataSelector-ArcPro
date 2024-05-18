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

using ArcGIS.Desktop.Internal.Framework.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DataTools
{
    /// Original code from:
    /// http://www.codeproject.com/Articles/11556/Converting-Wildcards-to-Regexes<summary>
    /// Represents a wildcard running on the
    /// <see cref="System.Text.RegularExpressions"/> engine.
    /// </summary>
    public class Wildcard : Regex
    {

        #region Constructor

        /// <summary>
        /// Initializes a wildcard with the given search pattern.
        /// </summary>
        /// <param name="pattern">The wildcard pattern to match.</param>
        public Wildcard(string pattern)
            : base(WildcardToRegex(pattern))
        {
        }

        /// <summary>
        /// Initializes a wildcard with the given search pattern and options.
        /// </summary>
        /// <param name="pattern">The wildcard pattern to match.</param>
        /// <param name="options">A combination of one or more
        /// <see cref="System.Text.RegexOptions"/>.</param>
        public Wildcard(string pattern, RegexOptions options)
            : base(WildcardToRegex(pattern), options)
        {
        }

        /// <summary>
        /// Initializes a wildcard with the given search pattern, schema and options.
        /// </summary>
        /// <param name="pattern">The wildcard pattern to match.</param>
        /// <param name="options">A combination of one or more
        /// <see cref="System.Text.RegexOptions"/>.</param>
        public Wildcard(string pattern, string schema, RegexOptions options)
            : base(WildcardToRegex(pattern, schema), options)
        {
        }

        /// <summary>
        /// Converts a wildcard to a regex.
        /// </summary>
        /// <param name="pattern">The wildcard pattern to convert.</param>
        /// <param name="schema">"The database schema to append before each part.</param>
        /// <returns>A regex equivalent of the given wildcard.</returns>
        public static string WildcardToRegex(string pattern, string schema)
        {
            if (schema is null)
                return "^" + Regex.Escape(pattern).
                 Replace("\\*", ".*").
                 Replace("\\?", "*").
                 Replace("\\|", "|") + "$";
            else
                return "^" + schema + "." + Regex.Escape(pattern).
                 Replace("\\*", ".*").
                 Replace("\\?", "*").
                 Replace("\\|", "|" + schema + ".") + "$";
        }

        /// <summary>
        /// Converts a wildcard to a regex.
        /// </summary>
        /// <param name="pattern">The wildcard pattern to convert.</param>
        /// <returns>A regex equivalent of the given wildcard.</returns>
        public static string WildcardToRegex(string pattern)
        {
            return "^" + Regex.Escape(pattern).
             Replace("\\*", ".*").
             Replace("\\?", "*").
             Replace("\\|", "|") + "$";
            //Replace("\\*", ".*").
        }

        #endregion
    }

    /// <summary>
    /// This class provides a variety of functions to check and modify
    /// strings.
    /// </summary>
    public class StringFunctions
    {

        #region Constructor

        public StringFunctions()
        {
            // constructor takes no arguments.
        }

        #endregion

        #region Characters

        /// <summary>
        /// Remove all potentially special characters from a string and return the result.
        /// </summary>
        /// <param name="anInputString"></param>
        /// <param name="aReplaceString"></param>
        /// <param name="isFileName"></param>
        /// <returns></returns>
        public static string StripIllegals(string anInputString, string aReplaceString, bool isFileName = false)
        {
            // If it is a file name, check if there is a '.' at fourth place before last.
            bool blAddFileDot = false;
            if (isFileName)
            {
                char chTest = anInputString[anInputString.Length - 4];
                if (chTest == '.') blAddFileDot = true;
            }

            string strOutputString = anInputString;
            List<string> theIllegals = new() { @"\", "%", "$", ":", "*", "/", "?", "<", ">", "|", "~", "£", "." };
            foreach (string aSearchString in theIllegals)
            {
                strOutputString = strOutputString.Replace(aSearchString, aReplaceString);
            }
            if (blAddFileDot)
            {
                strOutputString = strOutputString.Remove(strOutputString.Length - 4, 1);
                strOutputString = strOutputString.Insert(strOutputString.Length - 3, ".");
            }
            return strOutputString;
        }

        /// <summary>
        /// Check if the supplied replacement character is a valid character.
        /// </summary>
        /// <param name="aReplacementCharacter"></param>
        /// <returns></returns>
        public static bool IsValid(string aReplacementCharacter)
        {
            List<string> theIllegals = new() { @"\", "%", "$", ":", "*", "/", "?", "<", ">", "|", "~", "£", "." };

            if (theIllegals.IndexOf(aReplacementCharacter) == -1)
                return true;
            return false;

        }

        /// <summary>
        /// Keeps numbers and spaces from an input string and returns the results.
        /// </summary>
        /// <param name="anInputString"></param>
        /// <param name="aReplaceCharacter"></param>
        /// <returns></returns>
        public static string KeepNumbersAndSpaces(string anInputString, string aReplaceCharacter)
        {
            string strOutputString = "";
            int a;
            int aCount = 0;
            foreach (char strTest in anInputString)
            {
                if (int.TryParse(strTest.ToString(), out a) == true)
                {
                    strOutputString += strTest.ToString();
                    aCount++;
                }
                // Replace characters and spaces are not included at the start of the reference.
                else if ((strTest == ' ' || strTest.ToString() == aReplaceCharacter) && aCount > 0)
                    strOutputString += strTest.ToString();
            }
            return strOutputString;
        }

        #endregion

        #region References

        /// <summary>
        /// Gets the sub-reference out of a short reference string.
        /// </summary>
        /// <param name="anInputString"></param>
        /// <param name="aReplaceString"></param>
        /// <returns></returns>
        public static string GetSubref(string anInputString, string aReplaceString)
        {
            // Input should look like xx.xxxx or xxxx where x is an integer.
            int a = anInputString.IndexOf(aReplaceString) + 1; // The index of the first numeric character after the replace character.
            return anInputString.Substring(a, anInputString.Length - a);
        }

        /// <summary>
        /// Replace standard search strings in a supplied text string.
        /// </summary>
        /// <param name="RawName"></param>
        /// <param name="Reference"></param>
        /// <param name="SiteName"></param>
        /// <param name="ShortRef"></param>
        /// <param name="Subref"></param>
        /// <returns></returns>
        public static string ReplaceSearchStrings(string RawName, string Reference, string SiteName, string ShortRef, string Subref)
        {
            string CleanName = RawName;
            CleanName = CleanName.Replace("%ref%", Reference);
            CleanName = CleanName.Replace("%shortref%", ShortRef);
            CleanName = CleanName.Replace("%subref%", Subref);
            CleanName = CleanName.Replace("%sitename%", SiteName);

            // Take account of the occurrence of dangling underscores (if no site name was given).
            if (CleanName.Substring(CleanName.Length - 1, 1) == "_")
                CleanName = CleanName.Substring(0, CleanName.Length - 1);
            return CleanName;
        }

        #endregion

        #region Separators

        /// <summary>
        /// Replace a comma separated string with semi-colon separators.
        /// </summary>
        /// <param name="aGroupColumnString"></param>
        /// <returns></returns>
        public static string GetGroupColumnsFormatted(string aGroupColumnString)
        {
            List<string> strColumns = aGroupColumnString.Split(',').ToList();
            string strFormatted = "";
            foreach (string strEntry in strColumns)
            {
                strFormatted = strFormatted + strEntry.Trim() + ";";
            }
            if (strFormatted != "")
                return strFormatted.Substring(0, strFormatted.Length - 1); // Remove the final semicolon.
            else
                return "";

        }

        /// <summary>
        /// Replace a dollar separated string with semi-colon seperators.
        /// </summary>
        /// <param name="aStatsColumnString"></param>
        /// <returns></returns>
        public static string GetStatsColumnsFormatted(string aStatsColumnString)
        {
            List<string> strEntries = aStatsColumnString.Split('$').ToList();
            string strFormatted = "";
            foreach (string strEntry in strEntries)
            {
                strFormatted = strFormatted + strEntry.Replace(";", " ") + ";";
            }
            if (strFormatted != "")
                return strFormatted.Substring(0, strFormatted.Length - 1); // Remove the final comma.
            else
                return "";
        }

        #endregion

        #region Columns

        /// <summary>
        /// Align statistics columns.
        /// </summary>
        /// <param name="AllColumns"></param>
        /// <param name="StatsColumns"></param>
        /// <param name="GroupColumns"></param>
        /// <returns></returns>
        public static string AlignStatsColumns(string AllColumns, string StatsColumns, string GroupColumns)
        {
            if (GroupColumns == "" || AllColumns == "") //GroupColumns == "" ||
                return StatsColumns;
            List<string> liAllColumns = AllColumns.Split(',').ToList();
            foreach (string strFieldName in liAllColumns)
            {
                string strFieldNameTr = strFieldName.Trim();
                if (strFieldNameTr.Substring(0, 1) != "\"")
                {
                    // Is it in the group columns?
                    if (!GroupColumns.Contains(strFieldNameTr, StringComparison.CurrentCultureIgnoreCase))
                    {
                        // Is it in the stats columns?
                        if (!StatsColumns.Contains(strFieldNameTr, StringComparison.CurrentCultureIgnoreCase))
                        {
                            // It is in neither - add it.
                            if (StatsColumns != "")
                                StatsColumns += ";";
                            StatsColumns = StatsColumns + strFieldNameTr + " FIRST";
                        }
                    }
                }
            }

            return StatsColumns;
        }

        #endregion

        #region Groups

        /// <summary>
        /// Look at each layer in a list of layers and returns the unique
        /// group names (in front of any hyphen in the layer names).
        /// </summary>
        /// <param name="LayerList"></param>
        /// <returns></returns>
        public static List<string> ExtractGroups(List<string> LayerList)
        {
            List<string> liGroups = new();
            foreach (string strLayerName in LayerList)
            {
                int intHyphenIndex = strLayerName.IndexOf('-');
                if (intHyphenIndex != -1) // It has a group name
                {
                    string strGroupName = strLayerName.Substring(0, intHyphenIndex); // Check if we already have this one
                    if (liGroups.IndexOf(strGroupName) == -1)
                    {
                        liGroups.Add(strGroupName); // If not, add it.
                    }
                }
            }
            return liGroups; // Return the list of group names.

        }

        /// <summary>
        /// Return the group name from the front of a hyphen in
        /// the layer name.
        /// </summary>
        /// <param name="LayerName"></param>
        /// <returns></returns>
        public static string GetGroupName(string LayerName)
        {
            int intHyphenIndex = LayerName.IndexOf('-');
            if (intHyphenIndex != -1) // It has a group name
            {
                string strGroupName = LayerName.Substring(0, intHyphenIndex);
                return strGroupName; // Return the group name
            }
            else
                return ""; // No group name.
        }

        #endregion

    }
}