-- Content/data changes from the third round of QA (the SEO pass). Everything else in that round is
-- a code change; these are the values that live in the database.
--
-- Three things happen here:
--   1) Every page gets a unique title and description in BOTH languages. They existed only as the
--      single-value columns on customer.pages, so the Arabic pages advertised English copy to
--      search engines -- /ar/about was titled "About Us | Misk Beirut".
--   2) The business's phone, email and address move to the Global page. They were duplicated across
--      the footer, the Contact card, the tel: link and the WhatsApp button, and the four had drifted
--      to four different numbers.
--   3) Placeholder copy that was reaching the public site is replaced with real copy.
--
-- Idempotent: safe to run repeatedly.
SET NOCOUNT ON;
BEGIN TRANSACTION;

DECLARE @en INT = (SELECT Id FROM customer.languages WHERE Code = 'en');
DECLARE @ar INT = (SELECT Id FROM customer.languages WHERE Code = 'ar');

IF @en IS NULL OR @ar IS NULL
BEGIN
    ROLLBACK TRANSACTION;
    THROW 50000, 'Both the en and ar languages must exist in customer.languages before this runs.', 1;
END

DECLARE @Global INT = (SELECT Id FROM customer.pages WHERE PageName = N'Global');
DECLARE @Home   INT = (SELECT Id FROM customer.pages WHERE PageName = N'Home');
DECLARE @About  INT = (SELECT Id FROM customer.pages WHERE PageName = N'About');
DECLARE @Spaces INT = (SELECT Id FROM customer.pages WHERE PageName = N'Spaces');
DECLARE @Menu   INT = (SELECT Id FROM customer.pages WHERE PageName = N'Menu');
DECLARE @Events INT = (SELECT Id FROM customer.pages WHERE PageName = N'Events');
DECLARE @Careers INT = (SELECT Id FROM customer.pages WHERE PageName = N'Careers');
DECLARE @Contact INT = (SELECT Id FROM customer.pages WHERE PageName = N'Contact');

IF @Menu IS NULL
BEGIN
    ROLLBACK TRANSACTION;
    THROW 50000, 'The Menu page is missing -- run sql/add_menu_page.sql first.', 1;
END

-- ------------------------------------------------------------------------------------------------
-- 1) Per-language SEO metadata
-- ------------------------------------------------------------------------------------------------
-- Written as attribute rows (meta_title / meta_description / meta_keywords), which is what the
-- public site reads per language; the columns below are kept in step as the language-neutral
-- fallback and as what the Cms page list shows. See MiskBeirut.Web/Support/SeoAttributes.cs.
--
-- Every title and description here is distinct from every other one. Two pages sharing a title is
-- the single most common reason a page gets filtered out of results as a near-duplicate.

DECLARE @seo TABLE (PageId INT, AttributeName NVARCHAR(200), LangId INT, Value NVARCHAR(MAX));

INSERT INTO @seo (PageId, AttributeName, LangId, Value) VALUES
-- Home
(@Home, N'meta_title', @en, N'Misk Beirut | Lebanese Restaurant, Cafe & Event Spaces in Beirut'),
(@Home, N'meta_title', @ar, N'مسك بيروت | مطعم ومقهى ومساحات فعاليات في بيروت'),
(@Home, N'meta_description', @en, N'Lebanese cooking, shisha and event spaces in the heart of Beirut. Book a table, browse the menu, or plan a private celebration at Misk Beirut.'),
(@Home, N'meta_description', @ar, N'مطبخ لبناني وأركيلة ومساحات للفعاليات في قلب بيروت. احجز طاولتك، تصفّح قائمة الطعام، أو خطّط لمناسبتك الخاصة في مسك بيروت.'),
(@Home, N'meta_keywords', @en, N'misk beirut, lebanese restaurant beirut, beirut dining, shisha lounge beirut'),
(@Home, N'meta_keywords', @ar, N'مسك بيروت, مطعم لبناني بيروت, مطاعم بيروت, أركيلة بيروت'),

