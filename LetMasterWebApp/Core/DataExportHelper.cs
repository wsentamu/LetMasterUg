using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using System.Text;

namespace LetMasterWebApp.Core;
public static class DataExportHelper
{
    public static IActionResult ExportToCsv<T>(List<T> data, string fileName)
    {
        var builder = new StringBuilder();
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // Add headers
        builder.AppendLine(string.Join(",", properties.Select(p => p.Name)));

        // Add values
        foreach (var item in data)
        {
            builder.AppendLine(string.Join(",", properties.Select(p => p.GetValue(item)?.ToString())));
        }

        return new FileContentResult(Encoding.UTF8.GetBytes(builder.ToString()), "text/csv")
        {
            FileDownloadName = fileName
        };
    }
    public static IActionResult ExportToExcel<T>(List<T> data, string fileName)
    {
        using (var workbook = new XLWorkbook())
        {
            var worksheet = workbook.Worksheets.Add("Data");
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            // Add headers
            for (int i = 0; i < properties.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = properties[i].Name;
                worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                worksheet.Column(i + 1).AdjustToContents();
            }
            // Add values
            for (int i = 0; i < data.Count; i++)
            {
                for (int j = 0; j < properties.Length; j++)
                {
                    worksheet.Cell(i + 2, j + 1).Value = properties[j].GetValue(data[i])?.ToString();
                    worksheet.Column(j + 1).AdjustToContents();
                }
            }

            using (var stream = new MemoryStream())
            {
                workbook.SaveAs(stream);
                var content = stream.ToArray();
                return new FileContentResult(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
                {
                    FileDownloadName = fileName
                };
            }
        }
    }
}
