-- Ensures the "Menu" page exists in customer.pages. It was never part of seed_page_content.sql
-- (the PDF it shows lives on the Global page's menu_pdf_url attribute instead — see
-- MiskBeirut.Web/Areas/Customer/Controllers/MenuController.cs), so without this row Menu never
-- appeared in the Cms Pages list/sidebar and had no edit screen to open. Idempotent: only inserts
-- if missing, never overwrites meta an admin may have already set by hand.
SET NOCOUNT ON;

IF NOT EXISTS (SELECT 1 FROM customer.pages WHERE PageName = N'Menu')
BEGIN
    INSERT INTO customer.pages (PageName, MetaTitle, MetaDesc, MetaKeyword)
    VALUES (N'Menu',
            N'Menu | Lebanese Mezze, Grills & Wood-Oven Dishes | Misk Beirut',
            N'The full Misk Beirut menu: cold and hot mezze, charcoal grills, wood-oven manousheh, Levantine sweets and Arabic coffee.',
            N'misk beirut menu, lebanese mezze, charcoal grill beirut, manousheh');
END

-- The per-language copies of the same metadata. The columns above are only the fallback: without
-- these rows /ar/menu would advertise the English title and description to search engines. Inserted
-- only where missing, so an editor's own wording is never overwritten.
DECLARE @Menu INT = (SELECT Id FROM customer.pages WHERE PageName = N'Menu');
DECLARE @en INT = (SELECT Id FROM customer.languages WHERE Code = 'en');
DECLARE @ar INT = (SELECT Id FROM customer.languages WHERE Code = 'ar');

INSERT INTO customer.page_attributes (PageId, AttributeName, AttributeType, LangId, Value)
SELECT @Menu, src.AttributeName, 'Text', src.LangId, src.Value
FROM (VALUES
    (N'meta_title', @en, N'Menu | Lebanese Mezze, Grills & Wood-Oven Dishes | Misk Beirut'),
    (N'meta_title', @ar, N'قائمة الطعام | مازة ومشاوٍ وأطباق الفرن الحجري | مسك بيروت'),
    (N'meta_description', @en, N'The full Misk Beirut menu: cold and hot mezze, charcoal grills, wood-oven manousheh, Levantine sweets and Arabic coffee.'),
    (N'meta_description', @ar, N'قائمة مسك بيروت كاملة: مازة باردة وساخنة، مشاوٍ على الفحم، مناقيش من الفرن الحجري، حلويات شامية وقهوة عربية.'),
    (N'meta_keywords', @en, N'misk beirut menu, lebanese mezze, charcoal grill beirut, manousheh'),
    (N'meta_keywords', @ar, N'قائمة مسك بيروت, مازة لبنانية, مشاوي بيروت, مناقيش')
) AS src(AttributeName, LangId, Value)
WHERE @Menu IS NOT NULL AND src.LangId IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 FROM customer.page_attributes pa
      WHERE pa.PageId = @Menu AND pa.AttributeName = src.AttributeName AND pa.LangId = src.LangId);