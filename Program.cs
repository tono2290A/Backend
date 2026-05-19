using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// =======================================
// SWAGGER
// =======================================

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// =======================================
// CONEXIÓN A SQL SERVER
// =======================================

builder.Services.AddDbContext<TareasDbContext>(options =>
{
    options.UseSqlServer(
         "Server=TareasDB.mssql.somee.com;Database=TareasDB;User Id=ANTONIO001_SQLLogin_1;Password=tono123A;TrustServerCertificate=True;");
});


// =======================================
// CORS
// =======================================

builder.Services.AddCors();

var app = builder.Build();


// =======================================
// ACTIVAR SWAGGER
// =======================================

app.UseSwagger();

app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Tareas");
    c.RoutePrefix = string.Empty; // 💡 Carga Swagger directo en la raíz de tu URL de Render
});


// =======================================
// ACTIVAR CORS
// =======================================

app.UseCors(policy =>
    policy.AllowAnyOrigin()
          .AllowAnyMethod()
          .AllowAnyHeader());

app.UseHttpsRedirection();


// ======================================================
// GET - OBTENER TODAS LAS TAREAS
// ======================================================

app.MapGet("/api/tareas", async (TareasDbContext db) =>
{
    // =========================================
    // LLAMANDO PROCEDIMIENTO ALMACENADO
    // EXEC ObtenerTareas
    // =========================================

    var tareas = await db.Tareas
        .FromSqlRaw("EXEC ObtenerTareas")
        .ToListAsync();

    return Results.Ok(tareas);
});


// ======================================================
// GET - OBTENER TAREA POR ID
// ======================================================

app.MapGet("/api/tareas/{id}", async (int id, TareasDbContext db) =>
{
    // =========================================
    // PARÁMETRO DEL PROCEDIMIENTO
    // =========================================

    var parametro = new SqlParameter("@Id", id);

    // =========================================
    // LLAMANDO PROCEDIMIENTO ALMACENADO
    // EXEC ObtenerTareaPorId
    // =========================================

    var tarea = await db.Tareas
        .FromSqlRaw(
            "EXEC ObtenerTareaPorId @Id",
            parametro)
        .FirstOrDefaultAsync();

    if (tarea == null)
    {
        return Results.NotFound(new
        {
            mensaje = "Tarea no encontrada"
        });
    }

    return Results.Ok(tarea);
});


// ======================================================
// POST - INSERTAR TAREA
// ======================================================

app.MapPost("/api/tareas", async (
    [FromBody] Todo nuevaTarea,
    TareasDbContext db) =>
{
    // =========================================
    // LLAMANDO PROCEDIMIENTO ALMACENADO
    // EXEC InsertarTarea
    // =========================================

    await db.Database.ExecuteSqlRawAsync(
        @"EXEC InsertarTarea
            @Title,
            @Due,
            @Time,
            @Category,
            @Completed",

        new SqlParameter("@Title", nuevaTarea.Title),
        new SqlParameter("@Due", nuevaTarea.Due),
        new SqlParameter("@Time", nuevaTarea.Time),
        new SqlParameter("@Category", nuevaTarea.Category),
        new SqlParameter("@Completed", nuevaTarea.Completed)
    );

    return Results.Ok(new
    {
        mensaje = "Tarea insertada correctamente"
    });
});


// ======================================================
// PUT - ACTUALIZAR TAREA
// ======================================================

app.MapPut("/api/tareas/{id}", async (
    int id,
    [FromBody] Todo tarea,
    TareasDbContext db) =>
{
    // =========================================
    // LLAMANDO PROCEDIMIENTO ALMACENADO
    // EXEC ActualizarTarea
    // =========================================

    await db.Database.ExecuteSqlRawAsync(
        @"EXEC ActualizarTarea
            @Id,
            @Title,
            @Due,
            @Time,
            @Category,
            @Completed",

        new SqlParameter("@Id", id),
        new SqlParameter("@Title", tarea.Title),
        new SqlParameter("@Due", tarea.Due),
        new SqlParameter("@Time", tarea.Time),
        new SqlParameter("@Category", tarea.Category),
        new SqlParameter("@Completed", tarea.Completed)
    );

    return Results.Ok(new
    {
        mensaje = "Tarea actualizada correctamente"
    });
});


// ======================================================
// DELETE - ELIMINAR TAREA
// ======================================================

app.MapDelete("/api/tareas/{id}", async (
    int id,
    TareasDbContext db) =>
{
    // =========================================
    // LLAMANDO PROCEDIMIENTO ALMACENADO
    // EXEC EliminarTarea
    // =========================================

    await db.Database.ExecuteSqlRawAsync(
        "EXEC EliminarTarea @Id",
        new SqlParameter("@Id", id)
    );

    return Results.Ok(new
    {
        mensaje = "Tarea eliminada correctamente"
    });
});


// =======================================
// EJECUTAR API (CONFIGURADO PARA RENDER)
// =======================================

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Run($"http://0.0.0:{port}");


// =======================================
// MODELO TODO
// =======================================

public class Todo
{
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("due")]
    public string Due { get; set; } = string.Empty;

    [JsonPropertyName("time")]
    public string Time { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("completed")]
    public bool Completed { get; set; }
}


// =======================================
// DB CONTEXT
// =======================================

public class TareasDbContext : DbContext
{
    public TareasDbContext(DbContextOptions<TareasDbContext> options)
        : base(options)
    {
    }

    public DbSet<Todo> Tareas => Set<Todo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Todo>().ToTable("Tareas");
    }
}
