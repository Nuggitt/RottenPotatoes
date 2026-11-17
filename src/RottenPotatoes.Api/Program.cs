using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using RottenPotatoes.Api.Interfaces;
using RottenPotatoes.Api.Security;
using RottenPotatoes.Api.Services;
using RottenPotatoes.Data.Context;

var builder = WebApplication.CreateBuilder(args);

// EF Core (Database First). Connection string kommer fra miljøvariablen
// ConnectionStrings__RottenPotatoes (MySQL-brugeren rp_api) – aldrig fra kildekoden.
builder.Services.AddRottenPotatoesData(builder.Configuration.GetConnectionString("RottenPotatoes"), "RottenPotatoes");

builder.Services.AddScoped<IMovieService, MovieService>();
builder.Services.AddScoped<ISeriesService, SeriesService>();
builder.Services.AddScoped<IRatingService, RatingService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// JWT: nøglen kommer fra JWT_SECRET i .env (miljøvariablen Jwt__Key).
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwt = jwtSection.Get<JwtSettings>() ?? new JwtSettings();
if (jwt.Key.Length < 32)
{
    throw new InvalidOperationException("Jwt:Key mangler eller er under 32 tegn. Sæt JWT_SECRET i .env (se .env.example).");
}
builder.Services.Configure<JwtSettings>(jwtSection);

// Authentication: valider tokenets signatur, udsteder, modtager og udløb.
// Claims bruges med deres korte JWT-navne ("sub", "unique_name", "role").
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = JwtRegisteredClaimNames.UniqueName,
            RoleClaimType = "role", // det [Authorize(Roles = "Admin")] tjekker
        };
    });

// Authorization: reglerne sættes på endpoints med [Authorize] / [Authorize(Roles = RoleNames.Admin)].
builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // "Authorize"-knap i Swagger UI: indsæt tokenet fra POST /api/auth/login.
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Indsæt JWT'en fra POST /api/auth/login (uden 'Bearer ').",
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = [],
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Rækkefølgen betyder noget: først "hvem er du?" (401), så "hvad må du?" (403).
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
