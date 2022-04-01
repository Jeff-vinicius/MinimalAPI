using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using MinimalAPI.Data;
using MinimalAPI.Models;
using MiniValidation;
using NetDevPack.Identity;
using NetDevPack.Identity.Jwt;
using NetDevPack.Identity.Model;

var builder = WebApplication.CreateBuilder(args);

#region Configure Services

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddDbContext<MinimalContextDb>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentityEntityFrameworkContextConfiguration(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
    b => b.MigrationsAssembly("MinimalAPI")));

builder.Services.AddIdentityConfiguration();
builder.Services.AddJwtConfiguration(builder.Configuration, "AppSettings");

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ExcluirCliente",
        policy => policy.RequireClaim("ExcluirCliente"));
});

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Minimal API",
        Description = "Desenvolvido por Jefferson Vinicius",
        Contact = new OpenApiContact { Name = "Jefferson Vinicius", Email = "jefferson.vinicius.souza@gmail.com" },
        License = new OpenApiLicense { Name = "MIT", Url = new Uri("https://opensource.org/licenses/MIT") }
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Insira o token JWT desta maneira: Bearer {seu token}",
        Name = "Authorization",
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

var app = builder.Build();

#endregion

#region Configure Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthConfiguration();
app.UseHttpsRedirection();

MapActions(app);

app.Run();

#endregion

#region Actions
void MapActions(WebApplication app)
{
    app.MapPost("/registro", [AllowAnonymous] async (
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            IOptions<AppJwtSettings> appJwtSettings,
            RegisterUser registerUser) =>
    {
        if (registerUser == null)
            return Results.BadRequest("Usuário não informado");

        if (!MiniValidator.TryValidate(registerUser, out var errors))
            return Results.ValidationProblem(errors);

        var user = new IdentityUser
        {
            UserName = registerUser.Email,
            Email = registerUser.Email,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, registerUser.Password);

        if (!result.Succeeded)
            return Results.BadRequest(result.Errors);

        var jwt = new JwtBuilder()
                    .WithUserManager(userManager)
                    .WithJwtSettings(appJwtSettings.Value)
                    .WithEmail(user.Email)
                    .WithJwtClaims()
                    .WithUserClaims()
                    .WithUserRoles()
                    .BuildUserResponse();

        return Results.Ok(jwt);

    }).ProducesValidationProblem()
          .Produces(StatusCodes.Status200OK)
          .Produces(StatusCodes.Status400BadRequest)
          .WithName("RegistroUsuario")
          .WithTags("Usuario");

    app.MapPost("/login", [AllowAnonymous] async (
        SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager,
        IOptions<AppJwtSettings> appJwtSettings,
        LoginUser loginUser) =>
    {
        if (loginUser == null)
            return Results.BadRequest("Usuário não informado");

        if (!MiniValidator.TryValidate(loginUser, out var errors))
            return Results.ValidationProblem(errors);

        var result = await signInManager.PasswordSignInAsync(loginUser.Email, loginUser.Password, false, true);

        if (result.IsLockedOut)
            return Results.BadRequest("Usuário bloqueado");

        if (!result.Succeeded)
            return Results.BadRequest("Usuário ou senha inválidos");

        var jwt = new JwtBuilder()
                    .WithUserManager(userManager)
                    .WithJwtSettings(appJwtSettings.Value)
                    .WithEmail(loginUser.Email)
                    .WithJwtClaims()
                    .WithUserClaims()
                    .WithUserRoles()
                    .BuildUserResponse();

        return Results.Ok(jwt);

    }).ProducesValidationProblem()
      .Produces(StatusCodes.Status200OK)
      .Produces(StatusCodes.Status400BadRequest)
      .WithName("LoginUsuario")
      .WithTags("Usuario");



    app.MapGet("/cliente", [AllowAnonymous] async (
        MinimalContextDb context) =>
        await context.Clientes.ToListAsync())
        .WithName("GetCliente")
        .WithTags("Cliente");

    app.MapGet("/cliente/{id}", async (
        Guid id,
        MinimalContextDb context) =>
        await context.Clientes.FindAsync(id)
            is Cliente cliente
                ? Results.Ok(cliente)
                : Results.NotFound())
        .Produces<Cliente>(StatusCodes.Status200OK)
        .Produces<Cliente>(StatusCodes.Status404NotFound)
        .WithName("GetClientePorId")
        .WithTags("Cliente");

    app.MapPost("/cliente", [Authorize] async (
        MinimalContextDb context,
        Cliente cliente) =>
    {
        if (!MiniValidator.TryValidate(cliente, out var errors))
            return Results.ValidationProblem(errors);

        context.Clientes.Add(cliente);
        var result = await context.SaveChangesAsync();

        return result > 0
            ? Results.CreatedAtRoute("GetClientePorId", new { id = cliente.Id }, cliente)
            : Results.BadRequest("Houve um erro ao salvar o registro");

    }).ProducesValidationProblem()
        .Produces<Cliente>(StatusCodes.Status201Created)
        .Produces<Cliente>(StatusCodes.Status400BadRequest)
        .WithName("PostCliente")
        .WithTags("Cliente");

    app.MapPut("/cliente/{id}", [Authorize] async (
            Guid id,
            MinimalContextDb context,
            Cliente fornecedor) =>
    {
        var clienteBanco = await context.Clientes.AsNoTracking<Cliente>()
                                                 .FirstOrDefaultAsync(c => c.Id == id);

        if (clienteBanco == null) return Results.NotFound();

        if (!MiniValidator.TryValidate(fornecedor, out var errors))
            return Results.ValidationProblem(errors);

        context.Clientes.Update(fornecedor);
        var result = await context.SaveChangesAsync();

        return result > 0
            ? Results.NoContent()
            : Results.BadRequest("Houve um erro ao salvar o registro");

    }).ProducesValidationProblem()
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status400BadRequest)
        .WithName("PutCliente")
        .WithTags("Cliente");

    app.MapDelete("/cliente/{id}", [Authorize] async (
            Guid id,
            MinimalContextDb context) =>
    {
        var cliente = await context.Clientes.FindAsync(id);
        if (cliente == null) return Results.NotFound();

        context.Clientes.Remove(cliente);
        var result = await context.SaveChangesAsync();

        return result > 0
            ? Results.NoContent()
            : Results.BadRequest("Houve um erro ao salvar o registro");

    }).Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .RequireAuthorization("ExcluirCliente")
        .WithName("DeleteCliente")
        .WithTags("Cliente");
}
#endregion