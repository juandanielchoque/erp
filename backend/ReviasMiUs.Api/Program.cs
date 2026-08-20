using ReviasMiUs.Application.Abstractions;
using ReviasMiUs.Application.Dtos;
using ReviasMiUs.Application.Services;
using ReviasMiUs.Domain.Common;
using ReviasMiUs.Infrastructure.Persistence;
using ReviasMiUs.Infrastructure.Security;
using ReviasMiUs.Api.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.DataProtection;
using ReviasMiUs.Application.Security;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddRateLimiter(options => options.AddFixedWindowLimiter("public-ordering", limiter =>
{
    limiter.PermitLimit = 20;
    limiter.Window = TimeSpan.FromMinutes(1);
    limiter.QueueLimit = 0;
}));
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "Opaque access token",
        In = ParameterLocation.Header,
        Description = "Ingrese el token obtenido en /api/auth/login"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = Array.Empty<string>()
    });
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy => policy
        .WithOrigins("http://localhost:5173", "http://localhost:5174")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

builder.Services.AddAuthentication("Bearer")
    .AddScheme<AuthenticationSchemeOptions, TokenAuthenticationHandler>("Bearer", _ => { });
builder.Services.AddAuthorization(options =>
{
    foreach (var permission in Permissions.All)
        options.AddPolicy(permission, policy => policy.RequireClaim("permission", permission));
});
builder.Services.AddDataProtection()
    .SetApplicationName("ReviasMiUs")
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, ".keys")));

builder.Services.AddSingleton<InMemoryErpStore>();
builder.Services.AddSingleton<ICustomerRepository, InMemoryCustomerRepository>();
builder.Services.AddSingleton<ILeadRepository, InMemoryLeadRepository>();
builder.Services.AddSingleton<IProductRepository, InMemoryProductRepository>();
builder.Services.AddSingleton<ISalesOrderRepository, InMemorySalesOrderRepository>();
builder.Services.AddSingleton<IUserAccountRepository, InMemoryUserAccountRepository>();
builder.Services.AddSingleton<IRoleRepository, InMemoryRoleRepository>();
builder.Services.AddSingleton<IOperationsRepository, InMemoryOperationsRepository>();
builder.Services.AddSingleton<ISystemSettingsRepository, InMemorySystemSettingsRepository>();
builder.Services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
builder.Services.AddSingleton<ITokenService, InMemoryTokenService>();

builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<CrmService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<SalesOrderService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<UserAccountService>();
builder.Services.AddScoped<RoleService>();
builder.Services.AddScoped<RestaurantOperationsService>();
builder.Services.AddScoped<SystemSettingsService>();
builder.Services.AddScoped<AuthService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Frontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.Use(async (context, next) =>
{
    try
    {
        await next(context);
    }
    catch (DomainException exception)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new { error = exception.Message });
    }
});

using (var scope = app.Services.CreateScope())
{
    var store = scope.ServiceProvider.GetRequiredService<InMemoryErpStore>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    SeedData.Initialize(store, passwordHasher);
}

app.MapGet("/", () => Results.Ok(new
{
    name = "ReviasMiUs ERP Core",
    status = "running",
    modules = new[] { "crm", "customers", "catalog", "sales-orders", "point-of-sale", "users", "tables", "cash-shifts", "kitchen", "billing", "dashboard" },
    billing = "internal documents enabled; SUNAT adapter pending credentials"
}));

var auth = app.MapGroup("/api/auth");
auth.MapPost("/login", (LoginRequest request, AuthService service, HttpResponse response) =>
{
    try
    {
        var result = service.Login(request);
        SetRefreshCookie(response, result.RefreshToken, result.RefreshExpiresAtUtc);
        return Results.Ok(new AuthResponse(result.AccessToken, result.AccessExpiresAtUtc, result.User));
    }
    catch (DomainException) { return Results.Unauthorized(); }
});
auth.MapPost("/refresh", (AuthService service, HttpRequest request, HttpResponse response) =>
{
    if (!request.Cookies.TryGetValue("revias_refresh", out var refreshToken)) return Results.Unauthorized();
    try
    {
        var result = service.Refresh(refreshToken);
        SetRefreshCookie(response, result.RefreshToken, result.RefreshExpiresAtUtc);
        return Results.Ok(new AuthResponse(result.AccessToken, result.AccessExpiresAtUtc, result.User));
    }
    catch (DomainException) { return Results.Unauthorized(); }
});
auth.MapPost("/logout", (AuthService service, HttpRequest request, HttpResponse response) =>
{
    request.Cookies.TryGetValue("revias_refresh", out var refreshToken);
    var authorization = request.Headers.Authorization.ToString();
    var accessToken = authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? authorization[7..].Trim() : null;
    service.Logout(refreshToken, accessToken);
    response.Cookies.Delete("revias_refresh");
    return Results.NoContent();
});
auth.MapGet("/me", (ClaimsPrincipal principal, AuthService service) => Results.Ok(service.Me(CurrentUserId(principal)))).RequireAuthorization();

