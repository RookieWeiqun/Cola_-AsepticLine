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
    IFreeSql fsql = new FreeSql.FreeSqlBuilder()
        .UseConnectionString(FreeSql.DataType.PostgreSQL,
         "Host=192.168.1.199;Port=5432;Database=cola;Username=postgres;Password=123456")
         //"Host=8.141.80.120;Port=5432;Database=test;Username=cwq;Password=338670caO@")
        .UseAdoConnectionPool(true)
        .UseMonitorCommand(cmd => Console.WriteLine($"Sql：{cmd.CommandText}"))
        .UseAutoSyncStructure(false) //自动同步实体结构到数据库，只有CRUD时才会生成表
        .Build();
    fsql.CodeFirst.SyncStructure<HisDataCheck>();
    return fsql;
};

builder.Services.AddSingleton<IFreeSql>(fsqlFactory);
// 添加 NLog 日志提供程序
builder.Logging.ClearProviders(); // 清除默认的日志提供程序
builder.Host.UseNLog(); // 使用 NLog
builder.Services.AddAutoMapper(typeof(AutoMapperConfig));
// 绑定 AppConfig 配置节到类
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

app.UseAuthorization();

app.MapControllers();

app.Run();
