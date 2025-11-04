-- ======================================================
-- Create OLTP Database
-- ======================================================
CREATE DATABASE CareServicesOLTP;
GO

USE CareServicesOLTP;
GO

-- ======================================================
-- Tables
-- ======================================================

CREATE TABLE Employee (
    EmployeeID INT PRIMARY KEY,
    Name VARCHAR(255) NOT NULL,
    Address VARCHAR(255),
    JobTitle VARCHAR(255),
    EmployeeType VARCHAR(255),
    SalaryRate REAL,
    ReportsTo INT NULL
);

CREATE TABLE Payment (
    PaymentID INT PRIMARY KEY,
    EmployeeID INT NOT NULL,
    PayDate DATE NOT NULL,
    OverTimePay REAL NOT NULL,
    Deductions REAL NOT NULL,
    BasePay REAL NOT NULL,
    FOREIGN KEY (EmployeeID) REFERENCES Employee(EmployeeID)
);

CREATE TABLE Shift (
    ShiftID INT PRIMARY KEY,
    EmployeeID INT NOT NULL,
    StartTime DATE NOT NULL,
    EndTime DATE NOT NULL,
    IsOnCall BIT NOT NULL,
    FOREIGN KEY (EmployeeID) REFERENCES Employee(EmployeeID)
);

CREATE TABLE Client (
    ClientID INT PRIMARY KEY,
    Name VARCHAR(255) NOT NULL,
    Address VARCHAR(255),
    ContactInfo VARCHAR(255)
);

CREATE TABLE ServiceType (
    ServiceTypeID INT PRIMARY KEY,
    ServiceName VARCHAR(255),
    Rate REAL,
    RequiresCertification BIT
);

CREATE TABLE Service (
    ServiceID INT PRIMARY KEY,
    ServiceTypeID INT NOT NULL,
    EmployeeID INT NOT NULL,
    ClientID INT NOT NULL,
    RegistrationDate DATE NOT NULL,
    ScheduledDate DATE NOT NULL,
    TotalAmount REAL NOT NULL,
    IsPaid BIT NOT NULL,
    FOREIGN KEY (EmployeeID) REFERENCES Employee(EmployeeID),
    FOREIGN KEY (ServiceTypeID) REFERENCES ServiceType(ServiceTypeID),
    FOREIGN KEY (ClientID) REFERENCES Client(ClientID)
);

CREATE TABLE Asset (
    AssetID INT PRIMARY KEY,
    AssetType VARCHAR(255),
    Location VARCHAR(255),
    MonthlyRent REAL
);

CREATE TABLE Renter (
    RenterID INT PRIMARY KEY,
    Name VARCHAR(255),
    EmergencyContact VARCHAR(255),
    FamilyDoctor VARCHAR(255)
);

CREATE TABLE AssetRent (
    AssetRentID INT PRIMARY KEY,
    AssetID INT NOT NULL,
    RenterID INT NOT NULL,
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    FOREIGN KEY (AssetID) REFERENCES Asset(AssetID),
    FOREIGN KEY (RenterID) REFERENCES Renter(RenterID)
);

GO
