using System.Data.Odbc;
using System.Text;

public class ETLService
{
    private readonly string _oltpConnectionString;
    private readonly string _olapConnectionString;
    public ETLService(string oltpConnectionString, string olapConnectionString)
    {
        _oltpConnectionString = oltpConnectionString;
        _olapConnectionString = olapConnectionString;
    }

    public async Task RunETLAsync(StringBuilder log)
    {
        await using var oltpConn = new OdbcConnection(_oltpConnectionString);
        await using var olapConn = new OdbcConnection(_olapConnectionString);

        await oltpConn.OpenAsync();
        await olapConn.OpenAsync();

        await ClearOlapTablesAsync(olapConn, log);
        await LoadDimEmployeesAsync(oltpConn, olapConn, log);
        await LoadDimClientsAsync(oltpConn, olapConn, log);
        await LoadDimServicesAsync(oltpConn, olapConn, log);
        await LoadDimAssetsAsync(oltpConn, olapConn, log);
        await LoadDimRentersAsync(oltpConn, olapConn, log);
        await LoadFactPayrollAsync(oltpConn, olapConn, log);
        await LoadFactShiftsAsync(oltpConn, olapConn, log);
        await LoadFactAttendanceAsync(oltpConn, olapConn, log);
        await LoadFactServiceAssignmentAsync(oltpConn, olapConn, log);
        await LoadFactInvoiceAsync(oltpConn, olapConn, log);
        await LoadFactServiceRegistrationAsync(oltpConn, olapConn, log);
        await LoadFactDamageReportAsync(oltpConn, olapConn, log);
        await LoadFactRentalHistoryAsync(oltpConn, olapConn, log);
    }

