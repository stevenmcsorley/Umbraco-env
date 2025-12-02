#!/bin/bash
# Script to fix OpenIddict redirect URIs
# Run this after Umbraco starts to ensure HTTPS redirect URI is set

docker exec mydockerproject_database /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'Password1234' -d umbracoDb -C << 'EOFSQL'
SET QUOTED_IDENTIFIER ON;
DECLARE @q CHAR(1) = CHAR(34);
DECLARE @redirect NVARCHAR(MAX) = '[' + @q + 'https://hotel.halfagiraf.com/umbraco/oauth_complete' + @q + ',' + @q + 'http://hotel.halfagiraf.com/umbraco/oauth_complete' + @q + ']';
DECLARE @postlogout NVARCHAR(MAX) = '[' + @q + 'https://hotel.halfagiraf.com/umbraco/oauth_complete' + @q + ',' + @q + 'http://hotel.halfagiraf.com/umbraco/oauth_complete' + @q + ',' + @q + 'https://hotel.halfagiraf.com/umbraco/logout' + @q + ',' + @q + 'http://hotel.halfagiraf.com/umbraco/logout' + @q + ']';
UPDATE umbracoOpenIddictApplications SET RedirectUris = @redirect, PostLogoutRedirectUris = @postlogout WHERE ClientId = 'umbraco-back-office';
SELECT 'Redirect URIs updated' as Status;
EOFSQL

