using ApiBougies.Helpers;
using ApiBougies.Services;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage.Blobs;
using Bougies.Data;
using Bougies.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAzureClients(factory =>
{
    factory.AddSecretClient(builder.Configuration.GetSection("KeyVault"));

});
SecretClient secretClient = builder.Services.BuildServiceProvider().GetService<SecretClient>();

HelperCryptography.Initialize(builder.Configuration, secretClient);
builder.Services.AddTransient<HelperUserToken>();
builder.Services.AddHttpContextAccessor();


HelperActionBougies helper = new HelperActionBougies(builder.Configuration, secretClient);
builder.Services.AddSingleton<HelperActionBougies>(helper);
builder.Services.AddAuthentication(helper.GetAuthenticateSchema()).AddJwtBearer(helper.GetJwtBearerOptions());

KeyVaultSecret secret = await secretClient.GetSecretAsync("SqlAzure");
string connectionString = secret.Value;

KeyVaultSecret secretStorage = await secretClient.GetSecretAsync("StorageAccount");
string storage = secretStorage.Value;
BlobServiceClient blobService = new BlobServiceClient(storage);
builder.Services.AddTransient<BlobServiceClient>(x => blobService);
builder.Services.AddTransient<ServiceStorageBlob>();
builder.Services.AddTransient<ServiceStorageBlob>();

//string connectionString = builder.Configuration.GetConnectionString("SqlAzure");
builder.Services.AddDbContext<BougiesContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddTransient<RepositoryBougies>();
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