    public async Task ClearOlapTablesAsync(OdbcConnection olapConn, StringBuilder log)
    {
        var tables = new string[]
        {
            "FactAttendance","FactPayroll","FactShifts","FactServiceAssignment","FactInvoice",
            "FactServiceRegistration","FactDamageReport","FactRentalHistory",
            "DimEmployee","DimClient","DimService","DimAsset","DimRenter"
        };

        foreach (var table in tables)
        {
            log.AppendLine($"Clearing table {table}...");
            using var cmd = new OdbcCommand($"DELETE FROM {table};", olapConn);
            await cmd.ExecuteNonQueryAsync();
        }
    }
    public async Task LoadDimEmployeesAsync(OdbcConnection oltpConn, OdbcConnection olapConn, StringBuilder log)
    {
        log.AppendLine("Loading DimEmployees...");
        using var selectCmd = new OdbcCommand(
            "SELECT EmployeeID, Name, Address, JobTitle, EmployeeType, SalaryRate, ReportsTo FROM Employee", oltpConn);

        using var reader = await selectCmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var insertCmd = new OdbcCommand(
                "INSERT INTO DimEmployee (EmployeeID, Name, Address, JobTitle, EmployeeType, SalaryRate, ReportsTo) " +
                "VALUES (?, ?, ?, ?, ?, ?, ?)", olapConn);

            insertCmd.Parameters.AddWithValue("@EmployeeID", reader["EmployeeID"]);
            insertCmd.Parameters.AddWithValue("@Name", reader["Name"]);
            insertCmd.Parameters.AddWithValue("@Address", reader["Address"]);
            insertCmd.Parameters.AddWithValue("@JobTitle", reader["JobTitle"]);
            insertCmd.Parameters.AddWithValue("@EmployeeType", reader["EmployeeType"]);
            insertCmd.Parameters.AddWithValue("@SalaryRate", reader["SalaryRate"]);
            insertCmd.Parameters.AddWithValue("@ReportsTo", reader["ReportsTo"] is DBNull ? DBNull.Value : reader["ReportsTo"]);

            await insertCmd.ExecuteNonQueryAsync();
            log.AppendLine($"Inserted EmployeeID {reader["EmployeeID"]}");
        }
        log.AppendLine("DimEmployees loaded successfully.");
    }
    public async Task LoadDimClientsAsync(OdbcConnection oltpConn, OdbcConnection olapConn, StringBuilder log)
    {
        log.AppendLine("Loading DimClients...");
        using var reader = await new OdbcCommand("SELECT ClientID, Name, Address, ContactInfo FROM Client", oltpConn)
            .ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var cmd = new OdbcCommand(
                "INSERT INTO DimClient (ClientID, Name, Address, ContactInfo) VALUES (?, ?, ?, ?)", olapConn);
            cmd.Parameters.AddWithValue("@ClientID", reader["ClientID"]);
            cmd.Parameters.AddWithValue("@Name", reader["Name"]);
            cmd.Parameters.AddWithValue("@Address", reader["Address"]);
            cmd.Parameters.AddWithValue("@ContactInfo", reader["ContactInfo"]);
            await cmd.ExecuteNonQueryAsync();
            log.AppendLine($"Inserted ClientID {reader["ClientID"]}");
        }
        log.AppendLine("DimClients loaded successfully.");
    }
    public async Task LoadDimServicesAsync(OdbcConnection oltpConn, OdbcConnection olapConn, StringBuilder log)
    {
        log.AppendLine("Loading DimServices...");
        using var reader = await new OdbcCommand(
            @"SELECT st.ServiceTypeID, st.ServiceName, st.Rate, st.RequiresCertification
              FROM ServiceType st", oltpConn).ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var cmd = new OdbcCommand(
                "INSERT INTO DimService (ServiceID, ServiceName, Rate, RequiresCertification) VALUES (?, ?, ?, ?)", olapConn);
            cmd.Parameters.AddWithValue("@ServiceID", reader["ServiceTypeID"]);
            cmd.Parameters.AddWithValue("@ServiceName", reader["ServiceName"]);
            cmd.Parameters.AddWithValue("@Rate", reader["Rate"]);
            cmd.Parameters.AddWithValue("@RequiresCertification", reader["RequiresCertification"]);
            await cmd.ExecuteNonQueryAsync();
            log.AppendLine($"Inserted ServiceID {reader["ServiceTypeID"]}");
        }
        log.AppendLine("DimServices loaded successfully.");
    }
    public async Task LoadDimAssetsAsync(OdbcConnection oltpConn, OdbcConnection olapConn, StringBuilder log)
    {
        log.AppendLine("Loading DimAssets...");
        using var reader = await new OdbcCommand("SELECT AssetID, AssetType, Location, MonthlyRent FROM Asset", oltpConn)
            .ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var cmd = new OdbcCommand(
                "INSERT INTO DimAsset (AssetID, AssetType, Location, MonthlyRent) VALUES (?, ?, ?, ?)", olapConn);
            cmd.Parameters.AddWithValue("@AssetID", reader["AssetID"]);
            cmd.Parameters.AddWithValue("@AssetType", reader["AssetType"]);
            cmd.Parameters.AddWithValue("@Location", reader["Location"]);
            cmd.Parameters.AddWithValue("@MonthlyRent", reader["MonthlyRent"]);
            await cmd.ExecuteNonQueryAsync();
            log.AppendLine($"Inserted AssetID {reader["AssetID"]}");
        }
        log.AppendLine("DimAssets loaded successfully.");
    }

    public async Task LoadDimRentersAsync(OdbcConnection oltpConn, OdbcConnection olapConn, StringBuilder log)
    {
        log.AppendLine("Loading DimRenters...");
        using var reader = await new OdbcCommand("SELECT RenterID, Name, EmergencyContact, FamilyDoctor FROM Renter", oltpConn)
            .ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var cmd = new OdbcCommand(
                "INSERT INTO DimRenter (RenterID, Name, EmergencyContact, FamilyDoctor) VALUES (?, ?, ?, ?)", olapConn);
            cmd.Parameters.AddWithValue("@RenterID", reader["RenterID"]);
            cmd.Parameters.AddWithValue("@Name", reader["Name"]);
            cmd.Parameters.AddWithValue("@EmergencyContact", reader["EmergencyContact"]);
            cmd.Parameters.AddWithValue("@FamilyDoctor", reader["FamilyDoctor"]);
            await cmd.ExecuteNonQueryAsync();
            log.AppendLine($"Inserted RenterID {reader["RenterID"]}");
        }
        log.AppendLine("DimRenters loaded successfully.");
    }
    public async Task LoadFactPayrollAsync(OdbcConnection oltpConn, OdbcConnection olapConn, StringBuilder log)
    {
        log.AppendLine("Loading FactPayroll...");
        using var reader = await new OdbcCommand(
            "SELECT PaymentID, EmployeeID, PayDate, OverTimePay, Deductions, NetPay FROM Payment", oltpConn)
            .ExecuteReaderAsync();

        Random rand = new Random();

        while (await reader.ReadAsync())
        {   
            int baseSalary = rand.Next(1, 11) * 100;
            var cmd = new OdbcCommand(
                @"INSERT INTO FactPayroll 
                (PayrollID, DimEmployeeEmployeeID, PayDate, BaseSalary, OverTimePay, Deductions, NetPay) 
                VALUES (?, ?, ?, ?, ?, ?, ?)", olapConn);
            cmd.Parameters.AddWithValue("@PayrollID", reader["PaymentID"]);
            cmd.Parameters.AddWithValue("@DimEmployeeEmployeeID", reader["EmployeeID"]);
            cmd.Parameters.AddWithValue("@PayDate", reader["PayDate"]);
            cmd.Parameters.AddWithValue("@BaseSalary", baseSalary);
            cmd.Parameters.AddWithValue("@OverTimePay", reader["OverTimePay"]);
            cmd.Parameters.AddWithValue("@Deductions", reader["Deductions"]);
            cmd.Parameters.AddWithValue("@NetPay", reader["NetPay"]);
            await cmd.ExecuteNonQueryAsync();
            log.AppendLine($"Inserted PayrollID {reader["PaymentID"]}");
        }
        log.AppendLine("FactPayroll loaded successfully.");
    }
    public async Task LoadFactShiftsAsync(OdbcConnection oltpConn, OdbcConnection olapConn, StringBuilder log)
    {
        log.AppendLine("Loading FactShifts...");
        using var reader = await new OdbcCommand(
            "SELECT ShiftID, EmployeeID, StartTime, EndTime, IsOnCall FROM Shift", oltpConn)
            .ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var cmd = new OdbcCommand(
                @"INSERT INTO FactShifts 
                (ShiftID, DimEmployeeEmployeeID, StartTime, EndTime, IsOnCall) 
                VALUES (?, ?, ?, ?, ?)", olapConn);

            cmd.Parameters.AddWithValue("@ShiftID", reader["ShiftID"]);
            cmd.Parameters.AddWithValue("@DimEmployeeEmployeeID", reader["EmployeeID"]);
            cmd.Parameters.AddWithValue("@StartTime", reader["StartTime"]);
            cmd.Parameters.AddWithValue("@EndTime", reader["EndTime"]);
            cmd.Parameters.AddWithValue("@IsOnCall", reader["IsOnCall"]);

            await cmd.ExecuteNonQueryAsync();
            log.AppendLine($"Inserted ShiftID {reader["ShiftID"]}");
        }
        log.AppendLine("FactShifts loaded successfully.");
    }
    public async Task LoadFactAttendanceAsync(OdbcConnection oltpConn, OdbcConnection olapConn, StringBuilder log)
    {
        log.AppendLine("Loading FactAttendance...");
        using var reader = await new OdbcCommand(
        @"SELECT s.ShiftID, s.EmployeeID, s.IsOnCall 
          FROM Shift s", oltpConn)
        .ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var cmd = new OdbcCommand(
                @"INSERT INTO FactAttendance 
                (AttendanceID, DimEmployeeEmployeeID, DimFactShiftsShiftID, IsHoliday, IsVacation, IsOnCall) 
                VALUES (?, ?, ?, 0, 0, ?)", olapConn);

            cmd.Parameters.AddWithValue("@AttendanceID", reader["ShiftID"]); // using ShiftID as AttendanceID for demo
            cmd.Parameters.AddWithValue("@DimEmployeeEmployeeID", reader["EmployeeID"]);
            cmd.Parameters.AddWithValue("@DimFactShiftsShiftID", reader["ShiftID"]);
            cmd.Parameters.AddWithValue("@IsOnCall", reader["IsOnCall"]);

            await cmd.ExecuteNonQueryAsync();
            log.AppendLine($"Inserted AttendanceID {reader["ShiftID"]}");
        }
        log.AppendLine("FactAttendance loaded successfully.");
    }
    public async Task LoadFactServiceAssignmentAsync(OdbcConnection oltpConn, OdbcConnection olapConn, StringBuilder log)
    {
        log.AppendLine("Loading FactServiceAssignment...");
        using var reader = await new OdbcCommand(
        @"SELECT ServiceID, ServiceTypeID, EmployeeID, ScheduledDate FROM Service", oltpConn)
        .ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var cmd = new OdbcCommand(
                @"INSERT INTO FactServiceAssignment 
                (AssignedID, DimEmployeeEmployeeID, DimServiceServiceID, ScheduledDate) 
                VALUES (?, ?, ?, ?)", olapConn);

            cmd.Parameters.AddWithValue("@AssignedID", reader["ServiceID"]);
            cmd.Parameters.AddWithValue("@DimEmployeeEmployeeID", reader["EmployeeID"]);
            cmd.Parameters.AddWithValue("@DimServiceServiceID", reader["ServiceTypeID"]);
            cmd.Parameters.AddWithValue("@ScheduledDate", reader["ScheduledDate"]);

            await cmd.ExecuteNonQueryAsync();
            log.AppendLine($"Inserted AssignedID {reader["ServiceID"]}");
        }
        log.AppendLine("FactServiceAssignment loaded successfully.");
    }

    public async Task LoadFactInvoiceAsync(OdbcConnection oltpConn, OdbcConnection olapConn, StringBuilder log)
    {
        log.AppendLine("Loading FactInvoice...");
        using var reader = await new OdbcCommand(
            @"SELECT ServiceID, ServiceTypeID, ClientID, ScheduledDate, TotalAmount, IsPaid FROM Service", oltpConn)
            .ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var cmd = new OdbcCommand(
                @"INSERT INTO FactInvoice 
                (InvoiceID, DimClientClientID, DimServiceServiceID, InvoiceDate, TotalAmount, IsPaid) 
                VALUES (?, ?, ?, ?, ?, ?)", olapConn);

            cmd.Parameters.AddWithValue("@InvoiceID", reader["ServiceID"]);
            cmd.Parameters.AddWithValue("@DimClientClientID", reader["ClientID"]);
            cmd.Parameters.AddWithValue("@DimServiceServiceID", reader["ServiceTypeID"]);
            cmd.Parameters.AddWithValue("@InvoiceDate", reader["ScheduledDate"]);
            cmd.Parameters.AddWithValue("@TotalAmount", reader["TotalAmount"]);
            cmd.Parameters.AddWithValue("@IsPaid", reader["IsPaid"]);

            await cmd.ExecuteNonQueryAsync();
            log.AppendLine($"Inserted InvoiceID {reader["ServiceID"]}");
        }
        log.AppendLine("FactInvoice loaded successfully.");
    }
    public async Task LoadFactServiceRegistrationAsync(OdbcConnection oltpConn, OdbcConnection olapConn, StringBuilder log)
    {
        log.AppendLine("Loading FactServiceRegistration...");
        using var reader = await new OdbcCommand(
            @"SELECT ServiceID, ServiceTypeID, ClientID, RegistrationDate FROM Service", oltpConn)
            .ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var cmd = new OdbcCommand(
                @"INSERT INTO FactServiceRegistration 
                (RegistrationID, DimClientClientID, DimServiceServiceID, RegistrationDate) 
                VALUES (?, ?, ?, ?)", olapConn);

            cmd.Parameters.AddWithValue("@RegistrationID", reader["ServiceID"]);
            cmd.Parameters.AddWithValue("@DimClientClientID", reader["ClientID"]);
            cmd.Parameters.AddWithValue("@DimServiceServiceID", reader["ServiceTypeID"]);
            cmd.Parameters.AddWithValue("@RegistrationDate", reader["RegistrationDate"]);

            await cmd.ExecuteNonQueryAsync();
            log.AppendLine($"Inserted RegistrationID {reader["ServiceID"]}");
        }
        log.AppendLine("FactServiceRegistration loaded successfully.");
    }
    public async Task LoadFactDamageReportAsync(OdbcConnection oltpConn, OdbcConnection olapConn, StringBuilder log)
    {
        log.AppendLine("Loading FactDamageReport...");
        using var reader = await new OdbcCommand(
            @"SELECT AssetID FROM Asset", oltpConn)
            .ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var cmd = new OdbcCommand(
                @"INSERT INTO FactDamageReport 
                (ReportID, DimAssetAssetID, ReportDate, RepairCost, Description) 
                VALUES (?, ?, GETDATE(), ?, ?)", olapConn);

            cmd.Parameters.AddWithValue("@ReportID", reader["AssetID"]);
            cmd.Parameters.AddWithValue("@DimAssetAssetID", reader["AssetID"]);
            cmd.Parameters.AddWithValue("@RepairCost", 130.0);
            cmd.Parameters.AddWithValue("@Description", "Auto-generated damage report");

            await cmd.ExecuteNonQueryAsync();
            log.AppendLine($"Inserted ReportID {reader["AssetID"]}");
        }
        log.AppendLine("FactDamageReport loaded successfully.");
    }
    public async Task LoadFactRentalHistoryAsync(OdbcConnection oltpConn, OdbcConnection olapConn, StringBuilder log)
    {
        log.AppendLine("Loading FactRentalHistory...");
        using var reader = await new OdbcCommand(
            @"SELECT AssetRentID, AssetID, RenterID, StartDate, EndDate, 1000 AS RentAmount FROM AssetRent", oltpConn)
            .ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var cmd = new OdbcCommand(
                @"INSERT INTO FactRentalHistory 
                (HistoryID, DimAssetAssetID, DimRenterRenterID, StartDate, EndDate, RentAmount) 
                VALUES (?, ?, ?, ?, ?, ?)", olapConn);

            cmd.Parameters.AddWithValue("@HistoryID", reader["AssetRentID"]);
            cmd.Parameters.AddWithValue("@DimAssetAssetID", reader["AssetID"]);
            cmd.Parameters.AddWithValue("@DimRenterRenterID", reader["RenterID"]);
            cmd.Parameters.AddWithValue("@StartDate", reader["StartDate"]);
            cmd.Parameters.AddWithValue("@EndDate", reader["EndDate"]);
            cmd.Parameters.AddWithValue("@RentAmount", reader["RentAmount"]);

            await cmd.ExecuteNonQueryAsync();
            log.AppendLine($"Inserted HistoryID {reader["AssetRentID"]}");
        }
        log.AppendLine("FactRentalHistory loaded successfully.");
    }
}