-- About
(@About, N'meta_title', @en, N'About Misk Beirut | Our Story and Lebanese Hospitality'),
(@About, N'meta_title', @ar, N'عن مسك بيروت | قصتنا والضيافة اللبنانية'),
(@About, N'meta_description', @en, N'How Misk Beirut began, the family behind it, and the Levantine hospitality that shapes every table we set.'),
(@About, N'meta_description', @ar, N'كيف بدأت مسك بيروت، والعائلة التي تقف خلفها، والضيافة الشامية التي تشكّل كل طاولة نعدّها.'),
(@About, N'meta_keywords', @en, N'about misk beirut, our story, lebanese hospitality'),
(@About, N'meta_keywords', @ar, N'عن مسك بيروت, قصتنا, الضيافة اللبنانية'),

-- Spaces
(@Spaces, N'meta_title', @en, N'Our Spaces | Dining, Terraces & Meeting Areas in Beirut'),
(@Spaces, N'meta_title', @ar, N'مساحاتنا | قاعات الطعام والتراسات وأماكن اللقاء في بيروت'),
(@Spaces, N'meta_description', @en, N'Dining rooms, indoor and outdoor terraces, and quiet corners for work -- the spaces at Misk Beirut and what each one suits.'),
(@Spaces, N'meta_description', @ar, N'قاعات طعام، وتراسات داخلية وخارجية، وزوايا هادئة للعمل — مساحات مسك بيروت وما يناسب كلاً منها.'),
(@Spaces, N'meta_keywords', @en, N'beirut private dining, outdoor terrace beirut, meeting space beirut'),
(@Spaces, N'meta_keywords', @ar, N'قاعات خاصة بيروت, تراس خارجي بيروت, مساحات اجتماعات بيروت'),

-- Menu
(@Menu, N'meta_title', @en, N'Menu | Lebanese Mezze, Grills & Wood-Oven Dishes | Misk Beirut'),
(@Menu, N'meta_title', @ar, N'قائمة الطعام | مازة ومشاوٍ وأطباق الفرن الحجري | مسك بيروت'),
(@Menu, N'meta_description', @en, N'The full Misk Beirut menu: cold and hot mezze, charcoal grills, wood-oven manousheh, Levantine sweets and Arabic coffee.'),
(@Menu, N'meta_description', @ar, N'قائمة مسك بيروت كاملة: مازة باردة وساخنة، مشاوٍ على الفحم، مناقيش من الفرن الحجري، حلويات شامية وقهوة عربية.'),
(@Menu, N'meta_keywords', @en, N'misk beirut menu, lebanese mezze, charcoal grill beirut, manousheh'),
(@Menu, N'meta_keywords', @ar, N'قائمة مسك بيروت, مازة لبنانية, مشاوي بيروت, مناقيش'),

-- Events
(@Events, N'meta_title', @en, N'Events & Private Celebrations | Misk Beirut'),
(@Events, N'meta_title', @ar, N'الفعاليات والمناسبات الخاصة | مسك بيروت'),
(@Events, N'meta_description', @en, N'Live match screenings, birthdays, engagements and company dinners at Misk Beirut -- spaces and planning for gatherings of any size.'),
(@Events, N'meta_description', @ar, N'عرض المباريات، أعياد الميلاد، الخطوبات وعشاءات العمل في مسك بيروت — مساحات وتخطيط للقاءات بمختلف أحجامها.'),
(@Events, N'meta_keywords', @en, N'private events beirut, birthday venue beirut, match screening beirut'),
(@Events, N'meta_keywords', @ar, N'مناسبات خاصة بيروت, قاعة أعياد ميلاد بيروت, عرض مباريات بيروت'),

