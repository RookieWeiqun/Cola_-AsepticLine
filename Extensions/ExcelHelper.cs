using Cola.Model;
using NPOI.SS.UserModel;
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
    }
}
