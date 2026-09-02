-- Content/data changes from the second round of QA (the CMS pass). Everything else in that round
-- is a code change; these are values already sitting in the database that the new code expects to
-- find in a different shape.
--
-- Idempotent: safe to run repeatedly.
SET NOCOUNT ON;
BEGIN TRANSACTION;

-- 1) Menu PDF becomes a file field ---------------------------------------------------------------
-- The Global page's menu_pdf_url row was typed 'Link', which is why the Cms showed it as a text box
-- for an admin to type a path into. 'Pdf' is a real PageAttributeType now (see
-- MiskBeirut.Core/Enums/PageAttributeType.cs) and renders an upload button instead. The value is
-- unchanged — only how the Cms offers to edit it.
--
-- REQUIRES the AllowPdfPageAttributeType migration to have run first: CK_page_attributes_type only
-- permits the types it lists, so this UPDATE fails with a constraint violation against an older
-- schema. Checked here rather than left to a confusing error mid-script.
IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = 'CK_page_attributes_type' AND definition LIKE '%Pdf%')
BEGIN
    ROLLBACK TRANSACTION;
    THROW 50000, 'Run the EF migrations first — CK_page_attributes_type does not allow the Pdf attribute type yet.', 1;
END

UPDATE customer.page_attributes
SET AttributeType = N'Pdf'
WHERE AttributeName = N'menu_pdf_url' AND AttributeType <> N'Pdf';

-- 2) Existing SEO metadata becomes per-language --------------------------------------------------
-- customer.pages.MetaTitle/MetaDesc/MetaKeyword hold one value per page, so the Arabic version of
-- every page was advertising an English title and description to search engines. The Cms now edits
-- these as per-language attribute rows (MiskBeirut.Web/Support/SeoAttributes.cs), with the columns
-- kept as the language-neutral fallback. This seeds the English rows from what's already in the
-- columns so nothing has to be retyped; Arabic is left empty for a translator to fill in, and falls
-- back to these until they do.
DECLARE @en INT = (SELECT Id FROM customer.languages WHERE Code = 'en');

INSERT INTO customer.page_attributes (PageId, AttributeName, AttributeType, LangId, Value)
SELECT p.Id, N'meta_title', N'Text', @en, p.MetaTitle
FROM customer.pages p
WHERE p.MetaTitle IS NOT NULL AND LTRIM(RTRIM(p.MetaTitle)) <> N''
  AND NOT EXISTS (SELECT 1 FROM customer.page_attributes a
                  WHERE a.PageId = p.Id AND a.AttributeName = N'meta_title' AND a.LangId = @en);

INSERT INTO customer.page_attributes (PageId, AttributeName, AttributeType, LangId, Value)
SELECT p.Id, N'meta_description', N'Text', @en, p.MetaDesc
FROM customer.pages p
WHERE p.MetaDesc IS NOT NULL AND LTRIM(RTRIM(p.MetaDesc)) <> N''
  AND NOT EXISTS (SELECT 1 FROM customer.page_attributes a
                  WHERE a.PageId = p.Id AND a.AttributeName = N'meta_description' AND a.LangId = @en);

INSERT INTO customer.page_attributes (PageId, AttributeName, AttributeType, LangId, Value)
SELECT p.Id, N'meta_keywords', N'Text', @en, p.MetaKeyword
FROM customer.pages p
WHERE p.MetaKeyword IS NOT NULL AND LTRIM(RTRIM(p.MetaKeyword)) <> N''
  AND NOT EXISTS (SELECT 1 FROM customer.page_attributes a
                  WHERE a.PageId = p.Id AND a.AttributeName = N'meta_keywords' AND a.LangId = @en);

COMMIT TRANSACTION;
