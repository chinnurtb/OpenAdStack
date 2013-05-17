//-----------------------------------------------------------------------
// <copyright file="ExcelHelper.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using SeleniumFramework.Utilities;

namespace SeleniumFramework.Persistence
{
    /// <summary>
    /// This class contains methods and props to read data from Excel using OpenXml and return DataTable
    /// </summary>
    public static class ExcelHelper
    {
        #region Public Methods

        /// <summary>
        /// Read data from given Excel Sheet 
        /// </summary>
        /// <param name="fileToRead">File name to parse</param>
        /// <param name="sheetToRead">Sheet name to parse</param>
        /// <returns>Return DataTable</returns>
        public static DataTable GetDataTable(string fileToRead, string sheetToRead)
        {
            // Create DataTable object
            DataTable dataTable = new DataTable();
            dataTable.Locale = CultureInfo.InvariantCulture;

            // Parse Excel Workbook and Worksheet
            using (SpreadsheetDocument spreadSheetDocument = SpreadsheetDocument.Open(fileToRead, false))
            {
                WorkbookPart workbookPart = spreadSheetDocument.WorkbookPart;
                Workbook workbook = workbookPart.Workbook;

                // Find passed sheet reference
                Sheet sheet = workbook.Descendants<Sheet>().Where(s => sheetToRead.ToUpperInvariant().Equals(s.Name.String().ToUpperInvariant())).FirstOrDefault();

                // Throw an exception if there is no specified sheet
                if (sheet == null)
                {
                    throw new ArgumentException(sheetToRead);
                }

                WorksheetPart worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id);
                Worksheet workSheet = worksheetPart.Worksheet;
                SheetData sheetData = workSheet.GetFirstChild<SheetData>();
                IEnumerable<Row> rows = sheetData.Descendants<Row>();

                // This will include header row
                foreach (Cell cell in rows.ElementAt(0))
                {
                    if (cell != null && cell.CellValue != null)
                    {
                        dataTable.Columns.Add(GetCellValue(spreadSheetDocument, cell));
                    }
                }

                // This will include ONLY data rows and NOT header row
                foreach (Row row in rows.Skip(1))
                {
                    DataRow tempRow = dataTable.NewRow();

                    for (int i = 0; i < row.Descendants<Cell>().Count(); i++)
                    {
                        Cell cell = row.Descendants<Cell>().ElementAt(i);

                        if (cell != null && cell.CellValue != null)
                        {
                            tempRow[i] = GetCellValue(spreadSheetDocument, cell);
                        }
                    }

                    // Include ONLY Active = "Y" rows
                    if (tempRow["Active"].String().Equals("Y", StringComparison.CurrentCultureIgnoreCase))
                    {
                        dataTable.Rows.Add(tempRow);
                    }
                }
            }

            return dataTable;
        }

        /// <summary>
        /// Filters the rows matching to condition
        /// </summary>
        /// <param name="dataTable">DataTable from which the rows will be filtered based on the mode</param>
        /// <param name="columnName">Column Name to check</param>
        /// <param name="columnValue">Column Value to compare</param>
        /// <returns>Return DataTable</returns>
        public static DataTable FilterRows(DataTable dataTable, string columnName, string columnValue)
        {
            if (string.IsNullOrEmpty(columnValue))
            {
                return dataTable;
            }

            for (int i = dataTable.Rows.Count - 1; i >= 0; i--)
            {
                DataRow dr = dataTable.Rows[i];
                string dataColumnValue = dr[columnName].String();

                if (!string.Equals(dataColumnValue, columnValue, StringComparison.CurrentCultureIgnoreCase))
                {
                    dataTable.Rows.Remove(dr);
                }
            }

            return dataTable;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Gets cell value 
        /// </summary>
        /// <param name="document">Document to parse</param>
        /// <param name="cell">Cell to parse</param>
        /// <returns>Returns string</returns>
        private static string GetCellValue(SpreadsheetDocument document, Cell cell)
        {
            SharedStringTablePart stringTablePart = document.WorkbookPart.SharedStringTablePart;
            string value = cell.CellValue.InnerXml;

            if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
            {
                return stringTablePart.SharedStringTable.ChildElements[Convert.ToInt32(value, CultureInfo.InvariantCulture)].InnerText;
            }
            else
            {
                return value;                                  
            }
        }

        #endregion
    }
}
