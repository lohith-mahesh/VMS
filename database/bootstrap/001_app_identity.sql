:setvar AppServiceName "<rrvms-api-app-name>"

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'$(AppServiceName)')
BEGIN
    EXEC(N'CREATE USER [' + REPLACE(N'$(AppServiceName)', N']', N']]') + N'] FROM EXTERNAL PROVIDER');
END;

ALTER ROLE db_datareader ADD MEMBER [$(AppServiceName)];
ALTER ROLE db_datawriter ADD MEMBER [$(AppServiceName)];
