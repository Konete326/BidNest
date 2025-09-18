dotnet ef dbcontext scaffold "Server=localhost,1433;Database=bidnest;User Id=sa;Password=YourStrong!Pass;TrustServerCertificate=True;" Microsoft.EntityFrameworkCore.SqlServer -o Models -c BidnestContext --schema dbo --data-annotations --force


Scaffold-DbContext "Data Source=localhost\SQLEXPRESS;Initial Catalog=bidnest;Integrated Security=True;Encrypt=True;TrustServerCertificate=True" Microsoft.EntityFrameworkCore.SqlServer -OutputDir Models
