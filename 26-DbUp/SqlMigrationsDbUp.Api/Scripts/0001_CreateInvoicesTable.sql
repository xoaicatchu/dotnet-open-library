CREATE TABLE IF NOT EXISTS Invoices (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    InvoiceNumber TEXT NOT NULL UNIQUE,
    CustomerName TEXT NOT NULL,
    TotalAmount DECIMAL(18,2) NOT NULL,
    Status TEXT NOT NULL DEFAULT 'Pending',
    CreatedAt TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS IX_Invoices_InvoiceNumber ON Invoices(InvoiceNumber);
