-- ======================================================
-- Create OLAP Database
-- ======================================================
CREATE DATABASE CareServicesOLAP;
GO

USE CareServicesOLAP;
GO

-- ======================================================
-- Dimension Tables
-- ======================================================

CREATE TABLE DimEmployee (
    EmployeeID INT PRIMARY KEY,
    Name VARCHAR(255) NOT NULL,
    Address VARCHAR(255),
    JobTitle VARCHAR(255),
    EmployeeType VARCHAR(255),
    SalaryRate REAL,
    ReportsTo INT NULL
);

CREATE TABLE DimClient (
    ClientID INT PRIMARY KEY,
    Name VARCHAR(255) NOT NULL,
    Address VARCHAR(255),
    ContactInfo VARCHAR(255)
);

CREATE TABLE DimService (
    ServiceID INT PRIMARY KEY,
    ServiceName VARCHAR(255),
    Rate REAL,
    RequiresCertification BIT
);

CREATE TABLE DimAsset (
    AssetID INT PRIMARY KEY,
    AssetType VARCHAR(255),
    Location VARCHAR(255),
    MonthlyRent REAL
);

CREATE TABLE DimRenter (
    RenterID INT PRIMARY KEY,
    Name VARCHAR(255),
    EmergencyContact VARCHAR(255),
    FamilyDoctor VARCHAR(255)
);

-- ======================================================
-- Fact Tables
-- ======================================================

CREATE TABLE FactPayroll (
    PayrollID INT PRIMARY KEY,
    DimEmployeeEmployeeID INT NOT NULL,
    PayDate DATE NOT NULL,
    BaseSalary REAL NOT NULL,
    OverTimePay REAL NOT NULL,
    Deductions REAL NOT NULL,
    NetPay REAL NOT NULL,
    FOREIGN KEY (DimEmployeeEmployeeID) REFERENCES DimEmployee(EmployeeID)
);

CREATE TABLE FactShifts (
    ShiftID INT PRIMARY KEY,
    DimEmployeeEmployeeID INT NOT NULL,
    StartTime DATE NOT NULL,
    EndTime DATE NOT NULL,
    IsOnCall BIT NOT NULL,
    FOREIGN KEY (DimEmployeeEmployeeID) REFERENCES DimEmployee(EmployeeID)
);

CREATE TABLE FactAttendance (
    AttendanceID INT PRIMARY KEY,
    DimEmployeeEmployeeID INT NOT NULL,
    DimFactShiftsShiftID INT NOT NULL,
    IsHoliday BIT NOT NULL,
    IsVacation BIT NOT NULL,
    IsOnCall BIT NOT NULL,
    FOREIGN KEY (DimEmployeeEmployeeID) REFERENCES DimEmployee(EmployeeID),
    FOREIGN KEY (DimFactShiftsShiftID) REFERENCES FactShifts(ShiftID)
);

CREATE TABLE FactServiceAssignment (
    AssignedID INT PRIMARY KEY,
    DimEmployeeEmployeeID INT NOT NULL,
    DimServiceServiceID INT NOT NULL,
    ScheduledDate DATE NOT NULL,
    FOREIGN KEY (DimEmployeeEmployeeID) REFERENCES DimEmployee(EmployeeID),
    FOREIGN KEY (DimServiceServiceID) REFERENCES DimService(ServiceID)
);

CREATE TABLE FactInvoice (
    InvoiceID INT PRIMARY KEY,
    DimClientClientID INT NOT NULL,
    DimServiceServiceID INT NOT NULL,
    InvoiceDate DATE NOT NULL,
    TotalAmount REAL NOT NULL,
    IsPaid BIT NOT NULL,
    FOREIGN KEY (DimClientClientID) REFERENCES DimClient(ClientID),
    FOREIGN KEY (DimServiceServiceID) REFERENCES DimService(ServiceID)
);

CREATE TABLE FactServiceRegistration (
    RegistrationID INT PRIMARY KEY,
    DimClientClientID INT NOT NULL,
    DimServiceServiceID INT NOT NULL,
    RegistrationDate DATE NOT NULL,
    FOREIGN KEY (DimClientClientID) REFERENCES DimClient(ClientID),
    FOREIGN KEY (DimServiceServiceID) REFERENCES DimService(ServiceID)
);

CREATE TABLE FactDamageReport (
    ReportID INT PRIMARY KEY,
    DimAssetAssetID INT NOT NULL,
    ReportDate DATE NOT NULL,
    RepairCost REAL NOT NULL,
    Description VARCHAR(255),
    FOREIGN KEY (DimAssetAssetID) REFERENCES DimAsset(AssetID)
);

CREATE TABLE FactRentalHistory (
    HistoryID INT PRIMARY KEY,
    DimAssetAssetID INT NOT NULL,
    DimRenterRenterID INT NOT NULL,
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    RentAmount REAL NOT NULL,
    FOREIGN KEY (DimAssetAssetID) REFERENCES DimAsset(AssetID),
    FOREIGN KEY (DimRenterRenterID) REFERENCES DimRenter(RenterID)
);
GO
