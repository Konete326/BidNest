dotnet build
dotnet ef dbcontext scaffold "Server=(localdb)\MSSQLLocalDB;Database=bidnest;User Id=aptech;Password=aptech;TrustServerCertificate=True;" Microsoft.EntityFrameworkCore.SqlServer -o Models -c BidnestContext --schema dbo --data-annotations --force


Scaffold-DbContext "Data Source=localhost\SQLEXPRESS;Initial Catalog=bidnest;Integrated Security=True;Encrypt=True;TrustServerCertificate=True" Microsoft.EntityFrameworkCore.SqlServer -OutputDir Models
