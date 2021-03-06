﻿/* 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at 
 *    http://www.apache.org/licenses/LICENSE-2.0
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Library2525D
{
    /// <summary>
    /// Symbol Search/Lookup - use several lookup tables to provide military symbol 
    ///                        search/query capabilities
    /// Tables: (1) EntityTable (2) Modifier Table (3) Legacy Id Codes
    /// 
    /// WARNING: This class is highly dependent on the *column names* for its queries
    ///          If you change the format/columns(mainly the names) of the dependent
    ///          data tables, these queries/code may also need to be changed.
    /// </summary>
    public class SymbolLookup
    {
        // TODO: this class is a bit of a mess, this was just quick prototyping,
        //       refactor/cleanse later - I know that this is just a big, complicated ball
        //       of query/lookup code, but hopefully some of the methods make sense for the
        //       the types of things you need to query about mil-symbols i.e.:
        //       get{Distinct}Modifiers, get{Distinct}Entities, get{Type}Code etc.

        public SymbolLookup()
        {
        }


        public static string GetWellFormedName(SymbolSetType symbolSet, string entityPart,
            string entityTypePart, string entitySubTypePart)
        {
            string symbolSetName = symbolSet.ToString().Replace("_", " ");

            StringBuilder nameBuilder = new StringBuilder();

            if (!string.IsNullOrEmpty(symbolSetName) && symbolSetName.Length > 0)
                nameBuilder.Append(symbolSetName);

            if (!string.IsNullOrEmpty(entityPart) && entityPart.Length > 0)
            {
                nameBuilder.Append(TypeUtilities.NameSeparator);
                nameBuilder.Append(entityPart);
            }

            if (!string.IsNullOrEmpty(entityTypePart) && entityTypePart.Length > 0)
            {
                nameBuilder.Append(TypeUtilities.NameSeparator);
                nameBuilder.Append(entityTypePart);
            }

            if (!string.IsNullOrEmpty(entitySubTypePart) && entitySubTypePart.Length > 0)
            {
                nameBuilder.Append(TypeUtilities.NameSeparator);
                nameBuilder.Append(entitySubTypePart);
            }

            return nameBuilder.ToString();
        }

        /// <summary>
        /// Search based on the one or more attributes supplied
        /// </summary>
        public List<MilitarySymbol> GetMilitarySymbols(SymbolSetType symbolSet,
            StandardIdentityAffiliationType affiliation = StandardIdentityAffiliationType.Unknown,
            string entityNameString = "", string entityTypeNameString = "", 
            string entitySubTypeNameString = "", bool exactEntityMatchOnly = false)
        {
            List<MilitarySymbol> symbolList = new List<MilitarySymbol>();

            if (EntityTable == null)
                return symbolList; // nothing

            string symbolSetToSearch = TypeUtilities.EnumHelper.getEnumValAsString(symbolSet, 2);

            var results = from row in EntityTable.AsEnumerable()
                      where (row.Field<string>("SymbolSet") == symbolSetToSearch)
                      select row;

            // Check that search returned something
            int resultCount = results.Count();
            if (resultCount < 1)
            {
                System.Diagnostics.Trace.WriteLine("WARNING: Empty result of search");
                // TODO: add search params to the debug output
                return symbolList; // empty list
            }

            ////////////////////////////////////////////////////////////////
            // Filters for Entity, EntityType, EntitySubType if these are suppied

            // TRICKY: exactEntityMatchOnly if we only want the 1 that matches exactly, versus all of the
            // type, subtype matches
            if (exactEntityMatchOnly)
            {
                results = from row in results
                            where ((row.Field<string>("Entity") == entityNameString) 
                            & (row.Field<string>("EntityType") == entityTypeNameString)
                            & (row.Field<string>("EntitySubType") == entitySubTypeNameString))
                            select row;
            }
            else if (!string.IsNullOrEmpty(entityNameString))
            {
                // filter results if this is set
                results = from row in results
                            where (row.Field<string>("Entity") == entityNameString)
                            select row;

                if (!string.IsNullOrEmpty(entityTypeNameString))
                {
                    // filter results if this is set
                    results = from row in results
                                where (row.Field<string>("EntityType") == entityTypeNameString)
                                select row;

                    if (!string.IsNullOrEmpty(entitySubTypeNameString))
                    {
                        // filter results if this is set
                        results = from row in results
                                    where (row.Field<string>("EntitySubType") == entitySubTypeNameString)
                                    select row;

                    }
                }
            }

            // Check that search returned something
            resultCount = results.Count();
            if (resultCount < 1)
            {
                System.Diagnostics.Trace.WriteLine("WARNING: Empty result of search");
                // TODO: add search params to the debug output
                return symbolList; // empty list
            }

            foreach (DataRow row in results)
            {
                string symbolSetString = row["SymbolSet"] as string;
                string entityCode = row["Code"] as string;
                string geoType = row["GeometryType"] as string;

                string entityPart = row["Entity"] as string;
                string entityTypePart = row["EntityType"] as string;
                string entitySubTypePart = row["EntitySubType"] as string;

                MilitarySymbol createSymbol = CreateSymbolFromStringProperties(symbolSet, 
                    affiliation, symbolSetString, entityCode, geoType,
                    entityPart, entityTypePart, entitySubTypePart);

                symbolList.Add(createSymbol);
            }

            return symbolList;
        }

        public string GetModifierCodeFromName(SymbolSetType symbolSet, int modifierNumber, 
            string modifierNameString)
        {
            if (ModifierTable == null)
                return string.Empty;

            string symbolSetToSearch = TypeUtilities.EnumHelper.getEnumValAsString(symbolSet, 2);

            // assmumes that the names will be unique within a symbol set
            // if not, we will also need the modifier number as an input
            string modifierToSearch = modifierNumber.ToString();

            var results = from row in ModifierTable.AsEnumerable()
                           where ((row.Field<string>("SymbolSet") == symbolSetToSearch)
                                & (row.Field<string>("Name") == modifierNameString)
                                & (row.Field<string>("ModifierNumber") == modifierToSearch))
                           select row;

            int resultCount = results.Count();
            if (resultCount < 1)
            {
                System.Diagnostics.Trace.WriteLine("Modifier Name not found: " + modifierNameString);
                return string.Empty;
            }

            string modifierCode = string.Empty;

            foreach (DataRow row in results)
            {
                modifierCode = row["Code"] as string;

                // We only care about the 1st result
                break;
            }

            return modifierCode;
        }

        public string GetModifierCategoryFromName(SymbolSetType symbolSet, string modifierNameString,
            int modfierNumber = 1)
        {
            if (ModifierTable == null)
                return string.Empty;

            string symbolSetToSearch = TypeUtilities.EnumHelper.getEnumValAsString(symbolSet, 2);

            string modifierToSearch = modfierNumber.ToString();

            var results = from row in ModifierTable.AsEnumerable()
                          where ((row.Field<string>("SymbolSet") == symbolSetToSearch)
                               & (row.Field<string>("Name") == modifierNameString)
                                & (row.Field<string>("ModifierNumber") == modifierToSearch))
                          select row;

            int resultCount = results.Count();
            if (resultCount < 1)
            {
                System.Diagnostics.Trace.WriteLine("Modifier Name not found: " + modifierNameString);
                return string.Empty;
            }

            string modifierCategory = string.Empty;

            foreach (DataRow row in results)
            {
                modifierCategory = row["Category"] as string;

                // We only care about the 1st result
                break;
            }

            return modifierCategory;
        }

        public List<string> GetDistinctModifierNames(SymbolSetType symbolSet, int modfierNumber = 1)
        {
            List<string> distinctResultStrings = new List<string>();

            if (ModifierTable == null)
                return distinctResultStrings; // nothing

            string symbolSetToSearch = TypeUtilities.EnumHelper.getEnumValAsString(symbolSet, 2);

            string modifierToSearch = modfierNumber.ToString();

            var results = (from row in ModifierTable.AsEnumerable()
                           where ((row.Field<string>("SymbolSet") == symbolSetToSearch)
                                & (row.Field<string>("ModifierNumber") == modifierToSearch))
                           select row.Field<string>("Name")).Distinct();

            foreach (var result in results)
            {
                distinctResultStrings.Add(result);
            }

            return distinctResultStrings;
        }

        public List<string> GetDistinctEntries(SymbolSetType symbolSet, string entityNameString = "",
            string entityTypeNameString = "")
        {
            string symbolSetToSearch = TypeUtilities.EnumHelper.getEnumValAsString(symbolSet, 2);

            List<string> distinctResultStrings = new List<string>();

            if (EntityTable == null)
                return distinctResultStrings; // nothing

            // TODO: remove/refactor redundant code below

            if (string.IsNullOrEmpty(entityNameString))
            {
                // Distinct Entities for a SymbolSet
                var results = (from row in EntityTable.AsEnumerable()
                               where (row.Field<string>("SymbolSet") == symbolSetToSearch)
                               select row.Field<string>("Entity")).Distinct();

                int resultCount = results.Count();
                if (resultCount < 1)
                {
                    System.Diagnostics.Trace.WriteLine("WARNING: Empty result of search");
                    return distinctResultStrings; // empty list
                }

                foreach (var result in results)
                {
                    distinctResultStrings.Add(result);
                }
            }
            else if (string.IsNullOrEmpty(entityTypeNameString))
            {
                // Distinct EntityType for Entities
                var results = (from row in EntityTable.AsEnumerable()
                               where ((row.Field<string>("SymbolSet") == symbolSetToSearch)
                                    & (row.Field<string>("Entity") == entityNameString))
                               select row.Field<string>("EntityType")).Distinct();

                int resultCount = results.Count();
                if (resultCount < 1)
                {
                    System.Diagnostics.Trace.WriteLine("WARNING: Empty result of search");
                    return distinctResultStrings; // empty list
                }

                foreach (var result in results)
                {
                    if (!string.IsNullOrWhiteSpace(result))
                        distinctResultStrings.Add(result);
                }

            }
            else
            {
                //  Distinct EntitySubType for EntityTypes
                var results = (from row in EntityTable.AsEnumerable()
                               where ((row.Field<string>("SymbolSet") == symbolSetToSearch)
                                    & (row.Field<string>("Entity") == entityNameString)
                                    & (row.Field<string>("EntityType") == entityTypeNameString))
                               select row.Field<string>("EntitySubType")).Distinct();

                int resultCount = results.Count();
                if (resultCount < 1)
                {
                    System.Diagnostics.Trace.WriteLine("WARNING: Empty result of search");
                    return distinctResultStrings; // empty list
                }

                foreach (var result in results)
                {
                    if (!string.IsNullOrWhiteSpace(result))
                        distinctResultStrings.Add(result);
                }
            }

            return distinctResultStrings;
        }

        public string GetEntityCode(SymbolSetType symbolSet, string entityNameString, 
            string entityTypeNameString = "", string entitySubTypeNameString = "")
        {
            if (EntityTable == null)
                return string.Empty;

            string symbolSetToSearch = TypeUtilities.EnumHelper.getEnumValAsString(symbolSet, 2);

            var results = from row in EntityTable.AsEnumerable()
                          where ((row.Field<string>("SymbolSet") == symbolSetToSearch) 
                                & (row.Field<string>("Entity") == entityNameString))
                          select row;
           
            if (!string.IsNullOrEmpty(entityTypeNameString))
            {
                // filter results if this is set
                results = from row in results
                          where (row.Field<string>("EntityType") == entityTypeNameString)
                          select row;
            }

            if (!string.IsNullOrEmpty(entitySubTypeNameString))
            {
                // filter results if this is set
                results = from row in results
                          where (row.Field<string>("EntitySubType") == entitySubTypeNameString)
                          select row;
            }

            int resultCount = results.Count();
            if (resultCount < 1)
            {
                System.Diagnostics.Trace.WriteLine("Entity Name not found: " + entityNameString);
                return string.Empty;
            }

            string entityCode = string.Empty;

            foreach (DataRow row in results)
            {
                entityCode = row["Code"] as string;

                // We only care about the 1st result
                break;
            }

            return entityCode;
        }

        /// <summary>
        /// Creates a symbol by looking up the SymbolSet + Entity Name 
        /// And then it sets the Affiliation if provided
        /// - this is just a simple/basic test
        /// </summary>
        public MilitarySymbol CreateSymbolByEntityName(string entityName,
                        SymbolSetType symbolSet = SymbolSetType.NotSet,
                        StandardIdentityAffiliationType affiliation = StandardIdentityAffiliationType.Unknown)
        {
            if (EntityTable == null)
                return null;

            var results = from row in EntityTable.AsEnumerable()
                          where ((row.Field<string>("EntitySubType").Contains(entityName)
                            | (row.Field<string>("EntitySubType").Contains(entityName)
                            | (row.Field<string>("Entity").Contains(entityName)))))
                            select row;

            int resultCount = results.Count();

            if (resultCount < 1)
            {
                System.Diagnostics.Trace.WriteLine("Entity Name not found: " + entityName);
                return null;
            }

            if (symbolSet != SymbolSetType.NotSet)
            {
                // filter results if this is set
                string symbolSetToSearch = TypeUtilities.EnumHelper.getEnumValAsString(symbolSet, 2);

                results = from row in results
                          where (row.Field<string>("SymbolSet") == symbolSetToSearch)
                              select row;
            }

            resultCount = results.Count();

            if (resultCount < 1)
            {
                System.Diagnostics.Trace.WriteLine("Entity Name not found: " + entityName +
                    " in SymbolSet: " + symbolSet);
                return null;
            }

            MilitarySymbol retSymbol = null; 

            foreach (DataRow row in results)
            {
                string symbolSetString = row["SymbolSet"] as string;
                string entityCode = row["Code"] as string;
                string geoType = row["GeometryType"] as string;

                string entityPart        = row["Entity"] as string;
                string entityTypePart    = row["EntityType"] as string;
                string entitySubTypePart = row["EntitySubType"] as string;

                retSymbol = CreateSymbolFromStringProperties(symbolSet, 
                    affiliation, symbolSetString, entityCode, geoType,
                    entityPart, entityTypePart, entitySubTypePart);

                // TODO: figure out what to do if we get more than 1 result
                break;
            }

            return retSymbol;
        }

        public MilitarySymbol CreateSymbolFromStringProperties(
            SymbolSetType symbolSet,
            StandardIdentityAffiliationType affiliation,
            string symbolSetString, string entityCode, string geoType,
            string entityPart, string entityTypePart, string entitySubTypePart)
        {
            MilitarySymbol retSymbol = new MilitarySymbol();

            SymbolIdCode sidc = new SymbolIdCode();
            sidc.Affiliation = affiliation;
            sidc.SymbolSetAsString = symbolSetString;
            sidc.FullEntityCode = entityCode;

            sidc.Name = GetWellFormedName(symbolSet, entityPart, entityTypePart, entitySubTypePart);

            retSymbol.Id = sidc;
            retSymbol.Shape = (ShapeType)TypeUtilities.EnumHelper.getEnumFromString(typeof(ShapeType), geoType);

            return retSymbol;
        }

        public bool Initialized
        {
            get
            {
                if ((EntityTable == null) || (ModifierTable == null) || (LegacyCodeMappingTable == null))
                    return false;

                if ((EntityTable.Rows != null) && (EntityTable.Columns != null) &&
                    (EntityTable.Rows.Count > 0) && (EntityTable.Columns.Count > 0) && 
                    (ModifierTable.Rows != null) && (ModifierTable.Columns != null) &&
                    (ModifierTable.Rows.Count > 0) && (ModifierTable.Columns.Count > 0) && 
                    (LegacyCodeMappingTable.Rows != null) && (LegacyCodeMappingTable.Columns != null) &&
                    (LegacyCodeMappingTable.Rows.Count > 0) && (LegacyCodeMappingTable.Columns.Count > 0))
                {
                    // Any other check needed? required column names?
                    return true;
                }

                return false;
            }
        }

        public DataTable EntityTable
        {
            get { return entityTable; }
        }
        private DataTable entityTable = null;

        public DataTable ModifierTable
        {
            get { return modifierTable; }
        }
        private DataTable modifierTable = null;

        public DataTable LegacyCodeMappingTable
        {
            get { return legacyCodeMappingTable; }
        }
        private DataTable legacyCodeMappingTable = null; 

        public bool Initialize()
        {
            string basePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");

            string csvEntityTableFileName = "All_Entities.csv";
            string csvEntityTableFullPath = System.IO.Path.Combine(basePath, csvEntityTableFileName);
            CsvToTableMaker csvTableEntity = new CsvToTableMaker();
            csvTableEntity.LoadTable(csvEntityTableFullPath);
            entityTable = csvTableEntity.Table;

            string csvModifierTableFileName = "All_Modifiers.csv";
            string csvModifierTableFullPath = System.IO.Path.Combine(basePath, csvModifierTableFileName);
            CsvToTableMaker csvTableModifier = new CsvToTableMaker();
            csvTableModifier.LoadTable(csvModifierTableFullPath);
            modifierTable = csvTableModifier.Table;

            string csvLegacyCodeMappingTableFileName = "LegacyMappingTable.csv";
            string csvLegacyCodeMappingTableFullPath = System.IO.Path.Combine(basePath, csvLegacyCodeMappingTableFileName);
            CsvToTableMaker csvTableLegacyCodeMapping = new CsvToTableMaker();
            csvTableLegacyCodeMapping.LoadTable(csvLegacyCodeMappingTableFullPath);
            legacyCodeMappingTable = csvTableLegacyCodeMapping.Table;

            // In case you need to check these:
            //   csvTableEntity.DebugOutput();
            //   csvTableModifier.DebugOutput();
            //   csvTableLegacyCodeMapping.DebugOutput();

            if ((csvTableEntity.IsValid) && (csvTableModifier.IsValid) &&
                (csvTableLegacyCodeMapping.IsValid))
                return true;
            else
                return false;
        }

    }
}