-- Careers
(@Careers, N'meta_title', @en, N'Careers | Join the Team at Misk Beirut'),
(@Careers, N'meta_title', @ar, N'الوظائف | انضم إلى فريق مسك بيروت'),
(@Careers, N'meta_description', @en, N'Open positions at Misk Beirut across kitchen, service and management. See what we are hiring for and apply with your CV.'),
(@Careers, N'meta_description', @ar, N'الوظائف الشاغرة في مسك بيروت في المطبخ والخدمة والإدارة. اطّلع على ما نبحث عنه وقدّم سيرتك الذاتية.'),
(@Careers, N'meta_keywords', @en, N'restaurant jobs beirut, careers misk beirut, hospitality jobs lebanon'),
(@Careers, N'meta_keywords', @ar, N'وظائف مطاعم بيروت, وظائف مسك بيروت, وظائف ضيافة لبنان'),

-- Contact
(@Contact, N'meta_title', @en, N'Contact & Reservations | Misk Beirut'),
(@Contact, N'meta_title', @ar, N'التواصل والحجوزات | مسك بيروت'),
(@Contact, N'meta_description', @en, N'Reserve a table, ask about an event, or find us in Beirut. Phone, WhatsApp, email and directions to Misk Beirut.'),
(@Contact, N'meta_description', @ar, N'احجز طاولة، استفسر عن مناسبة، أو تعرّف على موقعنا في بيروت. الهاتف وواتساب والبريد الإلكتروني وطريق الوصول إلى مسك بيروت.'),
(@Contact, N'meta_keywords', @en, N'contact misk beirut, restaurant reservation beirut, book a table beirut'),
(@Contact, N'meta_keywords', @ar, N'تواصل مسك بيروت, حجز مطعم بيروت, احجز طاولة بيروت');

MERGE customer.page_attributes AS target
USING @seo AS src
ON target.PageId = src.PageId AND target.AttributeName = src.AttributeName AND target.LangId = src.LangId
WHEN MATCHED THEN UPDATE SET Value = src.Value, AttributeType = N'Text'
WHEN NOT MATCHED THEN INSERT (PageId, AttributeName, AttributeType, LangId, Value)
    VALUES (src.PageId, src.AttributeName, N'Text', src.LangId, src.Value);

-- Keep the columns in step with the English rows: they are the fallback the public site drops to
-- when an attribute row is missing, and they are what the Cms page list and dashboard display.
UPDATE p
SET p.MetaTitle   = (SELECT Value FROM @seo s WHERE s.PageId = p.Id AND s.AttributeName = N'meta_title' AND s.LangId = @en),
    p.MetaDesc    = (SELECT Value FROM @seo s WHERE s.PageId = p.Id AND s.AttributeName = N'meta_description' AND s.LangId = @en),
    p.MetaKeyword = (SELECT Value FROM @seo s WHERE s.PageId = p.Id AND s.AttributeName = N'meta_keywords' AND s.LangId = @en)
FROM customer.pages p
WHERE p.Id IN (SELECT DISTINCT PageId FROM @seo);

-- ------------------------------------------------------------------------------------------------
-- 2) One set of business details, on the Global page
-- ------------------------------------------------------------------------------------------------
-- Read by MiskBeirut.Web/Support/BusinessProfile.cs, which now backs the footer, the Contact card,
-- the tel: link, the WhatsApp button and the Restaurant structured data.

DECLARE @global_attrs TABLE (AttributeName NVARCHAR(200), AttributeType NVARCHAR(50), LangId INT, Value NVARCHAR(MAX));

INSERT INTO @global_attrs (AttributeName, AttributeType, LangId, Value) VALUES
(N'contact_phone', 'Text', @en, N'+961 76 551 204'),
(N'contact_phone', 'Text', @ar, N'+961 76 551 204'),
(N'contact_whatsapp_url', 'Link', @en, N'https://wa.me/96176551204'),
(N'contact_whatsapp_url', 'Link', @ar, N'https://wa.me/96176551204'),
(N'contact_email', 'Text', @en, N'hello@miskbeirut.com'),
(N'contact_email', 'Text', @ar, N'hello@miskbeirut.com'),
(N'address_line', 'Text', @en, N'Gemmayzeh, Pasteur Street, Beirut'),
(N'address_line', 'Text', @ar, N'الجميزة، شارع باستور، بيروت'),

