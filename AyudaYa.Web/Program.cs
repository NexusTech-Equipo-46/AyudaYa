using AyudaYa.Negocio;

var builder = WebApplication.CreateBuilder(args);

// La composición de Data + Identity vive en Negocio. Web no referencia
// AyudaYa.Data ni consulta el DbContext.
builder.Services.AddAyudaYa(builder.Configuration);
builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
