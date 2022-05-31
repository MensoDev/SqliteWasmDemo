using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using TodoList.Domain.Repositories;
using TodoList.Infra.Data;
using TodoList.Infra.Data.Extensions;
using TodoList.Infra.Data.Repositories;
using TodoList.Presentation;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient {BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)});

builder.Services.AddMudServices();

builder.Services.AddBlazorWasmDatabaseContextFactory<TodoListDbContext>(options =>
    options.UseSqlite("Data Source=todolist.sqlite3"));

builder.Services.AddScoped<ITodoRepository, TodoRepository>();

await builder.Build().RunAsync();