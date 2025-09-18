dotnet ef dbcontext scaffold "Server=localhost,1433;Database=bidnest;User Id=sa;Password=YourStrong!Pass;TrustServerCertificate=True;" Microsoft.EntityFrameworkCore.SqlServer -o Models -c BidnestContext --schema dbo --data-annotations --force



dotnet ef dbcontext scaffold "Server=.;Database=bidnest;Trusted_Connection=True;" Microsoft.EntityFrameworkCore.SqlServer -o Models -c BidnestContext --data-annotations
