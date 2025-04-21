using ApiBougies.Helpers;
using Bougies.Data;
using Bougies.Repositories;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

HelperCryptography.Initialize(builder.Configuration);
builder.Services.AddHttpContextAccessor();
HelperActionBougies helper = new HelperActionBougies(builder.Configuration);
builder.Services.AddSingleton<HelperActionBougies>(helper);
builder.Services.AddAuthentication(helper.GetAuthenticateSchema()).AddJwtBearer(helper.GetJwtBearerOptions());

string connectionString = builder.Configuration.GetConnectionString("SqlAzure");
builder.Services.AddDbContext<BougiesContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddTransient<RepositoryBougies>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseSession();

app.MapGet("/", context =>
{
    context.Response.Redirect("/scalar");
    return Task.CompletedTask;
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
