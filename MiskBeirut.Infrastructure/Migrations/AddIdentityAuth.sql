IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728154338_AddIdentityAuth'
)
BEGIN
    IF SCHEMA_ID(N'backoffice') IS NULL EXEC(N'CREATE SCHEMA [backoffice];');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728154338_AddIdentityAuth'
)
BEGIN
    IF SCHEMA_ID(N'customer') IS NULL EXEC(N'CREATE SCHEMA [customer];');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728154338_AddIdentityAuth'
)
BEGIN
    CREATE TABLE [backoffice].[AuditLogs] (
        [Id] int NOT NULL IDENTITY,
        [EntityType] nvarchar(200) NULL,
        [Action] nvarchar(100) NULL,
        [EntityId] nvarchar(100) NULL,
        [UserId] int NULL,
        [Username] nvarchar(200) NULL,
        [OldValues] nvarchar(max) NULL,
        [NewValues] nvarchar(max) NULL,
        [Description] nvarchar(1000) NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [IpAddress] nvarchar(50) NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728154338_AddIdentityAuth'
)
BEGIN
    CREATE TABLE [backoffice].[customers] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(200) NOT NULL,
        [PhoneNumber] nvarchar(50) NOT NULL,
        [Balance] decimal(18,2) NOT NULL DEFAULT 0.0,
        [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_customers] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728154338_AddIdentityAuth'
)
BEGIN
    CREATE TABLE [customer].[customers] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(200) NOT NULL,
        [PhoneNumber] nvarchar(50) NOT NULL,
        [Email] nvarchar(256) NOT NULL,
        [DiscountRedeemed] bit NOT NULL DEFAULT CAST(0 AS bit),
        [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_customers] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728154338_AddIdentityAuth'
)
BEGIN
    CREATE TABLE [backoffice].[daily_closing] (
        [Id] int NOT NULL IDENTITY,
        [Date] date NOT NULL,
        [MainReading] decimal(18,2) NOT NULL,
        [AdjustedReading] decimal(18,2) NULL,
        [ActualCash] decimal(18,2) NULL,
        [Note] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_daily_closing] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728154338_AddIdentityAuth'
)
BEGIN
    CREATE TABLE [backoffice].[investors] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(200) NOT NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_investors] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728154338_AddIdentityAuth'
)
BEGIN
    CREATE TABLE [customer].[languages] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(10) NOT NULL,
        [Name] nvarchar(50) NOT NULL,
        CONSTRAINT [PK_languages] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728154338_AddIdentityAuth'
)
BEGIN
    CREATE TABLE [customer].[pages] (
        [Id] int NOT NULL IDENTITY,
        [PageName] nvarchar(200) NOT NULL,
        [MetaTitle] nvarchar(300) NULL,
        [MetaDesc] nvarchar(500) NULL,
        [MetaKeyword] nvarchar(500) NULL,
        CONSTRAINT [PK_pages] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728154338_AddIdentityAuth'
)
BEGIN
    CREATE TABLE [backoffice].[receivers] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(200) NOT NULL,
        CONSTRAINT [PK_receivers] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728154338_AddIdentityAuth'
)
BEGIN
    CREATE TABLE [backoffice].[users] (
        [Id] int NOT NULL IDENTITY,
        [Role] nvarchar(50) NOT NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [Username] nvarchar(100) NOT NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL DEFAULT CAST(0 AS bit),
        [PasswordHash] nvarchar(max) NOT NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(50) NULL,
        [PhoneNumberConfirmed] bit NOT NULL DEFAULT CAST(0 AS bit),
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL DEFAULT CAST(1 AS bit),
        [AccessFailedCount] int NOT NULL DEFAULT 0,
        CONSTRAINT [PK_users] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728154338_AddIdentityAuth'
)
BEGIN
    CREATE TABLE [customer].[customer_ledger] (
        [Id] int NOT NULL IDENTITY,
        [Date] date NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Type] nvarchar(20) NOT NULL,
        [Note] nvarchar(500) NULL,
        [CustomerId] int NOT NULL,
        [DailyClosingId] int NOT NULL,
        CONSTRAINT [PK_customer_ledger] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_customer_ledger_sign] CHECK (([Type] = 'Credit' AND [Amount] < 0) OR ([Type] = 'Cashback' AND [Amount] > 0)),
        CONSTRAINT [CK_customer_ledger_type] CHECK ([Type] IN ('Credit', 'Cashback')),
        CONSTRAINT [FK_customer_ledger_customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [backoffice].[customers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_customer_ledger_daily_closing_DailyClosingId] FOREIGN KEY ([DailyClosingId]) REFERENCES [backoffice].[daily_closing] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728154338_AddIdentityAuth'
)
BEGIN
    CREATE TABLE [backoffice].[non_cash_payments] (
        [Id] int NOT NULL IDENTITY,
        [Date] date NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [PaymentMethod] nvarchar(30) NOT NULL,
        [Note] nvarchar(500) NULL,
        [DailyClosingId] int NOT NULL,
        CONSTRAINT [PK_non_cash_payments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_non_cash_payments_daily_closing_DailyClosingId] FOREIGN KEY ([DailyClosingId]) REFERENCES [backoffice].[daily_closing] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728154338_AddIdentityAuth'
)
BEGIN
    CREATE TABLE [customer].[page_attributes] (
        [Id] int NOT NULL IDENTITY,
        [PageId] int NOT NULL,
        [AttributeName] nvarchar(200) NOT NULL,
        [AttributeType] nvarchar(30) NOT NULL DEFAULT N'Text',
        [LangId] int NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_page_attributes] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_page_attributes_type] CHECK ([AttributeType] IN ('Text', 'RichText', 'Image', 'Link', 'Video', 'Number', 'Date', 'Boolean')),
        CONSTRAINT [FK_page_attributes_languages_LangId] FOREIGN KEY ([LangId]) REFERENCES [customer].[languages] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_page_attributes_pages_PageId] FOREIGN KEY ([PageId]) REFERENCES [customer].[pages] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728154338_AddIdentityAuth'
)
BEGIN
    CREATE TABLE [backoffice].[expenses] (
        [Id] int NOT NULL IDENTITY,
        [Date] date NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Note] nvarchar(500) NULL,
        [DailyClosingId] int NOT NULL,
        [ReceiverId] int NOT NULL,
        CONSTRAINT [PK_expenses] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_expenses_daily_closing_DailyClosingId] FOREIGN KEY ([DailyClosingId]) REFERENCES [backoffice].[daily_closing] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_expenses_receivers_ReceiverId] FOREIGN KEY ([ReceiverId]) REFERENCES [backoffice].[receivers] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728154338_AddIdentityAuth'
)
BEGIN
    CREATE TABLE [backoffice].[investor_transactions] (
        [Id] int NOT NULL IDENTITY,
        [Date] date NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [TransactionType] nvarchar(20) NOT NULL,
        [Note] nvarchar(500) NULL,
        [DailyClosingId] int NOT NULL,
        [InvestorId] int NOT NULL,
        [ReceiverId] int NULL,
        CONSTRAINT [PK_investor_transactions] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_investor_txn_receiver] CHECK ([TransactionType] <> 'Expense' OR [ReceiverId] IS NOT NULL),
        CONSTRAINT [CK_investor_txn_type] CHECK ([TransactionType] IN ('Withdrawal', 'Expense')),
        CONSTRAINT [FK_investor_transactions_daily_closing_DailyClosingId] FOREIGN KEY ([DailyClosingId]) REFERENCES [backoffice].[daily_closing] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_investor_transactions_investors_InvestorId] FOREIGN KEY ([InvestorId]) REFERENCES [backoffice].[investors] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_investor_transactions_receivers_ReceiverId] FOREIGN KEY ([ReceiverId]) REFERENCES [backoffice].[receivers] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728154338_AddIdentityAuth'
)
BEGIN
    CREATE TABLE [backoffice].[employees] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(200) NOT NULL,
        [PhoneNumber] nvarchar(50) NULL,
        [Position] nvarchar(100) NULL,
        [BaseSalary] decimal(18,2) NOT NULL DEFAULT 0.0,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] datetime2 NULL,
        [UserId] int NULL,
        CONSTRAINT [PK_employees] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_employees_users_UserId] FOREIGN KEY ([UserId]) REFERENCES [backoffice].[users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728154338_AddIdentityAuth'
)
BEGIN
    CREATE TABLE [backoffice].[user_claims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_user_claims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_user_claims_users_UserId] FOREIGN KEY ([UserId]) REFERENCES [backoffice].[users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728154338_AddIdentityAuth'
)
BEGIN
    CREATE TABLE [backoffice].[user_logins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] int NOT NULL,
        CONSTRAINT [PK_user_logins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_user_logins_users_UserId] FOREIGN KEY ([UserId]) REFERENCES [backoffice].[users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728154338_AddIdentityAuth'
)
BEGIN
    CREATE TABLE [backoffice].[user_tokens] (
        [UserId] int NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_user_tokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_user_tokens_users_UserId] FOREIGN KEY ([UserId]) REFERENCES [backoffice].[users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728154338_AddIdentityAuth'
)
BEGIN
    CREATE TABLE [backoffice].[employee_ledger] (
        [Id] int NOT NULL IDENTITY,
        [Date] date NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Type] nvarchar(20) NOT NULL,
        [Note] nvarchar(500) NULL,
        [EmployeeId] int NOT NULL,
        [DailyClosingId] int NOT NULL,
        CONSTRAINT [PK_employee_ledger] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_employee_ledger_sign] CHECK ([Amount] < 0),
        CONSTRAINT [CK_employee_ledger_type] CHECK ([Type] IN ('Advance', 'Deduct')),
        CONSTRAINT [FK_employee_ledger_daily_closing_DailyClosingId] FOREIGN KEY ([DailyClosingId]) REFERENCES [backoffice].[daily_closing] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_employee_ledger_employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [backoffice].[employees] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728154338_AddIdentityAuth'
)
BEGIN
    CREATE TABLE [backoffice].[employee_working] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [Year] int NOT NULL,
        [Month] int NOT NULL,
        [Status] nvarchar(50) NULL,
        [WorkingDays] int NULL,
        [ActualWorkingDays] int NULL,
        [DeductionsTotal] decimal(18,2) NULL,
        [AdvanceTotal] decimal(18,2) NULL,
        [ActualSalary] decimal(18,2) NULL,
        [Total] decimal(18,2) NULL,
        [StartedAt] date NULL,
        [EndedAt] date NULL,
        [IsWorking] bit NOT NULL DEFAULT CAST(1 AS bit),
        [Note] nvarchar(500) NULL,
        CONSTRAINT [PK_employee_working] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_employee_working_employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [backoffice].[employees] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728154338_AddIdentityAuth'
)
BEGIN
    CREATE INDEX [IX_customer_ledger_CustomerId] ON [customer].[customer_ledger] ([CustomerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728154338_AddIdentityAuth'
)
BEGIN
    CREATE INDEX [IX_customer_ledger_DailyClosingId] ON [customer].[customer_ledger] ([DailyClosingId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728154338_AddIdentityAuth'
)
BEGIN
    CREATE UNIQUE INDEX [IX_daily_closing_Date] ON [backoffice].[daily_closing] ([Date]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728154338_AddIdentityAuth'
)
BEGIN
    CREATE INDEX [IX_employee_ledger_DailyClosingId] ON [backoffice].[employee_ledger] ([DailyClosingId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728154338_AddIdentityAuth'
)
BEGIN
    CREATE INDEX [IX_employee_ledger_EmployeeId] ON [backoffice].[employee_ledger] ([EmployeeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728154338_AddIdentityAuth'
)
BEGIN
    CREATE INDEX [IX_employee_working_EmployeeId] ON [backoffice].[employee_working] ([EmployeeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728154338_AddIdentityAuth'
)
BEGIN
    CREATE INDEX [IX_employees_UserId] ON [backoffice].[employees] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728154338_AddIdentityAuth'
)
BEGIN
    CREATE INDEX [IX_expenses_DailyClosingId] ON [backoffice].[expenses] ([DailyClosingId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728154338_AddIdentityAuth'
)
BEGIN
    CREATE INDEX [IX_expenses_ReceiverId] ON [backoffice].[expenses] ([ReceiverId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728154338_AddIdentityAuth'
)
BEGIN
    CREATE INDEX [IX_investor_transactions_DailyClosingId] ON [backoffice].[investor_transactions] ([DailyClosingId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728154338_AddIdentityAuth'
)
BEGIN
    CREATE INDEX [IX_investor_transactions_InvestorId] ON [backoffice].[investor_transactions] ([InvestorId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728154338_AddIdentityAuth'
)
BEGIN
    CREATE INDEX [IX_investor_transactions_ReceiverId] ON [backoffice].[investor_transactions] ([ReceiverId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728154338_AddIdentityAuth'
)
BEGIN
    CREATE UNIQUE INDEX [IX_languages_Code] ON [customer].[languages] ([Code]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728154338_AddIdentityAuth'
)
BEGIN
    CREATE INDEX [IX_non_cash_payments_DailyClosingId] ON [backoffice].[non_cash_payments] ([DailyClosingId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728154338_AddIdentityAuth'
)
BEGIN
    CREATE INDEX [IX_page_attributes_LangId] ON [customer].[page_attributes] ([LangId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728154338_AddIdentityAuth'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_page_attributes_PageAttrLang] ON [customer].[page_attributes] ([PageId], [AttributeName], [LangId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728154338_AddIdentityAuth'
)
BEGIN
    CREATE UNIQUE INDEX [IX_pages_PageName] ON [customer].[pages] ([PageName]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728154338_AddIdentityAuth'
)
BEGIN
    CREATE INDEX [IX_user_claims_UserId] ON [backoffice].[user_claims] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728154338_AddIdentityAuth'
)
BEGIN
    CREATE INDEX [IX_user_logins_UserId] ON [backoffice].[user_logins] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728154338_AddIdentityAuth'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [backoffice].[users] ([NormalizedEmail]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728154338_AddIdentityAuth'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [backoffice].[users] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728154338_AddIdentityAuth'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260728154338_AddIdentityAuth', N'8.0.11');
END;
GO

COMMIT;
GO

