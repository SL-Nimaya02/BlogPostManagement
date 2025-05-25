using BlogPostManagement.Context;
using BlogPostManagement.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

//Load JWT Config
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
builder.Services.Configure<JwtSettings>(jwtSettings);

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!)),
            ClockSkew = TimeSpan.Zero,
            ValidateLifetime = true
        };
    });

builder.Services.AddAuthorization();


// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//Add DbContext(Memory database use.)
builder.Services.AddDbContext<MyDataContext>(Option =>
{
    Option.UseInMemoryDatabase("PostDb");
    Option.UseInMemoryDatabase("CategoryDb");
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//Login Endpoint
app.MapPost("/api/login", (LoginDto loginDto, IOptions<JwtSettings> jwtSettings) =>
{
    if (loginDto.Username == "admin" && loginDto.Password == "password")
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(jwtSettings.Value.Key);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, loginDto.Username) }),
            Expires = DateTime.UtcNow.AddMinutes(jwtSettings.Value.ExpiresInMinutes),
            Issuer = jwtSettings.Value.Issuer,
            Audience = jwtSettings.Value.Audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var jwt = tokenHandler.WriteToken(token);

        return Results.Ok(new { token = jwt });
    }

    return Results.Unauthorized();
});


// Post Crud Methods
app.MapPost("/SavePost", async (Post post, MyDataContext db) =>
{
    await db.Posts.AddAsync(post);
    await db.SaveChangesAsync();
    return Results.Created($"/save/{post.Id}", post);
})
    .RequireAuthorization();

app.MapGet("/GetAllPosts", async (MyDataContext db) =>
{
    return Results.Ok(await db.Posts.ToListAsync());
});

app.MapGet("/GetPosts/{id}", async (int id,MyDataContext db) =>
{
    var post = await db.Posts.FindAsync(id);
    if (post is null) Results.NotFound();

    return Results.Ok(post);
});

app.MapPut("/UpdatePosts/{id}", async (int id, Post inputPost,MyDataContext db) =>
{
    var post = await db.Posts.FindAsync(id);
    if (post is null) return Results.NotFound();
    
    post.Title = inputPost.Title;
    post.Content = inputPost.Content;

    await db.SaveChangesAsync();
    return Results.NoContent();
})
    .RequireAuthorization();

app.MapDelete("/DeletePosts/{id}", async (int id, MyDataContext db) =>
{
    var post = await db.Posts.FindAsync(id);
    if (post is null) return Results.NotFound();

    db.Posts.Remove(post);
    await db.SaveChangesAsync();
   
    return Results.Ok(post);
})
    .RequireAuthorization();


// Category Crud Methods
app.MapPost("/SaveCategory", async (Category category, MyDataContext db) =>
{
    await db.Categories.AddAsync(category);
    await db.SaveChangesAsync();
    return Results.Created($"/save/{category.Id}", category);
}).RequireAuthorization();

app.MapGet("/GetAllCategories", async (MyDataContext db) =>
{
    return Results.Ok(await db.Categories.ToListAsync());
});

app.MapGet("/GetCategories/{id}", async (int id, MyDataContext db) =>
{
    var category = await db.Categories.FindAsync(id);
    if (category is null) Results.NotFound();

    return Results.Ok(category);
});

app.MapPut("/UpdateCategories/{id}", async (int id, Category inputCategory, MyDataContext db) =>
{
    var category = await db.Categories.FindAsync(id);
    if (category is null) return Results.NotFound();

    category.Name = inputCategory.Name;
   
    await db.SaveChangesAsync();
    return Results.NoContent();
}).RequireAuthorization();

app.MapDelete("/DeleteCategories/{id}", async (int id, MyDataContext db) =>
{
    var category = await db.Categories.FindAsync(id);
    if (category is null) return Results.NotFound();

    db.Categories.Remove(category);
    await db.SaveChangesAsync();

    return Results.Ok(category);
}).RequireAuthorization();

app.UseAuthentication();
app.UseAuthorization();

app.Run();
