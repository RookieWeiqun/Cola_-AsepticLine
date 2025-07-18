using Cola.Extensions;
using Cola.Model;
using NLog.Web;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
    });
// Add CORS services
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
Func<IServiceProvider, IFreeSql> fsqlFactory = r =>
{
    // 从环境变量读取数据库配置
    var host = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "8.141.80.120";
    var port = Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "5432";
    var database = Environment.GetEnvironmentVariable("POSTGRES_DB") ?? "cola";
    var username = Environment.GetEnvironmentVariable("POSTGRES_USER") ?? "cwq";
    var password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "123456";
    
    // 构建连接字符串  
    var connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password}";
    
    IFreeSql fsql = new FreeSql.FreeSqlBuilder()
        .UseConnectionString(FreeSql.DataType.PostgreSQL, connectionString)
        .UseAdoConnectionPool(true)
        .UseMonitorCommand(cmd => Console.WriteLine($"Sql：{cmd.CommandText}"))
        .UseAutoSyncStructure(false) //自动同步实体结构到数据库，只有CRUD时才会生成表
        .Build();
    //fsql.CodeFirst.SyncStructure<CheckPara>();
    return fsql;
};

builder.Services.AddSingleton<IFreeSql>(fsqlFactory);
// ���� NLog ��־�ṩ����
builder.Logging.ClearProviders(); // ���Ĭ�ϵ���־�ṩ����
builder.Host.UseNLog(); // ʹ�� NLog
builder.Services.AddAutoMapper(typeof(AutoMapperConfig));
// �� AppConfig ���ýڵ���
builder.Services.Configure<AppConfig>(
    builder.Configuration.GetSection("AppConfig")
);
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthorization();

app.MapControllers();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

// 应用程序启动日志
logger.LogInformation("=== Cola服务启动于 {time} ===", DateTimeOffset.Now);

// 记录数据库连接信息
var dbHost = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "8.141.80.120";
var dbPort = Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "5432";
var dbName = Environment.GetEnvironmentVariable("POSTGRES_DB") ?? "cola";
var dbUser = Environment.GetEnvironmentVariable("POSTGRES_USER") ?? "cwq"; // 注意这里是cwq!

logger.LogInformation("数据库连接配置: Host={host}, Port={port}, Database={db}, User={user}",
    dbHost, dbPort, dbName, dbUser);

// 放在app.Run()之前
app.Lifetime.ApplicationStopping.Register(() => {
    logger.LogInformation("=== Cola服务正在关闭... ===");
});
app.Run();
