async function exportToExcel(data, metric) {
    if (!data || !data.length) {
        alert('No data available to export.');
        return;
    }

    const canvas = document.getElementById('profitChart');
    const chartImage = canvas.toDataURL('image/png');

    const workbook = new ExcelJS.Workbook();
    const sheet = workbook.addWorksheet('Profit Analysis');

    // Add headers
    sheet.addRow(['Service Name', metric.charAt(0).toUpperCase() + metric.slice(1)]);

    // Add data rows
    data.forEach(d => sheet.addRow([d.serviceName, d.metricValue]));

    // Add chart image below data
    const imageId = workbook.addImage({
        base64: chartImage,
        extension: 'png',
    });
    sheet.addImage(imageId, {
        tl: { col: 0, row: data.length + 2 },
        ext: { width: 500, height: 300 }
    });

    // Generate Excel file
    const buffer = await workbook.xlsx.writeBuffer();
    const blob = new Blob([buffer], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' });
    const url = URL.createObjectURL(blob);

    const a = document.createElement('a');
    a.href = url;
    a.download = `${metric}-analysis.xlsx`;
    a.click();

    URL.revokeObjectURL(url);
}
