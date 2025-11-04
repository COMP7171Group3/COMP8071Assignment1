import { useState, useEffect, useCallback } from 'react';
import { Bar } from 'react-chartjs-2';
import { Chart as ChartJS, CategoryScale, LinearScale, BarElement, Title, Tooltip, Legend } from 'chart.js';
import { Printer, FileSpreadsheet, Eye, EyeOff, DatabaseBackup, DatabaseZap } from 'lucide-react';

import { useReward } from 'partycles';





ChartJS.register(CategoryScale, LinearScale, BarElement, Title, Tooltip, Legend);

const labelMap = {
  profit: 'Profit',
  damages: 'Maintenance Cost',
  staffing: 'Demand vs Staffing Capacity',
  collectionrate: 'Customer Retention Rate'
};

const ProfitChart = () => {
  const [data, setData] = useState([]);
  const [metric, setMetric] = useState('profit');
  const [showTable, setShowTable] = useState(false);
  const [loading, setLoading] = useState(false);
  const [isDbEmpty, setIsDbEmpty] = useState(true);
  const [theme, setTheme] = useState('light');

  // --- Initialize confetti effect ---
  const { reward, isAnimating } = useReward('rewardId', 'confetti');

  // --- Detect system theme ---
  useEffect(() => {
    const mq = window.matchMedia('(prefers-color-scheme: dark)');
    const updateTheme = () => setTheme(mq.matches ? 'dark' : 'light');
    updateTheme();
    mq.addEventListener('change', updateTheme);
    return () => mq.removeEventListener('change', updateTheme);
  }, []);

  // --- Common reusable fetch function ---
  const fetchMetricData = useCallback(async (metricName = metric) => {
    try {
      const res = await fetch(`/api/reports/analytics?metric=${metricName}`);
      const jsonData = await res.json();
      setData(jsonData);
      setIsDbEmpty(jsonData.length === 0);
    } catch (err) {
      console.error('Failed to load data:', err);
    }
  }, [metric]);

  useEffect(() => {
    fetchMetricData();
  }, [metric, fetchMetricData]);

  const labelKey = data.length ? Object.keys(data[0])[0] : 'label';
  const valueKey =
    data.length && Object.keys(data[0]).find(k => k.toLowerCase().includes('metric'))
      ? Object.keys(data[0]).find(k => k.toLowerCase().includes('metric'))
      : data.length ? Object.keys(data[0])[1] : 'metricValue';

  const chartData = {
    labels: data.map(d => d[labelKey]),
    datasets: [
      {
        label: labelMap[metric],
        data: data.map(d => d[valueKey]),
        backgroundColor: theme === 'dark' ? 'rgba(255, 137, 2, 0.6)' : 'rgba(54, 162, 235, 0.6)',
        borderRadius: 6,
      }
    ]
  };

  const chartOptions = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { display: true, labels: { color: theme === 'dark' ? '#ddd' : '#222' } },
      title: {
        display: true,
        text: `${labelMap[metric]} Analysis`,
        color: theme === 'dark' ? '#fff' : '#000'
      }
    },
    scales: {
      x: { ticks: { color: theme === 'dark' ? '#ccc' : '#333' } },
      y: { ticks: { color: theme === 'dark' ? '#ccc' : '#333' } }
    }
  };

  const handleDownloadExcel = async () => {
    setLoading(true);
    try {
      const response = await fetch(`http://localhost:5292/api/reports/export?metric=${metric}`);
      const blob = await response.blob();
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `${metric}-analytics.xlsx`;
      a.click();
      URL.revokeObjectURL(url);
    } catch (err) {
      console.error('Download failed', err);
    } finally {
      setLoading(false);
    }
  };

  const handleEtlClick = async () => {
    const endpoint = isDbEmpty
      ? 'http://localhost:5292/api/etl/run'
      : 'http://localhost:5292/api/etl/purge';

    try {
      await fetch(endpoint, { method: 'GET' });
      await fetchMetricData();
    } catch (err) {
      console.error('ETL action failed:', err);
    }
  };

  return (
    <div className={`dashboard ${theme}`} data-theme={theme}>
      <header>
        <h2>Analysis</h2>
        <div className="controls">
          <select value={metric} onChange={(e) => {
            setMetric(e.target.value);
            reward();
          }}>
            <option value="profit">Profit By Service</option>
            <option value="damages">Maintenance Cost</option>
            <option value="staffing">Demand vs Staffing Capacity</option>
            <option value="collectionrate">Customer Retention</option>
          </select>

          <div className="button-group">
            <button onClick={() => window.print()}>
              <Printer size={16} /> Print
            </button>
            <button onClick={handleDownloadExcel} disabled={loading}>
              <FileSpreadsheet size={16} />
              {loading ? 'Preparing…' : 'Download Excel'}
            </button>
            <button onClick={() => setShowTable(p => !p)}>
              {showTable ? <><EyeOff size={16} /> Hide Table</> : <><Eye size={16} /> Show Table</>}
            </button>
            <button onClick={handleEtlClick}>
              {isDbEmpty ? <><DatabaseZap size={16} /> Run ETL</> : <><DatabaseBackup size={16} /> Purge DB</>}
            </button>
          </div>
        </div>
      </header>

      <div id='rewardId' className="chart-wrapper">
        <Bar data={chartData} options={chartOptions} />
      </div>

      {showTable && data.length > 0 && (
        <div className="table-container">
          <h3>{labelMap[metric]} Data</h3>
          <table>
            <thead>
              <tr>
                {Object.keys(data[0]).map((key) => (
                  <th key={key}>{key.charAt(0).toUpperCase() + key.slice(1)}</th>
                ))}
              </tr>
            </thead>
            <tbody>
              {data.map((row, i) => (
                <tr key={i}>
                  {Object.values(row).map((val, j) => (
                    <td key={j}>{val}</td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
};
  

//    <button id="rewardId" onClick={reward}>
//      Click me!
//    </button>

export default ProfitChart;
