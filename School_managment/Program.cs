using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using School_managment.Features.Classes.Models;
using School_managment.Features.Classes.Orchestrators;
using School_managment.Features.Teachers.Orchestrators;
using School_managment.Features.Timetables.Orchestrators;
using School_managment.Infrastructure;
using School_managment.Infrastructure.Interface;
using School_managment.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<SchoolDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped(typeof(IRepository<>), typeof(GeneralRepository<>));
builder.Services.AddScoped<IClassRepository<Class>, ClassRepository>();
builder.Services.AddScoped<TeacherOrchestrator>();
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
});
builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddMaps(typeof(TeacherOrchestrator).Assembly);
});
builder.Services.AddScoped<ClassOrchestrator>();
builder.Services.AddScoped<ITimeTableOrchestrator, TimeTableOrchestrator>();

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy
                .WithOrigins("http://localhost:4200") // Angular local
                .AllowAnyHeader()
                .AllowAnyMethod();

            policy.SetIsOriginAllowed(origin => true)
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseCors("AllowFrontend");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
        c.RoutePrefix = string.Empty; // Swagger على /index.html
    });
}
//app.UseSwagger();
//app.UseSwaggerUI(c =>
//{
//    c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
//    c.RoutePrefix = string.Empty;
//});


app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.UseDefaultFiles();   
app.UseStaticFiles();

app.MapFallbackToFile("index.html");
app.Run();
