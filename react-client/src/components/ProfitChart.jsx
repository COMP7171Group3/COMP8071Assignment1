import { useState, useEffect } from 'react';
import { Bar } from 'react-chartjs-2';
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  BarElement,
  Title,
  Tooltip,
  Legend
} from 'chart.js';

// Register ChartJS components
ChartJS.register(
  CategoryScale,
  LinearScale,
  BarElement,
  Title,
  Tooltip,
  Legend
);

const ProfitChart = () => {
  const [data, setData] = useState([]);
  const [metric, setMetric] = useState('profit');

  const labelMap = {
    profit: 'Profit',
    damages: 'Maintenance Cost',
    staffing: 'Demand vs Staffing Capacity',
    collectionrate: 'Customer Retention Rate'
  };

  useEffect(() => {
    const loadData = async () => {
      const res = await fetch(`/api/reports/analytics?metric=${metric}`);
      const jsonData = await res.json();
      setData(jsonData);
    };

    loadData();
  }, [metric]);

  const labelKey = data.length ? Object.keys(data[0])[0] : 'label';
  const valueKey = data.length ? Object.keys(data[0]).find(k => k.toLowerCase().includes('metric')) : 'metricValue';

  const chartData = {
    labels: data.map(d => d[labelKey]),
    datasets: [{
      label: labelMap[metric],
      data: data.map(d => d[valueKey]),
      backgroundColor: 'rgba(54, 162, 235, 0.6)'
    }]
  };

  const options = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { display: true },
      title: { display: true, text: `${labelMap[metric]} Analysis` }
    }
  };

  return (
    <div className="chart-container">
      <header>
        <h2>Service Profit Analysis</h2>
        <label htmlFor="analysisType">Choose an analysis: </label>
        <select 
          id="analysisType"
          value={metric}
          onChange={(e) => setMetric(e.target.value)}
        >
          <option value="profit">Profit By Service</option>
          <option value="damages">Maintenance Cost</option>
          <option value="staffing">Demand vs Staffing Capacity</option>
          <option value="collectionrate">Customer Retention</option>
        </select>
        <button style={{ marginLeft: '10px' }} onClick={() => window.print()}>Print</button>
      </header>
      <div
        className="chart-wrapper"
        style={{ margin: '16px auto', width: '90%', maxWidth: '1100px', height: 500 }}
      >
        <Bar data={chartData} options={options} />
      </div>
    </div>
  );
};

export default ProfitChart;