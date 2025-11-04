# Analytics Dashboard demo 8071

A full-stack analytics dashboard that visualizes service profit data with interactive charts and tables.
The app uses **.NET 9**, **React**, and **SQL Server** for ETL-based data reporting.

---

## Requirements

Before running this project, ensure the following are installed:

* **.NET SDK 9.0+**
* **Node.js & npm**
* **SQL Server** (Docker container or full install)

---

## Setup Instructions

### 1. Install React Client Dependencies

Navigate to the frontend directory and install dependencies:

```bash
cd react-client
npm install
```

### 2. Build the Frontend

Build the React client and output it to the correct location for the .NET app to serve:

```bash
npm run build --emptyOutDir
```

This ensures the compiled frontend is placed inside the backend’s `wwwroot` folder.

---

### 3. Configure Database Connection

Open **`appsettings.json`** inside the `/app/` directory and update the connection strings to match your environment:

```json
"ConnectionStrings": {
  "OltpConnection": "Server=localhost;Database=OLTP_DB;User Id=sa;Password=your_password;",
  "OlapConnection": "Server=localhost;Database=OLAP_DB;User Id=sa;Password=your_password;"
}
```

If using Docker, ensure your container exposes port **1433** and update the host accordingly.

---

### 4. Run the Application

From the `/app` directory, run:

```bash
dotnet run
```

The backend will start up, and the compiled frontend will be available at the same URL (typically `http://localhost:5292`).

---

## Using the App

Once running:

1. Open the app in your browser.
2. Use the **dropdown menu** to select an analysis metric (e.g., Profit, Maintenance Cost).
3. Click **“Run ETL”** to load sample analytics data into the database.
4. View the generated chart or toggle the **table view**.
5. Click **“Purge DB”** to clear the database when done.

---

## Optional: Run SQL Server via Docker

If you don’t have SQL Server installed locally, you can quickly spin one up:

```bash
docker run -e 'ACCEPT_EULA=Y' -e 'SA_PASSWORD=YourStrong!Pass123' \
   -p 1433:1433 --name sqlserver \
   -d mcr.microsoft.com/mssql/server:2022-latest
```

Then update your connection strings to use:

```
Server=localhost,1433;User Id=sa;Password=YourStrong!Pass123;
```


