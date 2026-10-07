using System.Text.Json.Serialization;

namespace Kt17Todo.Models;

[JsonSerializable(typeof(List<TaskItem>))]
public partial class TodoJsonContext : JsonSerializerContext
{
}
