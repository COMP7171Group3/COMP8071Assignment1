USE CareServicesOLTP;
GO

INSERT INTO Employee (
    EmployeeID,
    Name,
    Address,
    JobTitle,
    EmployeeType,
    SalaryRate,
    ReportsTo
)
VALUES
    (1, 'Alice Johnson', '123 Maple St', 'Registered Nurse', 'FullTime',
    35.5, 4),
    (2, 'Bob Smith', '456 Oak Ave', 'Registered Nurse', 'FullTime',
    34.75, 4),
    (3, 'Carol Lee', '789 Pine Rd', 'Physiotherapist', 'PartTime',
    28.0, 4),
    (4, 'David Brown', '10 Center Plaza', 'Clinic Manager', 'Management',
    55.0, NULL),
    (5, 'Eve Davis', '22 Lake View', 'Receptionist', 'PartTime',
    20.0, 4),
    (6, 'Frank Miller', '31 Grove St', 'Home Health Aide', 'FullTime',
    22.5, 4),
    (7, 'Grace Kim', '17 Elm St', 'Nurse Practitioner', 'FullTime',
    45.0, 4);

INSERT INTO Payment (
    PaymentID,
    EmployeeID,
    PayDate,
    OverTimePay,
    Deductions,
    NetPay
)
VALUES
    (1001, 1, '2025-10-31', 300.00, 450.00, 5200.00),
    (1002, 2, '2025-10-31', 250.00, 380.00, 4850.00),
    (1003, 4, '2025-10-31', 120.00, 150.00, 2100.00),
    (1004, 5, '2025-10-31',   0.00, 700.00, 7300.00),
    (1005, 5, '2025-10-31',  60.00,  80.00, 1600.00),
    (1006, 4, '2025-10-31',  90.00, 120.00, 3100.00),
    (1007, 2, '2025-11-15',  50.00, 100.00, 2500.00),
    (1008, 1, '2025-11-15', 200.00, 300.00, 4700.00);

INSERT INTO Client (
    ClientID,
    Name,
    Address,
    ContactInfo
)
VALUES
    (5001, 'Green Valley Home', '12 Green Way', '555-1001; gv@example.com'),
    (5002, 'Oakridge Apartments', '88 Oak Blvd', '555-1002; oak@example.com'),
    (5003, 'Sunrise Care', '5 Dawn St', '555-1003; sunrise@example.com'),
    (5004, 'Lakeside Family', '77 Lake Rd', '555-1004; lakeside@example.com'),
    (5005, 'Mountain View Clinic', '3 Summit Ave',
    '555-1005; mv@example.com'),
    (5006, 'Brookfield Community', '21 River Dr',
    '555-1006; brook@example.com');

INSERT INTO ServiceType (
    ServiceTypeID,
    ServiceName,
    Rate,
    RequiresCertification
)
VALUES
    (1, 'Physiotherapy Session', 85.00, 1),
    (2, 'Home Nursing Visit', 120.00, 1),
    (3, 'Initial Consultation', 60.00, 0),
    (4, 'Emergency On-Call', 150.00, 1),
    (5, 'Wellness Check', 75.00, 0),
    (6, 'Post-op Care', 140.00, 1);

INSERT INTO Asset (
    AssetID,
    AssetType,
    Location,
    MonthlyRent
)
VALUES
    (6001, 'Vehicle', 'Garage A', 800.00),
    (6002, 'Office Suite', 'Downtown Plaza Suite 3', 2500.00),
    (6003, 'Ultrasound Machine', 'Storage Room 2', 350.00),
    (6004, 'Laptop', 'IT Closet', 45.00),
    (6005, 'Rehab Bench', 'Therapy Room 1', 60.00),
    (6006, 'Storage Unit', 'Offsite Storage', 120.00);

INSERT INTO Renter (
    RenterID,
    Name,
    EmergencyContact,
    FamilyDoctor
)
VALUES
    (7001, 'Sunrise Clinic', '555-5892', 'Dr. Chen'),
    (7002, 'John Tenant', '555-0068', 'Dr. Patel'),
    (7003, 'Oakridge HOA', '555-1122', 'Dr. James'),
    (7004, 'Green Valley Co-Op', '555-7788', 'Dr. Nguyen'),
    (7005, 'Brookfield PTA', '555-9090', 'Dr. Gomez'),
    (7006, 'City Health Dept', '555-1234', 'Dr. Stone');


INSERT INTO Shift (
    ShiftID,
    EmployeeID,
    StartTime,
    EndTime,
    IsOnCall
)
VALUES
    (2001, 1, '2025-10-01', '2025-10-01', 0),
    (2002, 1, '2025-10-10', '2025-10-10', 1),
    (2003, 2, '2025-10-02', '2025-10-02', 0),
    (2004, 2, '2025-10-15', '2025-10-15', 0),
    (2005, 3, '2025-10-05', '2025-10-05', 0),
    (2006, 3, '2025-10-20', '2025-10-20', 1),
    (2007, 4, '2025-10-01', '2025-10-01', 0),
    (2008, 4, '2025-10-31', '2025-10-31', 0),
    (2009, 5, '2025-10-12', '2025-10-12', 0),
    (2010, 5, '2025-11-01', '2025-11-01', 1),
    (2011, 6, '2025-10-18', '2025-10-18', 0),
    (2012, 7, '2025-11-10', '2025-11-10', 0);

INSERT INTO Service (
    ServiceID,
    ServiceTypeID,
    EmployeeID,
    ClientID,
    RegistrationDate,
    ScheduledDate,
    TotalAmount,
    IsPaid
)
VALUES
    (9001, 1, 3, 5001, '2025-10-05', '2025-10-08', 170.00, 1),
    (9002, 2, 1, 5002, '2025-10-10', '2025-10-12', 120.00, 0),
    (9003, 3, 5, 5003, '2025-10-07', '2025-10-07', 60.00, 1),
    (9004, 4, 2, 5004, '2025-10-28', '2025-10-28', 150.00, 1),
    (9005, 5, 6, 5005, '2025-10-20', '2025-10-22', 75.00, 0),
    (9006, 2, 1, 5001, '2025-11-01', '2025-11-03', 120.00, 1),
    (9007, 6, 7, 5006, '2025-11-05', '2025-11-06', 280.00, 0),
    (9008, 1, 3, 5002, '2025-11-02', '2025-11-02', 85.00, 1),
    (9009, 5, 6, 5004, '2025-11-07', '2025-11-07', 75.00, 1),
    (9010, 4, 2, 5003, '2025-11-10', '2025-11-10', 150.00, 0);

INSERT INTO AssetRent (
    AssetRentID,
    AssetID,
    RenterID,
    StartDate,
    EndDate
)
VALUES
    (7501, 6002, 7002, '2025-09-01', '2025-12-31'),
    (7502, 6001, 7001, '2025-10-01', '2025-10-31'),
    (7503, 6003, 7006, '2025-10-15', '2026-10-14'),
    (7504, 6004, 7003, '2025-11-01', '2025-11-30'),
    (7505, 6005, 7004, '2025-09-15', '2026-03-14'),
    (7506, 6006, 7005, '2025-08-01', '2025-12-31'),
    (7507, 6001, 7006, '2025-11-01', '2025-11-30');

GO