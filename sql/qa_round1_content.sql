-- Content changes from the first round of site QA that live in the database rather than in code.
-- Everything else from that round is a code change; these are values an editor had already been
-- given through the Cms, so they have to be updated in place as well as in seed_page_content.sql
-- (which only re-applies on a full reseed).
--
-- Idempotent: safe to run repeatedly.
SET NOCOUNT ON;
BEGIN TRANSACTION;

DECLARE @en INT = (SELECT Id FROM customer.languages WHERE Code = 'en');
DECLARE @ar INT = (SELECT Id FROM customer.languages WHERE Code = 'ar');
DECLARE @Contact INT = (SELECT Id FROM customer.pages WHERE PageName = N'Contact');

-- 1) Contact form success message ---------------------------------------------------------------
-- "Message sent successfully." reads like a system log line. Only replaced where it is still that
-- exact string, so a message the client has since reworded is left alone.
UPDATE customer.page_attributes
SET Value = N'Thank you for contacting us. We''ll be in touch with you soon.'
WHERE PageId = @Contact AND AttributeName = N'success_message' AND LangId = @en
  AND Value = N'Message sent successfully.';

UPDATE customer.page_attributes
SET Value = N'شكراً لتواصلك معنا. سنعاود الاتصال بك قريباً.'
WHERE PageId = @Contact AND AttributeName = N'success_message' AND LangId = @ar
  AND Value = N'تم إرسال رسالتك بنجاح.';

COMMIT TRANSACTION;
