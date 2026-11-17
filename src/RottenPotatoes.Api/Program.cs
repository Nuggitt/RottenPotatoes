using RottenPotatoes.Api.Interfaces;
using RottenPotatoes.Api.Services;
using RottenPotatoes.Data.Context;

var builder = WebApplication.CreateBuilder(args);

// EF Core (Database First). Connection string kommer fra miljøvariablen
// ConnectionStrings__RottenPotatoes (MySQL-brugeren rp_api) – aldrig fra kildekoden.
builder.Services.AddRottenPotatoesData(builder.Configuration.GetConnectionString("RottenPotatoes"), "RottenPotatoes");

builder.Services.AddScoped<IMovieService, MovieService>();
builder.Services.AddScoped<ISeriesService, SeriesService>();

// AuthService er registreret, men endnu ikke implementeret (kaster NotImplementedException ved kald).
builder.Services.AddScoped<IAuthService, AuthService>();

// TODO: Authentication og authorization (fx builder.Services.AddAuthentication(...) og AddAuthorization(...)).

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// TODO: app.UseAuthentication(); app.UseAuthorization(); når login er implementeret.

app.MapControllers();

app.Run();
