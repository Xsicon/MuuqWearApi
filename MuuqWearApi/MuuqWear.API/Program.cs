using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MuuqWear.API.Interfaces;
using MuuqWear.API.Service;
using MuuqWear.Application.Interfaces;
using MuuqWear.Application.Service;
using MuuqWear.Application.Shared;
using Supabase;
using System.Text;
using ContentService = MuuqWear.API.Service.ContentService;


var builder = WebApplication.CreateBuilder(args);
var url = builder.Configuration["SupaBase:Url"];
var key = builder.Configuration["Authentication:SupabaseApiKey"];
//var serviceRoleKey = builder.Configuration["Supabase:ServiceRoleKey"];
//if (string.IsNullOrEmpty(serviceRoleKey))
//	throw new InvalidOperationException(
//		"Supabase:ServiceRoleKey is not configured. " +
//		"Set it via user-secrets or environment variable.");
// Add services to the container.

var options = new SupabaseOptions { AutoRefreshToken = false, AutoConnectRealtime = true, Schema = "MuuqWear" };
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<SupabaseClientFactory>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IAddressService, AddressService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IContentService, ContentService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IOrderReturnService, OrderReturnService>();
builder.Services.AddScoped<IAdminSettingService, AdminSettingService>();
builder.Services.AddSingleton<SupabaseAdminClientFactory>();
builder.Services.AddScoped<IVoteService, VoteService>();
builder.Services.AddScoped<IHelpCenterService, HelpService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAuthorization();
builder.Services.AddCors(options =>
{
	options.AddPolicy("AllowFrontend", policy =>
	{
		policy.WithOrigins("http://localhost:5276") // your frontend URL
			  .AllowAnyHeader()
			  .AllowAnyMethod()
			  .AllowCredentials(); //  required for cookies
	});
});

//builder.Services
//.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//.AddJwtBearer(options =>
//{
//	// directly provide signing keys 
//	var jwks = new JsonWebKeySet(builder.Configuration["SupaBase:JwksJson"]!);
//	var signingKeys = jwks.GetSigningKeys();

//	options.TokenValidationParameters = new TokenValidationParameters
//	{
//		ValidateIssuerSigningKey = true,
//		IssuerSigningKeys = signingKeys,
//		ValidateIssuer = false,
//		ValidateAudience = false,
//		ValidateLifetime = true,
//		RoleClaimType = "app_role"

//	};

//	options.RequireHttpsMetadata = false;

//	options.Events = new JwtBearerEvents
//	{
//		OnAuthenticationFailed = context =>
//		{
//			System.Diagnostics.Debug.WriteLine($"JWT rejected: {context.Exception.Message}"); ;
//			return Task.CompletedTask;
//		},
//		OnTokenValidated = context =>
//		{
//			System.Diagnostics.Debug.WriteLine("JWT Valid ");
//			return Task.CompletedTask;
//		}
//	};
//});
var jwtSecret = builder.Configuration["Authentication:JwtSecret"];
System.Diagnostics.Debug.WriteLine($"JWT Secret loaded: {!string.IsNullOrEmpty(jwtSecret)}");
System.Diagnostics.Debug.WriteLine($"JWT Secret length: {jwtSecret?.Length}");
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // ✅ switch from JWKS to legacy HS256 secret
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    builder.Configuration["Authentication:JwtSecret"]!)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            RoleClaimType = "app_role"
        };
        options.RequireHttpsMetadata = false;
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine(
                    $"JWT failed: {context.Exception.Message}");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Console.WriteLine("JWT Valid ✅");
                return Task.CompletedTask;
            }
        };
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
