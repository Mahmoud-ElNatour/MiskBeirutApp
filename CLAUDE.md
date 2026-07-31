## Architecture Context: Multi-Subdomain Routing

This is a single ASP.NET Core MVC project serving three subdomains, NOT three 
separate projects. Keep this in mind for any routing, controller, auth, or 
deployment work:

- `miskbeirut.com` → Customer-facing area (public, anonymous access)
- `backoffice.miskbeirut.com` → Admin area (Daily Closing, financials, payroll, 
  employee data — Admin and Employee roles only)
- `cms.miskbeirut.com` → Cms area (content/page management only — for 
  non-financial content editors)

Routing is handled via subdomain-to-area mapping in Program.cs (host-based 
middleware or RouteValueTransformer), NOT via separate projects, separate 
deployments, or path-based routing (e.g. no /admin or /cms path prefixes).

Rules to follow:
1. All three areas share ONE EF Core DbContext and ONE set of models. Do not 
   suggest splitting into separate class libraries or projects unless I 
   explicitly ask.
2. When adding a new controller/view, ask which Area it belongs to (Customer, 
   Admin, or Cms) if it's not obvious, and place it in the correct 
   /Areas/{AreaName}/ folder.
3. Role checks matter: Admin area = Admin + Employee roles (Employee limited 
   to Daily Closing only). Cms area = Admin + Content roles. Customer area = 
   anonymous/public.
4. Database schema is split into `backoffice` and `customer` SQL Server 
   schemas within the `new` database — always use fully qualified 
   schema-prefixed table names in queries/migrations.
5. Deployment target is MassiveGrid Windows Hosting via IIS — one site, 
   multiple host bindings (root + subdomains), not multiple app pools, unless 
   I say otherwise.
6. If a task seems to require a new area, subdomain, or project split, flag 
   it and ask before restructuring — don't assume.