var api = app.MapGroup("/api").RequireAuthorization();

api.MapGet("/customers", (CustomerService service) => Results.Ok(service.List())).RequireAuthorization(Permissions.CustomersView);
api.MapGet("/customers/search", (string term, CustomerService service) => Results.Ok(service.Search(term))).RequireAuthorization(Permissions.CustomersView);
api.MapPost("/customers", (CreateCustomerRequest request, CustomerService service) =>
{
    var created = service.Create(request);
    return Results.Created($"/api/customers/{created.Id}", created);
}).RequireAuthorization(Permissions.CustomersManage);
api.MapPut("/customers/{id:guid}", (Guid id, UpdateCustomerRequest request, CustomerService service) => Results.Ok(service.Update(id, request))).RequireAuthorization(Permissions.CustomersManage);
api.MapDelete("/customers/{id:guid}", (Guid id, CustomerService service) => { service.Delete(id); return Results.NoContent(); }).RequireAuthorization(Permissions.CustomersDelete);

api.MapGet("/crm/leads", (CrmService service) => Results.Ok(service.List())).RequireAuthorization(Permissions.CrmManage);
api.MapGet("/crm/leads/search", (string term, CrmService service) => Results.Ok(service.Search(term))).RequireAuthorization(Permissions.CrmManage);
api.MapPost("/crm/leads", (CreateLeadRequest request, CrmService service) =>
{
    var created = service.Create(request);
    return Results.Created($"/api/crm/leads/{created.Id}", created);
}).RequireAuthorization(Permissions.CrmManage);
api.MapPatch("/crm/leads/{id:guid}/stage", (Guid id, UpdateLeadStageRequest request, CrmService service) =>
{
    var updated = service.UpdateStage(id, request);
    return Results.Ok(updated);
}).RequireAuthorization(Permissions.CrmManage);
api.MapPut("/crm/leads/{id:guid}", (Guid id, UpdateLeadRequest request, CrmService service) => Results.Ok(service.Update(id, request))).RequireAuthorization(Permissions.CrmManage);
api.MapDelete("/crm/leads/{id:guid}", (Guid id, CrmService service) => { service.Delete(id); return Results.NoContent(); }).RequireAuthorization(Permissions.CrmManage);
api.MapGet("/crm/dashboard", (CrmService service) => Results.Ok(service.GetDashboard())).RequireAuthorization(Permissions.CrmManage);

api.MapGet("/products", (ProductService service) => Results.Ok(service.List())).RequireAuthorization(Permissions.ProductsView);
api.MapGet("/products/low-stock", (ProductService service) => Results.Ok(service.LowStock())).RequireAuthorization(Permissions.ProductsView);
api.MapPost("/products", (CreateProductRequest request, ProductService service) =>
{
    var created = service.Create(request);
    return Results.Created($"/api/products/{created.Id}", created);
}).RequireAuthorization(Permissions.ProductsManage);
api.MapPut("/products/{id:guid}", (Guid id, UpdateProductRequest request, ProductService service) => Results.Ok(service.Update(id, request))).RequireAuthorization(Permissions.ProductsManage);
api.MapDelete("/products/{id:guid}", (Guid id, ProductService service) => { service.Delete(id); return Results.NoContent(); }).RequireAuthorization(Permissions.ProductsManage);

api.MapGet("/sales-orders", (string? status, Guid? customerId, SalesOrderService service) =>
    Results.Ok(service.List(status, customerId))).RequireAuthorization(Permissions.SalesManage);
api.MapGet("/sales-orders/{id:guid}", (Guid id, SalesOrderService service) =>
    Results.Ok(service.GetById(id))).RequireAuthorization(Permissions.SalesManage);
