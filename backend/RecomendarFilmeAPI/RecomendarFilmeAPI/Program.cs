using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.ML;
using RecomendarFilmeAPI.Data;
using RecomendarFilmeAPI.Models;
using RecomendarFilmeAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Configurar Banco de Dados (MySQL)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<MovieContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// 2. Configurar ML.NET PredictionEnginePool
// Isso permite carregar o modelo e fazer previsões rápidas sem recarregar do disco a cada request.
builder.Services.AddPredictionEnginePool<HybridMovieRating, MovieRatingPrediction>()
    .FromFile(modelName: "MovieRecommenderModel", filePath: builder.Configuration["MLModelPath"], watchForChanges: true);

// 3. Registrar Serviço de Treinamento
builder.Services.AddScoped<IModelTrainingService, ModelTrainingService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 4. Pipeline de Requisições HTTP 

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();

// Mapeia os Controllers
app.MapControllers();

app.Run();