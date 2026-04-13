using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MuuqWear.API.Interfaces;
using MuuqWear.API.Service;
using Supabase;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var url = builder.Configuration["SupaBase:Url"];
var key = builder.Configuration["Authentication:SupabaseApiKey"];
// Add services to the container.

var options = new SupabaseOptions { AutoRefreshToken = true, AutoConnectRealtime = true, Schema = "MuuqWear" };
builder.Services.AddSingleton(new Supabase.Client(url!, key, options));
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProductService, ProductService>();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAuthorization();
var bytes = Encoding.UTF8.GetBytes(builder.Configuration["Authentication:JwtSecret"]!);
builder.Services
.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.Authority = "http://127.0.0.1:54321/auth/v1";
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateAudience = false
    };
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