api.MapPost("/sales-orders", (CreateSalesOrderRequest request, SalesOrderService service) =>
{
    var created = service.CreateQuotation(request);
    return Results.Created($"/api/sales-orders/{created.Id}", created);
}).RequireAuthorization(Permissions.SalesManage);
api.MapPost("/sales-orders/{id:guid}/confirm", (Guid id, SalesOrderService service) =>
    Results.Ok(service.Confirm(id))).RequireAuthorization(Permissions.SalesManage);
api.MapPost("/sales-orders/{id:guid}/cancel", (Guid id, SalesOrderService service) =>
    Results.Ok(service.Cancel(id))).RequireAuthorization(Permissions.SalesManage);
api.MapPut("/sales-orders/{id:guid}", (Guid id, CreateSalesOrderRequest request, SalesOrderService service) => Results.Ok(service.UpdateQuotation(id, request))).RequireAuthorization(Permissions.SalesManage);
api.MapDelete("/sales-orders/{id:guid}", (Guid id, SalesOrderService service) => { service.DeleteDraft(id); return Results.NoContent(); }).RequireAuthorization(Permissions.SalesManage);

api.MapGet("/dashboard", (DashboardService service) => Results.Ok(service.GetOverview())).RequireAuthorization(Permissions.DashboardView);

api.MapGet("/users", (UserAccountService service) => Results.Ok(service.List())).RequireAuthorization(Permissions.UsersManage);
api.MapPost("/users", (CreateUserAccountRequest request, UserAccountService service) =>
{
    var created = service.Create(request);
    return Results.Created($"/api/users/{created.Id}", created);
}).RequireAuthorization(Permissions.UsersManage);
api.MapPatch("/users/{id:guid}/role", (Guid id, UpdateUserRoleRequest request, UserAccountService service) =>
    Results.Ok(service.ChangeRole(id, request))).RequireAuthorization(Permissions.UsersManage);
api.MapPatch("/users/{id:guid}/status", (Guid id, UpdateUserStatusRequest request, UserAccountService service) =>
    Results.Ok(service.ChangeStatus(id, request))).RequireAuthorization(Permissions.UsersManage);
api.MapPut("/users/{id:guid}", (Guid id, UpdateUserAccountRequest request, UserAccountService service) => Results.Ok(service.Update(id, request))).RequireAuthorization(Permissions.UsersManage);
api.MapDelete("/users/{id:guid}", (Guid id, UserAccountService service) => { service.Delete(id); return Results.NoContent(); }).RequireAuthorization(Permissions.UsersManage);

api.MapGet("/roles", (RoleService service) => Results.Ok(service.List())).RequireAuthorization(Permissions.RolesManage);
api.MapGet("/roles/permissions", (RoleService service) => Results.Ok(service.ListPermissions())).RequireAuthorization(Permissions.RolesManage);
api.MapPost("/roles", (CreateRoleRequest request, RoleService service) =>
{
    var created = service.Create(request);
    return Results.Created($"/api/roles/{created.Id}", created);
}).RequireAuthorization(Permissions.RolesManage);
api.MapPut("/roles/{id:guid}", (Guid id, UpdateRoleRequest request, RoleService service) => Results.Ok(service.Update(id, request))).RequireAuthorization(Permissions.RolesManage);
api.MapDelete("/roles/{id:guid}", (Guid id, RoleService service) => { service.Delete(id); return Results.NoContent(); }).RequireAuthorization(Permissions.RolesManage);

api.MapGet("/restaurant/tables", (RestaurantOperationsService service) => Results.Ok(service.ListTables())).RequireAuthorization(Permissions.TablesView);
api.MapPost("/restaurant/tables", (CreateRestaurantTableRequest request, RestaurantOperationsService service) => Results.Created($"/api/restaurant/tables/{request.Number}", service.CreateTable(request))).RequireAuthorization(Permissions.TablesManage);
api.MapPut("/restaurant/tables/{number}", (string number, UpdateRestaurantTableRequest request, RestaurantOperationsService service) => Results.Ok(service.UpdateTable(number, request))).RequireAuthorization(Permissions.TablesManage);
api.MapDelete("/restaurant/tables/{number}", (string number, RestaurantOperationsService service) => { service.DeleteTable(number); return Results.NoContent(); }).RequireAuthorization(Permissions.TablesManage);
api.MapPost("/restaurant/tables/{number}/release", (string number, RestaurantOperationsService service) => Results.Ok(service.ReleaseTable(number))).RequireAuthorization(Permissions.TablesRelease);
api.MapPost("/restaurant/tables/{number}/qr/regenerate", (string number, RestaurantOperationsService service) => Results.Ok(service.RegenerateTableQr(number))).RequireAuthorization(Permissions.TablesManage);

