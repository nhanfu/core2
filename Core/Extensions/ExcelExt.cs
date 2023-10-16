using Bridge.Html5;

namespace Core.Extensions
{
    public class ExcelExt
    {
        public static void ExportTableToExcel(string tableId, string filename, HTMLElement table = null, bool border = false)
        {
            if (border)
            {
                table.QuerySelectorAll("td").SelectForeach(x =>
                {
                    /*@
                     x.style="border:1px solid;white-space: nowrap;font-family: 'times new roman', times, serif;";
                     */
                });
                table.QuerySelectorAll("th").SelectForeach(x =>
                {
                    /*@
                     x.style="border:1px solid;white-space: nowrap;font-family: 'times new roman', times, serif;";
                     */
                });
            }
            /*@
                var dataType = "application/vnd.ms-excel";
                var extension = ".xls";
                let base64 = function(s) {
                    return window.btoa(unescape(encodeURIComponent(s)))
                };
                let template = '<html xmlns:o="urn:schemas-microsoft-com:office:office" xmlns:x="urn:schemas-microsoft-com:office:excel" xmlns="http://www.w3.org/TR/REC-html40"><head><!--[if gte mso 9]><xml><x:ExcelWorkbook><x:ExcelWorksheets><x:ExcelWorksheet><x:Name>{worksheet}</x:Name><x:WorksheetOptions><x:DisplayGridlines/></x:WorksheetOptions></x:ExcelWorksheet></x:ExcelWorksheets></x:ExcelWorkbook></xml><![endif]--></head><body><table>{table}</table></body></html>';
                let render = function(template, content) {
                    return template.replace(/{(\w+)}/g, function(m, p) { return content[p]; });
                };

                let tableElement = table || document.getElementById(tableId);

                let tableExcel = render(template, {
                    worksheet: filename,
                    table: tableElement.innerHTML
                });

                filename = filename + extension;

                if (navigator.msSaveOrOpenBlob)
                {
                    let blob = new Blob(
                        [ '\ufeff', tableExcel ],
                        { type: dataType }
                    );

                    navigator.msSaveOrOpenBlob(blob, filename);
                } else {
                    let downloadLink = document.createElement("a");

                    document.body.appendChild(downloadLink);

                    downloadLink.href = 'data:' + dataType + ';base64,' + base64(tableExcel);

                    downloadLink.download = filename;

                    downloadLink.click();
            }*/
            if (border)
            {
                table.QuerySelectorAll("td").SelectForeach(x =>
                {
                    /*@
                     x.style="";
                     */
                });
                table.QuerySelectorAll("th").SelectForeach(x =>
                {
                    /*@
                     x.style="";
                     */
                });
            }
        }

    }
}
