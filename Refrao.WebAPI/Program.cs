using Refrao.WebAPI.Configuracoes;
using Refrao.WebAPI.Context;
using Refrao.WebAPI.Interfaces;
using Refrao.WebAPI.Middleware;
using Refrao.WebAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<RefraoDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Configure a ConnectionString")));

builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IArtistaService, ArtistaService>();
builder.Services.AddScoped<IGeneroService, GeneroService>();
builder.Services.AddScoped<IAlbumService, AlbumService>();
builder.Services.AddScoped<IMusicaService, MusicaService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IComentarioService, ComentarioService>();
builder.Services.AddScoped<ISugestaoService, SugestaoService>();

builder.Services.AddControllers();

var jwt = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException("Configure a seção Jwt.");

if (string.IsNullOrWhiteSpace(jwt.Issuer) || string.IsNullOrWhiteSpace(jwt.Audience)
    || Encoding.UTF8.GetByteCount(jwt.Key) < 32 || jwt.ExpirationMinutes is < 1 or > 1440)
    throw new InvalidOperationException("Configure Jwt:Issuer, Jwt:Audience, Jwt:ExpirationMinutes e Jwt:Key (mínimo de 32 bytes)");

builder.Services.AddSingleton(jwt);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
            RequireSignedTokens = true,
            RequireExpirationTime = true,
            ClockSkew = TimeSpan.Zero,
            NameClaimType = JwtRegisteredClaimNames.Sub,
            RoleClaimType = ClaimTypes.Role
        };

        options.Events = new JwtBearerEvents
        {
            // A cada requisição confere no banco se o usuário ainda existe,
            // está ativo e mantém o mesmo perfil do token.
            OnTokenValidated = async context =>
            {
                var idTexto = context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub);
                if (!int.TryParse(idTexto, out var id))
                {
                    context.Fail("Token sem ID de usuário");
                    return;
                }

                var servico = context.HttpContext.RequestServices.GetRequiredService<IUsuarioService>();
                var usuario = await servico.BuscarPorId(id);
                if (usuario is null || !usuario.Ativo || context.Principal?.FindFirstValue(ClaimTypes.Role) != usuario.Perfil)
                    context.Fail("Usuário inativo, inexistente ou com permissão alterada.");
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Refrão! API",
        Version = "v1",
        Description = "Refrão! — API do catálogo de músicas pop (músicas, artistas, álbuns, gêneros, usuários e autenticação JWT)."
    });

    var xml = Path.Combine(AppContext.BaseDirectory, "Refrao.WebAPI.xml");
    if (File.Exists(xml)) options.IncludeXmlComments(xml);

    options.InferSecuritySchemes();
    options.OperationFilter<SwaggerAuthorizeOperationFilter>();
});

// O front-end é servido pela própria API (wwwroot). O CORS libera também
// o Live Server do VS Code (porta 5500) e a porta 3000 usada no Plot!.
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontEnd", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5500", "http://127.0.0.1:5500")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// Cria o banco a partir de Database/RefrãoDatabase.sql se ele ainda não existir.
try
{
    await InicializadorBanco.CriarSeNecessario(app.Configuration, app.Logger);
}
catch (Exception erro)
{
    app.Logger.LogError(erro,
        "Não foi possível criar/verificar o banco RefrãoDB. Confira se o SQL Server está rodando e a ConnectionString no appsettings.json.");
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Refrão! API v1");
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "Refrão! — Swagger";
    });
}

app.UseMiddleware<ErroGlobalMiddleware>();
app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors("FrontEnd");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
