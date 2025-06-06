﻿using Cola.Model;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;

namespace Cola.Extensions
{
    public class ExcelHelper
    {
        public static void ExportToExcel(List<CheckPara> data, string filePath)
        {
            // 创建一个新的工作簿
            IWorkbook workbook = new XSSFWorkbook();
            ISheet worksheet = workbook.CreateSheet("Sheet1");

            // 添加表头
            IRow headerRow = worksheet.CreateRow(0);
            headerRow.CreateCell(0).SetCellValue("ID");
            headerRow.CreateCell(1).SetCellValue("Name");
            headerRow.CreateCell(2).SetCellValue("Age");

            // 填充数据
            for (int i = 0; i < data.Count; i++)
            {
                IRow row = worksheet.CreateRow(i + 1);
                row.CreateCell(0).SetCellValue(data[i].Id);
                row.CreateCell(1).SetCellValue(data[i].Name);
                row.CreateCell(2).SetCellValue(data[i].AliasName);
            }

            // 保存文件
            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                workbook.Write(fileStream);
            }
        }


        public static byte[] CreateCheckDataTemplate(List<ExcelData> excelList)
        {
            using (var workbook = new XSSFWorkbook())
            {
                var sheet = workbook.CreateSheet("果肉杀菌在线混合记录表");
                
                // 设置列宽
                sheet.SetColumnWidth(0, 20 * 256);  // 设备名称
                sheet.SetColumnWidth(1, 20 * 256);  // 项目说明
                sheet.SetColumnWidth(2, 15 * 256);  // 参考值
                sheet.SetColumnWidth(3, 10 * 256);  // 单位
                // 时间列宽度
                for (int i = 4; i <= 16; i++)
                {
                    sheet.SetColumnWidth(i, 12 * 256);
                }

                // 创建样式
                var titleStyle = workbook.CreateCellStyle();
                var titleFont = workbook.CreateFont();
                titleFont.FontHeightInPoints = 14;
                titleFont.IsBold = true;
                titleStyle.SetFont(titleFont);
                titleStyle.Alignment = HorizontalAlignment.Center;
                titleStyle.VerticalAlignment = VerticalAlignment.Center;
                
                // 创建表头
                var titleRow = sheet.CreateRow(0);
                var titleCell = titleRow.CreateCell(0);
                titleCell.SetCellValue("果肉杀菌在线混合记录表");
                titleCell.CellStyle = titleStyle;
                sheet.AddMergedRegion(new CellRangeAddress(0, 0, 0, 16));
                
                // 添加日期行
                var dateRow = sheet.CreateRow(1);
                dateRow.CreateCell(0).SetCellValue("日期：");
                
                // 创建时间表头
                var timeRow = sheet.CreateRow(2);
                string[] times = { "07:00", "08:00", "09:00", "10:00", "11:00", "12:00", "13:00", 
                                 "14:00", "15:00", "16:00", "17:00", "18:00" };
                timeRow.CreateCell(0).SetCellValue("设备名称");
                timeRow.CreateCell(1).SetCellValue("项目说明");
                timeRow.CreateCell(2).SetCellValue("参考值");
                timeRow.CreateCell(3).SetCellValue("单位");
                
                for (int i = 0; i < times.Length; i++)
                {
                    timeRow.CreateCell(i + 4).SetCellValue(times[i]);
                }

                // 创建左侧表头
                string[][] leftHeaders = new string[][]
                {
                    new string[] { "蒸汽UHT杀菌", "蒸汽UHT步骤", "", "HMI_STEP_NUM" },
                    new string[] { "", "蒸汽UHT配方", "", "ProductName" },
                    new string[] { "生产温度点TEM01(℃)", "", "97.5-99.5", "℃" },
                    new string[] { "温控温度TEM02(℃)", "", "97.5-99.5", "℃" },
                    new string[] { "冷却温度TEM01(℃)", "", "<30", "℃" },
                    new string[] { "放气罐液位LEVEL02(%)", "", "10-85", "%" },
                    new string[] { "除气罐压力UATMP01(bar)", "", "", "Bar" },
                    new string[] { "产品流量UATMP01(t/h)", "", "2200-5000", "t/h" },
                    new string[] { "产品压力UATMP01(bar)", "", "-", "Bar" },
                    new string[] { "产品与冷水压差UATMP03(bar)", "", "-", "Bar" },
                    new string[] { "产品与热水压差UATMP03(bar)", "", "-", "Bar" },
                    new string[] { "杀菌时间", "", "", "Sec" },
                    new string[] { "蒸汽工艺编号集", "", "", "" },
                    // ... 可以继续添加其他行
                };

                int rowNum = 3;
                foreach (var header in leftHeaders)
                {
                    var row = sheet.CreateRow(rowNum++);
                    for (int i = 0; i < header.Length; i++)
                    {
                        row.CreateCell(i).SetCellValue(header[i]);
                    }
                }

                // 返回Excel文件的字节数组
                using (var ms = new MemoryStream())
                {
                    workbook.Write(ms);
                    return ms.ToArray();
                }
            }
        }
        public static byte[] ExportCheckDataToExcel(string templatePath, List<ExcelData> excelList, List<ExcelAlarmData> alarmDataList, DateTime inputTime, string title)
        {
            // Load the existing Excel template
            using (var fileStream = new FileStream(templatePath, FileMode.Open, FileAccess.Read))
            {
                // ================== 第一部分：sheet0 ==================
                var workbook = new XSSFWorkbook(fileStream);
                var sheet = workbook.GetSheetAt(0); // Assuming the data is in the first sheet

                //handle the date and title
                sheet.GetRow(0).GetCell(0).SetCellValue(title);
                sheet.GetRow(1).GetCell(3).SetCellValue( inputTime.ToString("yyyy-MM-dd"));
                //sheet.GetRow(1).GetCell(15).SetCellValue(shift);
                var borderedCellStyle = workbook.CreateCellStyle();
                borderedCellStyle.Alignment = HorizontalAlignment.Center;
                borderedCellStyle.VerticalAlignment = VerticalAlignment.Center;
                borderedCellStyle.BorderTop = BorderStyle.Thin;
                borderedCellStyle.BorderBottom = BorderStyle.Thin;
                borderedCellStyle.BorderLeft = BorderStyle.Thin;
                borderedCellStyle.BorderRight = BorderStyle.Thin;
                // Find the row to start adding data (after the headers)
                int startRow = 3; // Assuming the headers are in the first 3 rows
                int endRow = startRow+excelList.Count; // Adjust this value based on your data
                int startCol = 0;
                int endCol = 17;
                // Populate the data from excelList
                string currentDeviceName = null;
                int mergeStartRow = startRow;

                foreach (var excelData in excelList)
                {
                    var row = sheet.CreateRow(startRow++);
                    var cell0 = row.CreateCell(0);
                    cell0.SetCellValue(excelData.DeviceName);
                    cell0.CellStyle = borderedCellStyle;

                    var cell1 = row.CreateCell(1);
                    cell1.SetCellValue(excelData.ProjectDescription);
                    cell1.CellStyle = borderedCellStyle;

                    var cell2 = row.CreateCell(2);
                    cell2.SetCellValue(excelData.ReferenceValue);
                    cell2.CellStyle = borderedCellStyle;

                    var cell3 = row.CreateCell(3);
                    cell3.SetCellValue(excelData.Unit);
                    cell3.CellStyle = borderedCellStyle;

                    var cell4 = row.CreateCell(4);
                    cell4.SetCellValue(excelData.ProjectName);
                    cell4.CellStyle = borderedCellStyle;

                    var cell5= row.CreateCell(5);
                    cell5.SetCellValue(excelData.CurrentValue); // Set the current value in the fifth column
                    cell5.CellStyle = borderedCellStyle;

                    int cellIndex = 6; // Assuming time-based values start from the 6th column
                    foreach (var timeValue in excelData.TimeValues)
                    {
                        var cell = row.CreateCell(cellIndex++);
                        cell.SetCellValue(timeValue.Value);
                        cell.CellStyle = borderedCellStyle;
                    }

                    if (currentDeviceName == null)
                    {
                        currentDeviceName = excelData.DeviceName;
                    }
                    else if (currentDeviceName != excelData.DeviceName)
                    {
                        // Merge cells for the previous DeviceName
                        if (mergeStartRow < startRow - 1)
                        {
                            sheet.AddMergedRegion(new CellRangeAddress(mergeStartRow, startRow - 2, 0, 0));
                        }
                        currentDeviceName = excelData.DeviceName;
                        mergeStartRow = startRow - 1;
                    }
                }
                // Merge cells for the last DeviceName
                if (mergeStartRow < startRow - 1)
                {
                    sheet.AddMergedRegion(new CellRangeAddress(mergeStartRow, startRow - 1, 0, 0));
                }
                for (int rowIdx = startRow; rowIdx < endRow; rowIdx++)
                {
                    var row = sheet.GetRow(rowIdx) ?? sheet.CreateRow(rowIdx);
                    for (int colIdx = startCol; colIdx < endCol; colIdx++)
                    {
                        var cell = row.GetCell(colIdx) ?? row.CreateCell(colIdx);
                        cell.CellStyle = borderedCellStyle;
                    }
                }
                // ================== 第二部分：sheet2 ==================
                // Write alarmDataList to the second row
                var alarmSheet = workbook.GetSheetAt(1); // Assuming the alarm data is in the second sheet
                int alarmStartRow = 1; // Start writing from the second row

                foreach (var alarmData in alarmDataList)
                {
                    var row = alarmSheet.CreateRow(alarmStartRow++);

                    var cell0 = row.CreateCell(0);
                    cell0.SetCellValue(alarmData.DeviceName);
                    cell0.CellStyle = borderedCellStyle;

                    var cell1 = row.CreateCell(1);
                    cell1.SetCellValue(alarmData.CheckItem);
                    cell1.CellStyle = borderedCellStyle;

                    var cell2 = row.CreateCell(2);
                    cell2.SetCellValue(alarmData.SharpTime);
                    cell2.CellStyle = borderedCellStyle;

                    var cell3 = row.CreateCell(3);
                    cell3.SetCellValue(alarmData.Remark);
                    cell3.CellStyle = borderedCellStyle;
                }

                // Save the updated workbook to a memory stream
                using (var ms = new MemoryStream())
                {
                    workbook.Write(ms);
                    return ms.ToArray();
                }
            }
        }
    }
}
