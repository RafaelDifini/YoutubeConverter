using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.WebHost.ConfigureKestrel(options =>
{
    var port = Environment.GetEnvironmentVariable("PORT");

    if (!string.IsNullOrEmpty(port))
    {
        options.ListenAnyIP(int.Parse(port));
        port = Environment.GetEnvironmentVariable("PORT") ?? "5173";
    }
    else
    {
        options.ListenLocalhost(5173);
        options.ListenLocalhost(7259, listenOptions =>
        {
            listenOptions.UseHttps();
        });
    }
});

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
    pattern: "{controller=Youtube}/{action=Index}/{id?}");

app.Lifetime.ApplicationStarted.Register(() =>
{
    var urls = string.Join(", ", app.Urls);
    Console.WriteLine($"Servidor iniciado. Acesse: {urls}");
});

app.Run();
