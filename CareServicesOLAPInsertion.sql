USE CareServicesOLAP;
GO

-- =========================
-- DIMENSION TABLES
-- =========================

-- Employees
INSERT INTO DimEmployee (EmployeeID, Name, Address, JobTitle, EmployeeType, SalaryRate, ReportsTo)
VALUES
(1, 'Alice Johnson', '123 Main St', 'Caregiver', 'Full-Time', 25.50, NULL),
(2, 'Bob Smith', '456 Elm St', 'Nurse', 'Part-Time', 30.00, 1),
(3, 'Charlie Green', '789 Pine St', 'Supervisor', 'Full-Time', 35.75, NULL),
(4, 'Diana Brown', '15 Oak Ave', 'Cleaner', 'Contract', 18.00, 3),
(5, 'Ethan Clark', '27 Cedar Rd', 'Physiotherapist', 'Full-Time', 40.00, 3);

-- Clients
INSERT INTO DimClient (ClientID, Name, Address, ContactInfo)
VALUES
(1, 'Happy Homes', '10 River Rd', 'contact@happyhomes.com'),
(2, 'Sunrise Care', '99 Valley View', 'info@sunrisecare.org'),
(3, 'Goodlife Living', '35 Forest Lane', 'support@goodlife.com');

-- Services
INSERT INTO DimService (ServiceID, ServiceName, Rate, RequiresCertification)
VALUES
(1, 'Home Nursing', 60.00, 1),
(2, 'Physiotherapy', 80.00, 1),
(3, 'Cleaning', 30.00, 0),
(4, 'Meal Preparation', 40.00, 0);

-- Assets
INSERT INTO DimAsset (AssetID, AssetType, Location, MonthlyRent)
VALUES
(1, 'Apartment', 'Downtown', 1200.00),
(2, 'House', 'Suburb', 2000.00),
(3, 'Condo', 'City Center', 1800.00);

-- Renters
INSERT INTO DimRenter (RenterID, Name, EmergencyContact, FamilyDoctor)
VALUES
(1, 'Mary White', 'Tom White', 'Dr. L. Howard'),
(2, 'John Lee', 'Sarah Lee', 'Dr. Patel'),
(3, 'Nancy Brown', 'Rick Brown', 'Dr. Wong');

-- =========================
-- FACT TABLES
-- =========================

-- Payroll
INSERT INTO FactPayroll (PayrollID, DimEmployeeEmployeeID, PayDate, BaseSalary, OverTimePay, Deductions, NetPay)
VALUES
(1, 1, '2025-10-01', 4000.00, 200.00, 300.00, 3900.00),
(2, 2, '2025-10-01', 2500.00, 150.00, 200.00, 2450.00),
(3, 3, '2025-10-01', 5000.00, 0.00, 400.00, 4600.00),
(4, 4, '2025-10-01', 2000.00, 100.00, 150.00, 1950.00),
(5, 5, '2025-10-01', 4800.00, 300.00, 500.00, 4600.00);

-- Shifts
INSERT INTO FactShifts (ShiftID, DimEmployeeEmployeeID, StartTime, EndTime, IsOnCall)
VALUES
(1, 1, '2025-10-01', '2025-10-01', 0),
(2, 2, '2025-10-02', '2025-10-02', 1),
(3, 3, '2025-10-03', '2025-10-03', 0),
(4, 4, '2025-10-03', '2025-10-03', 0),
(5, 5, '2025-10-04', '2025-10-04', 1);

-- Attendance
INSERT INTO FactAttendance (AttendanceID, DimEmployeeEmployeeID, DimFactShiftsShiftID, IsHoliday, IsVacation, IsOnCall)
VALUES
(1, 1, 1, 0, 0, 0),
(2, 2, 2, 0, 1, 1),
(3, 3, 3, 0, 0, 0),
(4, 4, 4, 1, 0, 0),
(5, 5, 5, 0, 0, 1);

-- Service Assignments
INSERT INTO FactServiceAssignment (AssignedID, DimEmployeeEmployeeID, DimServiceServiceID, ScheduledDate)
VALUES
(1, 1, 1, '2025-10-05'),
(2, 2, 2, '2025-10-05'),
(3, 3, 3, '2025-10-06'),
(4, 4, 4, '2025-10-06'),
(5, 5, 1, '2025-10-07');

-- Invoices
INSERT INTO FactInvoice (InvoiceID, DimClientClientID, DimServiceServiceID, InvoiceDate, TotalAmount, IsPaid)
VALUES
(1, 1, 1, '2025-09-30', 1200.00, 1),
(2, 1, 2, '2025-09-30', 800.00, 0),
(3, 2, 3, '2025-09-29', 400.00, 1),
(4, 3, 4, '2025-09-28', 700.00, 1),
(5, 3, 2, '2025-09-27', 1500.00, 0);

-- Service Registrations
INSERT INTO FactServiceRegistration (RegistrationID, DimClientClientID, DimServiceServiceID, RegistrationDate)
VALUES
(1, 1, 1, '2025-01-10'),
(2, 2, 2, '2025-02-14'),
(3, 3, 3, '2025-03-20'),
(4, 1, 4, '2025-05-01'),
(5, 2, 1, '2025-06-18');

-- Damage Reports
INSERT INTO FactDamageReport (ReportID, DimAssetAssetID, ReportDate, RepairCost, Description)
VALUES
(1, 1, '2025-08-01', 250.00, 'Broken window'),
(2, 2, '2025-08-15', 450.00, 'Water leak repair'),
(3, 3, '2025-09-05', 300.00, 'Appliance replacement');

-- Rental History
INSERT INTO FactRentalHistory (HistoryID, DimAssetAssetID, DimRenterRenterID, StartDate, EndDate, RentAmount)
VALUES
(1, 1, 1, '2025-01-01', '2025-03-31', 1200.00),
(2, 2, 2, '2025-02-01', '2025-04-30', 2000.00),
(3, 3, 3, '2025-03-01', '2025-06-30', 1800.00),
(4, 1, 2, '2025-07-01', '2025-09-30', 1300.00),
(5, 2, 3, '2025-08-01', '2025-10-31', 2100.00);