api.MapGet("/cash-shifts", (RestaurantOperationsService service) => Results.Ok(service.ListShifts())).RequireAuthorization(Permissions.CashShiftsManage);
api.MapPost("/cash-shifts/open", (OpenCashShiftRequest request, ClaimsPrincipal principal, RestaurantOperationsService service) => Results.Ok(service.OpenShift(request with { UserId = CurrentUserId(principal) }))).RequireAuthorization(Permissions.CashShiftsManage);
api.MapPost("/cash-shifts/{id:guid}/movements", (Guid id, CashMovementRequest request, ClaimsPrincipal principal, RestaurantOperationsService service) =>
{
    if (!CanManageShift(id, principal, service)) return Results.Forbid();
    return Results.Ok(service.AddCashMovement(id, request));
}).RequireAuthorization(Permissions.CashShiftsManage);
api.MapPost("/cash-shifts/{id:guid}/close", (Guid id, CloseCashShiftRequest request, ClaimsPrincipal principal, RestaurantOperationsService service) =>
{
    if (!CanManageShift(id, principal, service)) return Results.Forbid();
    return Results.Ok(service.CloseShift(id, request));
}).RequireAuthorization(Permissions.CashShiftsManage);

api.MapGet("/kitchen/tickets", (RestaurantOperationsService service) => Results.Ok(service.ListKitchenTickets())).RequireAuthorization(Permissions.KitchenManage);
api.MapPost("/kitchen/tickets/{id:guid}/advance", (Guid id, RestaurantOperationsService service) => Results.Ok(service.AdvanceKitchenTicket(id))).RequireAuthorization(Permissions.KitchenManage);
api.MapPost("/kitchen/tickets/{id:guid}/cancel", (Guid id, RestaurantOperationsService service) => Results.Ok(service.CancelKitchenTicket(id))).RequireAuthorization(Permissions.KitchenManage);

api.MapGet("/billing/documents", (RestaurantOperationsService service) => Results.Ok(service.ListFiscalDocuments())).RequireAuthorization(Permissions.BillingView);
api.MapGet("/billing/documents/{id:guid}/receipt", (Guid id, RestaurantOperationsService service) => Results.Ok(service.GetPrintableReceipt(id))).RequireAuthorization(Permissions.BillingView);
api.MapPost("/billing/documents/{id:guid}/cancel", (Guid id, RestaurantOperationsService service) => Results.Ok(service.CancelFiscalDocument(id))).RequireAuthorization(Permissions.BillingCancel);
api.MapPost("/pos/checkout", (CompletePosSaleRequest request, ClaimsPrincipal principal, RestaurantOperationsService service) => Results.Ok(service.CompleteSale(request with { UserId = CurrentUserId(principal) }))).RequireAuthorization(Permissions.PosUse);

api.MapGet("/settings", (SystemSettingsService service) => Results.Ok(service.Get())).RequireAuthorization(Permissions.SettingsManage);
api.MapGet("/settings/receipt-template", (SystemSettingsService service) => Results.Ok(service.GetReceiptTemplate())).RequireAuthorization(Permissions.BillingView);
api.MapPut("/settings", (UpdateSystemSettingsRequest request, SystemSettingsService service) => Results.Ok(service.Update(request))).RequireAuthorization(Permissions.SettingsManage);

var publicOrdering = app.MapGroup("/api/public/table-ordering").RequireRateLimiting("public-ordering");
publicOrdering.MapGet("/{token}/menu", (string token, RestaurantOperationsService service) => Results.Ok(service.GetPublicMenu(token)));
publicOrdering.MapPost("/{token}/orders", (string token, CreatePublicTableOrderRequest request, RestaurantOperationsService service) => Results.Ok(service.CreatePublicTableOrder(token, request)));

app.Run();

static void SetRefreshCookie(HttpResponse response, string token, DateTime expiresAtUtc) => response.Cookies.Append("revias_refresh", token, new CookieOptions
{
    HttpOnly = true,
    Secure = false,
    SameSite = SameSiteMode.Strict,
    Expires = expiresAtUtc,
    Path = "/api/auth"
});

static Guid CurrentUserId(ClaimsPrincipal principal) => Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
static bool CanManageShift(Guid shiftId, ClaimsPrincipal principal, RestaurantOperationsService service) =>
    principal.IsInRole("Administrator") || service.ListShifts().Any(shift => shift.Id == shiftId && shift.UserId == CurrentUserId(principal));
