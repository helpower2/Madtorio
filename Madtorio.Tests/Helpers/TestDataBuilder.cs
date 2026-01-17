using Madtorio.Data.Models;

namespace Madtorio.Tests.Helpers;

/// <summary>
/// Fluent builder for creating test data entities.
/// Provides sensible defaults while allowing customization.
/// </summary>
public static class TestDataBuilder
{
    /// <summary>
    /// Creates a SaveFile builder with default test data.
    /// </summary>
    public static SaveFileBuilder SaveFile() => new SaveFileBuilder();

    /// <summary>
    /// Creates a RuleCategory builder with default test data.
    /// </summary>
    public static RuleCategoryBuilder RuleCategory() => new RuleCategoryBuilder();

    /// <summary>
    /// Creates a Rule builder with default test data.
    /// </summary>
    public static RuleBuilder Rule() => new RuleBuilder();

    /// <summary>
    /// Creates a ServerConfig builder with default test data.
    /// </summary>
    public static ServerConfigBuilder ServerConfig() => new ServerConfigBuilder();
}

public class SaveFileBuilder
{
    private readonly SaveFile _saveFile;

    public SaveFileBuilder()
    {
        _saveFile = new SaveFile
        {
            FileName = "test-save.zip",
            Description = "Test save file",
            FilePath = "saves/test-save.zip",
            FileSize = 1024 * 1024, // 1 MB
            UploadDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            UploadedBy = "test-user"
        };
    }

    public SaveFileBuilder WithId(int id)
    {
        _saveFile.Id = id;
        return this;
    }

    public SaveFileBuilder WithFileName(string fileName)
    {
        _saveFile.FileName = fileName;
        return this;
    }

    public SaveFileBuilder WithDescription(string description)
    {
        _saveFile.Description = description;
        return this;
    }

    public SaveFileBuilder WithFilePath(string filePath)
    {
        _saveFile.FilePath = filePath;
        return this;
    }

    public SaveFileBuilder WithFileSize(long fileSize)
    {
        _saveFile.FileSize = fileSize;
        return this;
    }

    public SaveFileBuilder WithUploadDate(DateTime uploadDate)
    {
        _saveFile.UploadDate = uploadDate;
        return this;
    }

    public SaveFileBuilder WithModifiedDate(DateTime modifiedDate)
    {
        _saveFile.ModifiedDate = modifiedDate;
        return this;
    }

    public SaveFileBuilder WithUploadedBy(string uploadedBy)
    {
        _saveFile.UploadedBy = uploadedBy;
        return this;
    }

    public SaveFile Build() => _saveFile;
}

public class RuleCategoryBuilder
{
    private readonly RuleCategory _category;

    public RuleCategoryBuilder()
    {
        _category = new RuleCategory
        {
            Name = "Test Category",
            Description = "Test category description",
            DisplayOrder = 1,
            IsEnabled = true,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };
    }

    public RuleCategoryBuilder WithId(int id)
    {
        _category.Id = id;
        return this;
    }

    public RuleCategoryBuilder WithName(string name)
    {
        _category.Name = name;
        return this;
    }

    public RuleCategoryBuilder WithDescription(string? description)
    {
        _category.Description = description;
        return this;
    }

    public RuleCategoryBuilder WithDisplayOrder(int displayOrder)
    {
        _category.DisplayOrder = displayOrder;
        return this;
    }

    public RuleCategoryBuilder WithIsEnabled(bool isEnabled)
    {
        _category.IsEnabled = isEnabled;
        return this;
    }

    public RuleCategoryBuilder WithCreatedDate(DateTime createdDate)
    {
        _category.CreatedDate = createdDate;
        return this;
    }

    public RuleCategoryBuilder WithModifiedDate(DateTime modifiedDate)
    {
        _category.ModifiedDate = modifiedDate;
        return this;
    }

    public RuleCategory Build() => _category;
}

public class RuleBuilder
{
    private readonly Rule _rule;

    public RuleBuilder()
    {
        _rule = new Rule
        {
            CategoryId = 1,
            Content = "Test rule content",
            DetailedDescription = null,
            DisplayOrder = 1,
            IsEnabled = true,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };
    }

    public RuleBuilder WithId(int id)
    {
        _rule.Id = id;
        return this;
    }

    public RuleBuilder WithCategoryId(int categoryId)
    {
        _rule.CategoryId = categoryId;
        return this;
    }

    public RuleBuilder WithContent(string content)
    {
        _rule.Content = content;
        return this;
    }

    public RuleBuilder WithDetailedDescription(string? detailedDescription)
    {
        _rule.DetailedDescription = detailedDescription;
        return this;
    }

    public RuleBuilder WithDisplayOrder(int displayOrder)
    {
        _rule.DisplayOrder = displayOrder;
        return this;
    }

    public RuleBuilder WithIsEnabled(bool isEnabled)
    {
        _rule.IsEnabled = isEnabled;
        return this;
    }

    public RuleBuilder WithCreatedDate(DateTime createdDate)
    {
        _rule.CreatedDate = createdDate;
        return this;
    }

    public RuleBuilder WithModifiedDate(DateTime modifiedDate)
    {
        _rule.ModifiedDate = modifiedDate;
        return this;
    }

    public RuleBuilder WithCategory(RuleCategory category)
    {
        _rule.Category = category;
        _rule.CategoryId = category.Id;
        return this;
    }

    public Rule Build() => _rule;
}

public class ServerConfigBuilder
{
    private readonly ServerConfig _config;

    public ServerConfigBuilder()
    {
        _config = new ServerConfig
        {
            Key = "TestKey",
            Value = "TestValue",
            Description = "Test configuration",
            ModifiedDate = DateTime.UtcNow,
            ModifiedBy = "test-user"
        };
    }

    public ServerConfigBuilder WithId(int id)
    {
        _config.Id = id;
        return this;
    }

    public ServerConfigBuilder WithKey(string key)
    {
        _config.Key = key;
        return this;
    }

    public ServerConfigBuilder WithValue(string value)
    {
        _config.Value = value;
        return this;
    }

    public ServerConfigBuilder WithDescription(string? description)
    {
        _config.Description = description;
        return this;
    }

    public ServerConfigBuilder WithModifiedDate(DateTime modifiedDate)
    {
        _config.ModifiedDate = modifiedDate;
        return this;
    }

    public ServerConfigBuilder WithModifiedBy(string? modifiedBy)
    {
        _config.ModifiedBy = modifiedBy;
        return this;
    }

    public ServerConfig Build() => _config;
}
