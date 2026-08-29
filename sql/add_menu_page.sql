-- Ensures the "Menu" page exists in customer.pages. It was never part of seed_page_content.sql
-- (the PDF it shows lives on the Global page's menu_pdf_url attribute instead — see
-- MiskBeirut.Web/Areas/Customer/Controllers/MenuController.cs), so without this row Menu never
-- appeared in the Cms Pages list/sidebar and had no edit screen to open. Idempotent: only inserts
-- if missing, never overwrites meta an admin may have already set by hand.
SET NOCOUNT ON;

IF NOT EXISTS (SELECT 1 FROM customer.pages WHERE PageName = N'Menu')
BEGIN
    INSERT INTO customer.pages (PageName, MetaTitle, MetaDesc, MetaKeyword)
    VALUES (N'Menu', N'Menu | Misk Beirut', N'View our full menu.', N'menu, misk beirut, food');
END