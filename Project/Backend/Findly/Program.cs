using System.Text;
using System.Text.Json.Serialization;
using LostFound.DAL;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// 1. Controllers & Swagger
// Enums are exchanged as strings ("Lost", "Open", "Admin"...) so the JS frontend can read/send them.
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();

// 2. Connection String (جاهز لاستخدامه في DAL)
var connectionString = builder.Configuration.GetConnectionString("ConnectionString");

// 3. Dependency Injection (سجّل كلاسات/واجهات الـ BLL والـ DAL هنا)
// builder.Services.AddScoped<...>();

// 4. JWT Authentication & Authorization
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"]
    ?? throw new InvalidOperationException("JWT Key is missing in appsettings.json.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// 5. CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowClient", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// DAL reads the connection string from configuration; it MUST be initialized or every DB call throws.
clsDataAccess.Initialize(app.Configuration);

// 6. Middleware Pipeline


// Redirecting http->https breaks browser fetch()/CORS preflight from the local frontend, so only in production.
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseCors("AllowClient");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();