-- Fixes customer.page_attributes 'menu_pdf_url' (Global, both languages): it was set to a local
-- Windows filesystem path instead of a site-relative URL, so it never actually resolved in a
-- browser. Idempotent: safe to re-run.
UPDATE customer.page_attributes
SET Value = N'/img/Menu/misk_beirut_menu_complete.pdf'
WHERE AttributeName = N'menu_pdf_url';