-- The structured-data breakdown of the same address. Kept as separate fields because
-- schema.org/PostalAddress wants them separately, not because they are shown anywhere.
(N'address_street', 'Text', @en, N'Pasteur Street, Gemmayzeh'),
(N'address_street', 'Text', @ar, N'شارع باستور، الجميزة'),
(N'address_locality', 'Text', @en, N'Beirut'),
(N'address_locality', 'Text', @ar, N'بيروت'),
(N'address_country', 'Text', @en, N'LB'),
(N'address_country', 'Text', @ar, N'LB'),
(N'serves_cuisine', 'Text', @en, N'Lebanese'),
(N'serves_cuisine', 'Text', @ar, N'لبناني'),

-- Opening hours in schema.org's own notation: days Mo-Su, 24-hour times, and 00:00 as the closing
-- time meaning midnight. Confirmed against what the Home page's "Visit Us" block already displays
-- (12:00 PM - 12:00 AM), so the two cannot disagree. Google shows these directly in results, which
-- is why they were left blank until confirmed rather than guessed at.
(N'opening_hours', 'Text', @en, N'Mo-Su 12:00-00:00'),
(N'opening_hours', 'Text', @ar, N'Mo-Su 12:00-00:00'),

-- Still blank, and the structured data omits each field entirely while it is. Price range and the
-- map coordinates are also shown to people directly in Google's results, so they need real values
-- rather than plausible ones.
(N'price_range', 'Text', @en, N''),
(N'price_range', 'Text', @ar, N''),
(N'geo_latitude', 'Text', @en, N''),
(N'geo_longitude', 'Text', @en, N'');

MERGE customer.page_attributes AS target
USING (SELECT @Global AS PageId, AttributeName, AttributeType, LangId, Value FROM @global_attrs) AS src
ON target.PageId = src.PageId AND target.AttributeName = src.AttributeName AND target.LangId = src.LangId
WHEN MATCHED THEN UPDATE SET Value = src.Value, AttributeType = src.AttributeType
WHEN NOT MATCHED THEN INSERT (PageId, AttributeName, AttributeType, LangId, Value)
    VALUES (src.PageId, src.AttributeName, src.AttributeType, src.LangId, src.Value);

-- The rows the views used to read, now superseded by the Global ones above. Dropped rather than
-- left in place: an editor changing a value that nothing renders any more is worse than not finding
-- the field, and the Cms attribute editor lists every row a page has.
DELETE FROM customer.page_attributes
WHERE (PageId = @Global  AND AttributeName IN (N'footer_phone', N'footer_email'))
   OR (PageId = @Contact AND AttributeName IN (N'info_call_phone_1', N'info_call_phone_2', N'info_call_tel_link', N'info_email_value', N'info_address_value', N'fab_whatsapp_url'));

-- The Home page's "Visit Us" block quotes the same number and address, so it follows the same
-- values rather than keeping its own copy of them.
UPDATE customer.page_attributes SET Value = N'+961 76 551 204' WHERE PageId = @Home AND AttributeName = N'visit_phone';
UPDATE customer.page_attributes SET Value = N'hello@miskbeirut.com' WHERE PageId = @Home AND AttributeName = N'visit_email';

-- ------------------------------------------------------------------------------------------------
-- 3) Placeholder copy that was reaching the public site
-- ------------------------------------------------------------------------------------------------
-- "Placeholder description ... Final copy to be provided by the client" was seeded as the real
-- value, so it rendered on the live Events and Spaces pages and was there to be crawled. Replaced
-- with copy that is true of the restaurant and safe to publish while the client's own wording is
-- still being written -- and with Arabic, which these fields never had.
--
-- Only replaced where the value is still the placeholder, so anything the client has since reworded
-- is left alone.

DECLARE @copy TABLE (PageId INT, AttributeName NVARCHAR(200), LangId INT, Value NVARCHAR(MAX));

