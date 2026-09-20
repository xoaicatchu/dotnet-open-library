INSERT INTO Invoices (InvoiceNumber, CustomerName, TotalAmount, Status, CreatedAt)
VALUES ('INV-2026-001', 'Tech Corp', 1500.00, 'Paid', datetime('now'));

INSERT INTO InvoiceItems (InvoiceId, Description, Quantity, UnitPrice)
VALUES (1, 'Cloud Hosting Services', 1, 1500.00);
