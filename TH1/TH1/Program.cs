using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using TH1.Data;
using TH1.Patterns.AbstractFactory;
using TH1.Patterns.Builder;
using TH1.Patterns.Decorator;
using TH1.Patterns.Facade;
using TH1.Repositories;
using TH1.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configure DbContext
builder.Services.AddDbContext<DataContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure Repositories
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

// Configure Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<IOrderService>(provider => {
    var baseService = provider.GetRequiredService<OrderService>();
    return new TaxOrderDecorator(baseService); // Bọc thêm lớp tính thuế
});
builder.Services.AddScoped<IOrderFacade, OrderFacade>();

// Configure Design Patterns
builder.Services.AddTransient<IOrderBuilder, OrderBuilder>();
// A choice has to be made for the default notification factory
builder.Services.AddTransient<INotificationFactory, EmailNotificationFactory>();


// Configure JWT Authentication
var key = Encoding.ASCII.GetBytes(builder.Configuration["Jwt:Key"]);
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "TH1 E-Commerce API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");

        c.HeadContent = @"
        <script>
            // Hàm lưu token sau khi login thành công
            async function loginAndSaveToken(username, password) {
                const response = await fetch('/api/login', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ username, password })
                });
                const data = await response.json();
                if (data && data.token) {
                    localStorage.setItem('authToken', data.token);
                }
            }

            // Observer để chờ nút Authorize xuất hiện
            const observer = new MutationObserver(mutations => {
                const btn = document.querySelector('.auth-wrapper .authorize');
                if (btn) {
                    const token = localStorage.getItem('authToken');
                    if (token) {
                        // Tự động điền token vào Authorize
                        const ui = window.ui;
                        if (ui) {
                            ui.authActions.authorize({
                                'Bearer': {
                                    name: 'Authorization',
                                    schema: {
                                        type: 'apiKey',
                                        in: 'header',
                                        name: 'Authorization'
                                    },
                                    value: 'Bearer ' + token
                                }
                            });
                        }
                    }
                }
            });

            observer.observe(document.body, { childList: true, subtree: true });
        </script>";
    });
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Seed the database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<DataContext>();
        context.Database.Migrate(); // apply migrations
        // you can add seeding data here if needed
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the DB.");
    }
}


app.Run();