INSERT INTO @copy (PageId, AttributeName, LangId, Value) VALUES
(@Events, N'football_body', @en, N'Big matches on the big screen, with the kitchen open and a table held for your group.'),
(@Events, N'football_body', @ar, N'المباريات الكبرى على الشاشة الكبيرة، مع مطبخ مفتوح وطاولة محجوزة لمجموعتك.'),
(@Events, N'private_body', @en, N'Birthdays, engagements and company dinners, planned with you and hosted in the space that fits them.'),
(@Events, N'private_body', @ar, N'أعياد ميلاد وخطوبات وعشاءات عمل، نخطّط لها معك ونستضيفها في المساحة التي تناسبها.'),
(@Spaces, N'panel_1_body', @en, N'Dining rooms serving Lebanese cooking and the hospitality that comes with it, alongside a curated shisha selection.'),
(@Spaces, N'panel_1_body', @ar, N'قاعات طعام تقدّم المطبخ اللبناني بضيافته المعهودة، إلى جانب تشكيلة مختارة من الأراكيل.'),
(@Spaces, N'panel_2_body', @en, N'Rooms and terraces that suit a table for two or a gathering of thirty, indoors and out.'),
(@Spaces, N'panel_2_body', @ar, N'قاعات وتراسات تتّسع لطاولة لشخصين أو للقاء يضمّ ثلاثين، في الداخل وفي الهواء الطلق.'),
(@Spaces, N'panel_3_body', @en, N'Quiet corners for studying, working and unhurried meetings, with power and Wi-Fi within reach.'),
(@Spaces, N'panel_3_body', @ar, N'زوايا هادئة للدراسة والعمل واللقاءات على مهل، مع الكهرباء والإنترنت في المتناول.'),

-- "Menu Item 1/2/3" was seeded literally and rendered as the labels under the Home page's menu
-- teaser. They now name the three categories the Menu page already leads with.
(@Home, N'menu_item_1_label', @en, N'From the Wood Oven'),
(@Home, N'menu_item_1_label', @ar, N'من الفرن الحجري'),
(@Home, N'menu_item_2_label', @en, N'Mezze & Grills'),
(@Home, N'menu_item_2_label', @ar, N'المازة والمشاوي'),
(@Home, N'menu_item_3_label', @en, N'Sweets & Coffee'),
(@Home, N'menu_item_3_label', @ar, N'الحلويات والقهوة');

MERGE customer.page_attributes AS target
USING @copy AS src
ON target.PageId = src.PageId AND target.AttributeName = src.AttributeName AND target.LangId = src.LangId
-- The guard: a row is only overwritten while it still holds the placeholder, so anything the client
-- has since reworded is left alone. Both languages are listed -- the Arabic rows were not missing,
-- they held a translation OF the placeholder ("نص مؤقت ... سيتم توفير النص النهائي من قبل العميل"),
-- which means the note to the client was live on the Arabic pages too.
WHEN MATCHED AND (target.Value LIKE N'Placeholder description%'
               OR target.Value LIKE N'نص مؤقت%'
               OR target.Value LIKE N'Menu Item[ ]%'
               OR target.Value LIKE N'صنف[ ]%')
    THEN UPDATE SET Value = src.Value
WHEN NOT MATCHED THEN INSERT (PageId, AttributeName, AttributeType, LangId, Value)
    VALUES (src.PageId, src.AttributeName, N'Text', src.LangId, src.Value);

COMMIT TRANSACTION;

-- What a crawler will now see: one title and one description per page per language, all distinct.
SELECT p.PageName,
       MAX(CASE WHEN pa.LangId = @en THEN pa.Value END) AS TitleEn,
       MAX(CASE WHEN pa.LangId = @ar THEN pa.Value END) AS TitleAr
FROM customer.pages p
JOIN customer.page_attributes pa ON pa.PageId = p.Id AND pa.AttributeName = N'meta_title'
GROUP BY p.PageName
ORDER BY p.PageName;
