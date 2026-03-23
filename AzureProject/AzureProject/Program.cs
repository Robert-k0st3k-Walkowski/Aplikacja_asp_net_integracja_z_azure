using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using AzureProject.Services.AI;
using AzureProject.Services.Database;
using AzureProject.Services.Storage;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllersWithViews();
//Linijki odpowiadaj¹ce za rejestracjê us³ug
builder.Services.AddScoped<IAzureBlobStorageService, AzureBlobStorageService>();
builder.Services.AddSingleton<IAzureCosmosDatabaseService, AzureCosmosDatabaseService>();
builder.Services.AddSingleton<IAzureComputerVisionService, AzureComputerVisionService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
