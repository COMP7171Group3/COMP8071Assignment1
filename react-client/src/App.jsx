import React, { useState } from 'react';
import ProfitChart from './components/ProfitChart';
import './styles/ProfitChart.css';

function App() {
  const [loading, setLoading] = useState(false);

  const handleDownload = async () => {
    setLoading(true);


    try {
      const response = await fetch('http://localhost:5292/api/reports/export?metric=profit');
      const blob = await response.blob();
      const url = URL.createObjectURL(blob);

      const a = document.createElement('a');
      a.href = url;
      a.download = 'profit-analytics.xlsx';
      a.click();

      URL.revokeObjectURL(url);
    } catch (err) {
      console.error('Download failed', err);
    } finally {
      setLoading(false);
    }


  };

  return (
    <div className="App">
      <ProfitChart />
      <button onClick={handleDownload} disabled={loading}>
        {loading ? 'Preparing…' : 'Download Excel'}
      </button>
    </div>
  );
}

export default App;
