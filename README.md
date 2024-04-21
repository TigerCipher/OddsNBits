# Odds & Bits
---
A WIP recreation of my dev log / blog website originally created with as a `.NET 7 Blazor Server App`. This project is a `.NET 8 Blazor Web App` with server side interactivity.

If for whatever reason you choose to use this as the basis for your own blog, please be sure to credit myself as well as the author of the bootstrap template (which I haven't picked one yet.)

You will need to create an appsettings.json for a production environment. For a development environment, the provided appsettings.Development.json assumes the use of SQLExpress.

Using SQL Studio Manager (or whatever you prefer), add the following rows to the `dbo.AspNetRoles` table:
+ id: admin, name: Admin, normalized..: ADMIN

Run the web app and create a test account (and then log out). Close the app and in the `dbo.AspNetUserRoles` table add a row with the created user ID (got from table `dbo.AspNetUsers`) and the `role` id.

Design is inspired by multiple themes, will link once I finallize my design decisions on what I want to use.