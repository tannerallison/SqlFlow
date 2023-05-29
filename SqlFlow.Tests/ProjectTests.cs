using System.Text.Json;
using FluentAssertions;
using SqlFlow.Manager;

namespace SqlFlow.Tests;

public class ProjectTests
{
    [Fact]
    public void SerializingProject()
    {
        var project = new Project()
        {
            Name = "Test Project",
        };

        var serialize = Project.Serialize(project);

        var jsonDocument = JsonDocument.Parse(serialize);
        jsonDocument.RootElement.GetProperty("Name").GetString().Should().Be("Test Project");
    }

}
