using System.Text.Json;
using Kt17Todo.Models;
using Microsoft.JSInterop;

namespace Kt17Todo.Services;

public class TodoService(IJSRuntime js)
{
    private const string StorageKey = "kt17-todo-tasks";
    public List<TaskItem> Tasks { get; } = new();
    public bool IsLoaded { get; private set; }
    public string? StorageError { get; private set; }

    public async Task LoadAsync()
    {
        if (IsLoaded) return;
        try
        {
            var json = await js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
            var saved = string.IsNullOrEmpty(json) ? new List<TaskItem>() : JsonSerializer.Deserialize(json, TodoJsonContext.Default.ListTaskItem);
            if (saved is not null)
            {
                var ids = new HashSet<Guid>();
                foreach (var item in saved)
                {
                    if (item is null || string.IsNullOrWhiteSpace(item.Name)) continue;
                    if (item.Id == Guid.Empty || !ids.Add(item.Id)) item.Id = Guid.NewGuid();
                    Tasks.Add(item);
                }
            }
        }
        catch (JsonException)
        {
            StorageError = "Не удалось прочитать сохранённые задачи";
        }
        catch (JSException)
        {
            StorageError = "Не удалось открыть хранилище браузера";
        }
        IsLoaded = true;
    }

    public async Task AddAsync(string name)
    {
        Tasks.Add(new TaskItem { Name = ValidateName(name) });
        await SaveAsync();
    }

    public async Task RenameAsync(Guid id, string name)
    {
        var item = Tasks.FirstOrDefault(item => item.Id == id);
        if (item is null) return;
        item.Name = ValidateName(name);
        await SaveAsync();
    }

    public async Task SetCompletionAsync(Guid id, bool isComplete)
    {
        var item = Tasks.FirstOrDefault(item => item.Id == id);
        if (item is null) return;
        item.IsComplete = isComplete;
        await SaveAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        Tasks.RemoveAll(item => item.Id == id);
        await SaveAsync();
    }

    public async Task DeleteCompletedAsync()
    {
        Tasks.RemoveAll(item => item.IsComplete);
        await SaveAsync();
    }

    private async Task SaveAsync()
    {
        StorageError = null;
        try
        {
            await js.InvokeVoidAsync("localStorage.setItem", StorageKey, JsonSerializer.Serialize(Tasks, TodoJsonContext.Default.ListTaskItem));
        }
        catch (JSException)
        {
            StorageError = "Не удалось сохранить задачи, изменения доступны до закрытия страницы";
        }
    }

    private static string ValidateName(string name)
    {
        var value = name.Trim();
        if (value.Length is 0 or > 200) throw new ArgumentException("Название должно содержать от 1 до 200 символов");
        return value;
    }
}
