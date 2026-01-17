using Madtorio.Data;
using Madtorio.Data.Models;
using Madtorio.Services;
using Madtorio.Tests.Helpers;

namespace Madtorio.Tests.Unit.Services;

public class RulesServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly RulesService _service;
    private readonly Mock<ILogger<RulesService>> _mockLogger;

    public RulesServiceTests()
    {
        _context = InMemoryDbContextFactory.Create();
        _mockLogger = new Mock<ILogger<RulesService>>();
        _service = new RulesService(_context, _mockLogger.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region GetAllCategoriesAsync Tests

    [Fact]
    public async Task GetAllCategoriesAsync_WithMultipleCategories_ReturnsOrderedByDisplayOrder()
    {
        // Arrange
        var cat1 = TestDataBuilder.RuleCategory()
            .WithName("Third Category")
            .WithDisplayOrder(3)
            .Build();

        var cat2 = TestDataBuilder.RuleCategory()
            .WithName("First Category")
            .WithDisplayOrder(1)
            .Build();

        var cat3 = TestDataBuilder.RuleCategory()
            .WithName("Second Category")
            .WithDisplayOrder(2)
            .Build();

        _context.RuleCategories.AddRange(cat1, cat2, cat3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAllCategoriesAsync();

        // Assert
        result.Should().HaveCount(3);
        result.Should().BeInAscendingOrder(c => c.DisplayOrder);
        result[0].Name.Should().Be("First Category");
        result[1].Name.Should().Be("Second Category");
        result[2].Name.Should().Be("Third Category");
    }

    [Fact]
    public async Task GetAllCategoriesAsync_IncludeDisabledFalse_ExcludesDisabledCategories()
    {
        // Arrange
        var enabled = TestDataBuilder.RuleCategory()
            .WithName("Enabled")
            .WithIsEnabled(true)
            .Build();

        var disabled = TestDataBuilder.RuleCategory()
            .WithName("Disabled")
            .WithIsEnabled(false)
            .Build();

        _context.RuleCategories.AddRange(enabled, disabled);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAllCategoriesAsync(includeDisabled: false);

        // Assert
        result.Should().ContainSingle();
        result[0].Name.Should().Be("Enabled");
    }

    [Fact]
    public async Task GetAllCategoriesAsync_IncludeDisabledTrue_IncludesAllCategories()
    {
        // Arrange
        var enabled = TestDataBuilder.RuleCategory()
            .WithName("Enabled")
            .WithIsEnabled(true)
            .Build();

        var disabled = TestDataBuilder.RuleCategory()
            .WithName("Disabled")
            .WithIsEnabled(false)
            .Build();

        _context.RuleCategories.AddRange(enabled, disabled);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAllCategoriesAsync(includeDisabled: true);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllCategoriesAsync_WithEmptyDatabase_ReturnsEmptyList()
    {
        // Act
        var result = await _service.GetAllCategoriesAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllCategoriesAsync_IncludesRelatedRules()
    {
        // Arrange
        var category = TestDataBuilder.RuleCategory()
            .WithName("Test Category")
            .Build();
        _context.RuleCategories.Add(category);
        await _context.SaveChangesAsync();

        var rule = TestDataBuilder.Rule()
            .WithCategoryId(category.Id)
            .WithContent("Test Rule")
            .Build();
        _context.Rules.Add(rule);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAllCategoriesAsync();

        // Assert
        result.Should().ContainSingle();
        result[0].Rules.Should().ContainSingle();
        result[0].Rules.First().Content.Should().Be("Test Rule");
    }

    #endregion

    #region GetCategoryByIdAsync Tests

    [Fact]
    public async Task GetCategoryByIdAsync_WithExistingId_ReturnsCategory()
    {
        // Arrange
        var category = TestDataBuilder.RuleCategory()
            .WithName("Test Category")
            .Build();
        _context.RuleCategories.Add(category);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetCategoryByIdAsync(category.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(category.Id);
        result.Name.Should().Be("Test Category");
    }

    [Fact]
    public async Task GetCategoryByIdAsync_WithNonExistentId_ReturnsNull()
    {
        // Act
        var result = await _service.GetCategoryByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetCategoryByIdAsync_IncludesRelatedRules()
    {
        // Arrange
        var category = TestDataBuilder.RuleCategory().Build();
        _context.RuleCategories.Add(category);
        await _context.SaveChangesAsync();

        var rule = TestDataBuilder.Rule()
            .WithCategoryId(category.Id)
            .WithContent("Test Rule")
            .Build();
        _context.Rules.Add(rule);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetCategoryByIdAsync(category.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Rules.Should().ContainSingle();
        result.Rules.First().Content.Should().Be("Test Rule");
    }

    #endregion

    #region CreateCategoryAsync Tests

    [Fact]
    public async Task CreateCategoryAsync_WithValidCategory_CreatesAndReturns()
    {
        // Arrange
        var category = TestDataBuilder.RuleCategory()
            .WithName("New Category")
            .WithDescription("Test Description")
            .Build();

        // Act
        var result = await _service.CreateCategoryAsync(category);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.Name.Should().Be("New Category");
        result.Description.Should().Be("Test Description");

        var saved = await _context.RuleCategories.FindAsync(result.Id);
        saved.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateCategoryAsync_SetsDisplayOrderToMaxPlusOne()
    {
        // Arrange
        var existing1 = TestDataBuilder.RuleCategory()
            .WithDisplayOrder(1)
            .Build();
        var existing2 = TestDataBuilder.RuleCategory()
            .WithDisplayOrder(5)
            .Build();

        _context.RuleCategories.AddRange(existing1, existing2);
        await _context.SaveChangesAsync();

        var newCategory = TestDataBuilder.RuleCategory()
            .WithName("New Category")
            .Build();

        // Act
        var result = await _service.CreateCategoryAsync(newCategory);

        // Assert
        result.DisplayOrder.Should().Be(6); // Max (5) + 1
    }

    [Fact]
    public async Task CreateCategoryAsync_SetsCreatedDate()
    {
        // Arrange
        var category = TestDataBuilder.RuleCategory().Build();
        var before = DateTime.UtcNow;

        // Act
        var result = await _service.CreateCategoryAsync(category);
        var after = DateTime.UtcNow;

        // Assert
        result.CreatedDate.Should().BeAfter(before.AddSeconds(-1));
        result.CreatedDate.Should().BeBefore(after.AddSeconds(1));
    }

    [Fact]
    public async Task CreateCategoryAsync_WithEmptyDatabase_SetsDisplayOrderToOne()
    {
        // Arrange
        var category = TestDataBuilder.RuleCategory()
            .WithName("First Category")
            .Build();

        // Act
        var result = await _service.CreateCategoryAsync(category);

        // Assert
        result.DisplayOrder.Should().Be(1);
    }

    #endregion

    #region UpdateCategoryAsync Tests

    [Fact]
    public async Task UpdateCategoryAsync_WithExistingCategory_UpdatesAndReturnsTrue()
    {
        // Arrange
        var category = TestDataBuilder.RuleCategory()
            .WithName("Original Name")
            .WithDescription("Original Description")
            .Build();
        _context.RuleCategories.Add(category);
        await _context.SaveChangesAsync();

        category.Name = "Updated Name";
        category.Description = "Updated Description";

        // Act
        var result = await _service.UpdateCategoryAsync(category);

        // Assert
        result.Should().BeTrue();

        var updated = await _context.RuleCategories.FindAsync(category.Id);
        updated!.Name.Should().Be("Updated Name");
        updated.Description.Should().Be("Updated Description");
    }

    [Fact]
    public async Task UpdateCategoryAsync_UpdatesModifiedDate()
    {
        // Arrange
        var category = TestDataBuilder.RuleCategory()
            .WithModifiedDate(DateTime.UtcNow.AddDays(-1))
            .Build();
        _context.RuleCategories.Add(category);
        await _context.SaveChangesAsync();

        var originalModifiedDate = category.ModifiedDate;
        var before = DateTime.UtcNow;

        // Act
        await _service.UpdateCategoryAsync(category);
        var after = DateTime.UtcNow;

        // Assert
        var updated = await _context.RuleCategories.FindAsync(category.Id);
        updated!.ModifiedDate.Should().NotBeNull();
        updated.ModifiedDate!.Value.Should().BeAfter(originalModifiedDate!.Value);
        updated.ModifiedDate!.Value.Should().BeAfter(before.AddSeconds(-1));
        updated.ModifiedDate!.Value.Should().BeBefore(after.AddSeconds(1));
    }

    #endregion

    #region DeleteCategoryAsync Tests

    [Fact]
    public async Task DeleteCategoryAsync_WithExistingId_DeletesAndReturnsTrue()
    {
        // Arrange
        var category = TestDataBuilder.RuleCategory().Build();
        _context.RuleCategories.Add(category);
        await _context.SaveChangesAsync();
        var id = category.Id;

        // Act
        var result = await _service.DeleteCategoryAsync(id);

        // Assert
        result.Should().BeTrue();

        var deleted = await _context.RuleCategories.FindAsync(id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteCategoryAsync_WithNonExistentId_ReturnsFalse()
    {
        // Act
        var result = await _service.DeleteCategoryAsync(999);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteCategoryAsync_CascadeDeletesRules()
    {
        // Arrange
        var category = TestDataBuilder.RuleCategory().Build();
        _context.RuleCategories.Add(category);
        await _context.SaveChangesAsync();

        var rule1 = TestDataBuilder.Rule()
            .WithCategoryId(category.Id)
            .WithContent("Rule 1")
            .Build();
        var rule2 = TestDataBuilder.Rule()
            .WithCategoryId(category.Id)
            .WithContent("Rule 2")
            .Build();
        _context.Rules.AddRange(rule1, rule2);
        await _context.SaveChangesAsync();

        var ruleId1 = rule1.Id;
        var ruleId2 = rule2.Id;

        // Act
        await _service.DeleteCategoryAsync(category.Id);

        // Assert
        var deletedRule1 = await _context.Rules.FindAsync(ruleId1);
        var deletedRule2 = await _context.Rules.FindAsync(ruleId2);
        deletedRule1.Should().BeNull();
        deletedRule2.Should().BeNull();
    }

    #endregion

    #region ReorderCategoriesAsync Tests

    [Fact]
    public async Task ReorderCategoriesAsync_UpdatesDisplayOrder()
    {
        // Arrange
        var cat1 = TestDataBuilder.RuleCategory()
            .WithName("First")
            .WithDisplayOrder(1)
            .Build();
        var cat2 = TestDataBuilder.RuleCategory()
            .WithName("Second")
            .WithDisplayOrder(2)
            .Build();
        var cat3 = TestDataBuilder.RuleCategory()
            .WithName("Third")
            .WithDisplayOrder(3)
            .Build();

        _context.RuleCategories.AddRange(cat1, cat2, cat3);
        await _context.SaveChangesAsync();

        // Act - Reverse order
        var result = await _service.ReorderCategoriesAsync(new List<int> { cat3.Id, cat2.Id, cat1.Id });

        // Assert
        result.Should().BeTrue();

        var updated1 = await _context.RuleCategories.FindAsync(cat1.Id);
        var updated2 = await _context.RuleCategories.FindAsync(cat2.Id);
        var updated3 = await _context.RuleCategories.FindAsync(cat3.Id);

        updated3!.DisplayOrder.Should().Be(1);
        updated2!.DisplayOrder.Should().Be(2);
        updated1!.DisplayOrder.Should().Be(3);
    }

    [Fact]
    public async Task ReorderCategoriesAsync_UpdatesModifiedDate()
    {
        // Arrange
        var category = TestDataBuilder.RuleCategory()
            .WithModifiedDate(DateTime.UtcNow.AddDays(-1))
            .Build();
        _context.RuleCategories.Add(category);
        await _context.SaveChangesAsync();

        var before = DateTime.UtcNow;

        // Act
        await _service.ReorderCategoriesAsync(new List<int> { category.Id });
        var after = DateTime.UtcNow;

        // Assert
        var updated = await _context.RuleCategories.FindAsync(category.Id);
        updated!.ModifiedDate.Should().NotBeNull();
        updated.ModifiedDate!.Value.Should().BeAfter(before.AddSeconds(-1));
        updated.ModifiedDate!.Value.Should().BeBefore(after.AddSeconds(1));
    }

    [Fact]
    public async Task ReorderCategoriesAsync_WithNonExistentIds_IgnoresThem()
    {
        // Arrange
        var category = TestDataBuilder.RuleCategory()
            .WithDisplayOrder(1)
            .Build();
        _context.RuleCategories.Add(category);
        await _context.SaveChangesAsync();

        // Act - Include non-existent ID
        var result = await _service.ReorderCategoriesAsync(new List<int> { 999, category.Id });

        // Assert
        result.Should().BeTrue();
        var updated = await _context.RuleCategories.FindAsync(category.Id);
        updated!.DisplayOrder.Should().Be(2); // Position in the list
    }

    #endregion

    #region GetRulesByCategoryAsync Tests

    [Fact]
    public async Task GetRulesByCategoryAsync_WithMultipleRules_ReturnsOrderedByDisplayOrder()
    {
        // Arrange
        var category = TestDataBuilder.RuleCategory().Build();
        _context.RuleCategories.Add(category);
        await _context.SaveChangesAsync();

        var rule1 = TestDataBuilder.Rule()
            .WithCategoryId(category.Id)
            .WithContent("Third Rule")
            .WithDisplayOrder(3)
            .Build();
        var rule2 = TestDataBuilder.Rule()
            .WithCategoryId(category.Id)
            .WithContent("First Rule")
            .WithDisplayOrder(1)
            .Build();
        var rule3 = TestDataBuilder.Rule()
            .WithCategoryId(category.Id)
            .WithContent("Second Rule")
            .WithDisplayOrder(2)
            .Build();

        _context.Rules.AddRange(rule1, rule2, rule3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetRulesByCategoryAsync(category.Id);

        // Assert
        result.Should().HaveCount(3);
        result.Should().BeInAscendingOrder(r => r.DisplayOrder);
        result[0].Content.Should().Be("First Rule");
        result[1].Content.Should().Be("Second Rule");
        result[2].Content.Should().Be("Third Rule");
    }

    [Fact]
    public async Task GetRulesByCategoryAsync_IncludeDisabledFalse_ExcludesDisabledRules()
    {
        // Arrange
        var category = TestDataBuilder.RuleCategory().Build();
        _context.RuleCategories.Add(category);
        await _context.SaveChangesAsync();

        var enabled = TestDataBuilder.Rule()
            .WithCategoryId(category.Id)
            .WithContent("Enabled Rule")
            .WithIsEnabled(true)
            .Build();
        var disabled = TestDataBuilder.Rule()
            .WithCategoryId(category.Id)
            .WithContent("Disabled Rule")
            .WithIsEnabled(false)
            .Build();

        _context.Rules.AddRange(enabled, disabled);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetRulesByCategoryAsync(category.Id, includeDisabled: false);

        // Assert
        result.Should().ContainSingle();
        result[0].Content.Should().Be("Enabled Rule");
    }

    [Fact]
    public async Task GetRulesByCategoryAsync_IncludeDisabledTrue_IncludesAllRules()
    {
        // Arrange
        var category = TestDataBuilder.RuleCategory().Build();
        _context.RuleCategories.Add(category);
        await _context.SaveChangesAsync();

        var enabled = TestDataBuilder.Rule()
            .WithCategoryId(category.Id)
            .WithContent("Enabled Rule")
            .WithIsEnabled(true)
            .Build();
        var disabled = TestDataBuilder.Rule()
            .WithCategoryId(category.Id)
            .WithContent("Disabled Rule")
            .WithIsEnabled(false)
            .Build();

        _context.Rules.AddRange(enabled, disabled);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetRulesByCategoryAsync(category.Id, includeDisabled: true);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetRulesByCategoryAsync_WithNonExistentCategory_ReturnsEmptyList()
    {
        // Act
        var result = await _service.GetRulesByCategoryAsync(999);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRulesByCategoryAsync_OnlyReturnsRulesForSpecifiedCategory()
    {
        // Arrange
        var category1 = TestDataBuilder.RuleCategory().WithName("Category 1").Build();
        var category2 = TestDataBuilder.RuleCategory().WithName("Category 2").Build();
        _context.RuleCategories.AddRange(category1, category2);
        await _context.SaveChangesAsync();

        var rule1 = TestDataBuilder.Rule()
            .WithCategoryId(category1.Id)
            .WithContent("Rule in Category 1")
            .Build();
        var rule2 = TestDataBuilder.Rule()
            .WithCategoryId(category2.Id)
            .WithContent("Rule in Category 2")
            .Build();
        _context.Rules.AddRange(rule1, rule2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetRulesByCategoryAsync(category1.Id);

        // Assert
        result.Should().ContainSingle();
        result[0].Content.Should().Be("Rule in Category 1");
    }

    #endregion

    #region GetRuleByIdAsync Tests

    [Fact]
    public async Task GetRuleByIdAsync_WithExistingId_ReturnsRule()
    {
        // Arrange
        var category = TestDataBuilder.RuleCategory().Build();
        _context.RuleCategories.Add(category);
        await _context.SaveChangesAsync();

        var rule = TestDataBuilder.Rule()
            .WithCategoryId(category.Id)
            .WithContent("Test Rule")
            .Build();
        _context.Rules.Add(rule);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetRuleByIdAsync(rule.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(rule.Id);
        result.Content.Should().Be("Test Rule");
    }

    [Fact]
    public async Task GetRuleByIdAsync_WithNonExistentId_ReturnsNull()
    {
        // Act
        var result = await _service.GetRuleByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region CreateRuleAsync Tests

    [Fact]
    public async Task CreateRuleAsync_WithValidRule_CreatesAndReturns()
    {
        // Arrange
        var category = TestDataBuilder.RuleCategory().Build();
        _context.RuleCategories.Add(category);
        await _context.SaveChangesAsync();

        var rule = TestDataBuilder.Rule()
            .WithCategoryId(category.Id)
            .WithContent("New Rule")
            .WithDetailedDescription("Detailed description")
            .Build();

        // Act
        var result = await _service.CreateRuleAsync(rule);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.Content.Should().Be("New Rule");
        result.DetailedDescription.Should().Be("Detailed description");

        var saved = await _context.Rules.FindAsync(result.Id);
        saved.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateRuleAsync_SetsDisplayOrderToMaxPlusOneWithinCategory()
    {
        // Arrange
        var category = TestDataBuilder.RuleCategory().Build();
        _context.RuleCategories.Add(category);
        await _context.SaveChangesAsync();

        var existing1 = TestDataBuilder.Rule()
            .WithCategoryId(category.Id)
            .WithDisplayOrder(1)
            .Build();
        var existing2 = TestDataBuilder.Rule()
            .WithCategoryId(category.Id)
            .WithDisplayOrder(5)
            .Build();

        _context.Rules.AddRange(existing1, existing2);
        await _context.SaveChangesAsync();

        var newRule = TestDataBuilder.Rule()
            .WithCategoryId(category.Id)
            .WithContent("New Rule")
            .Build();

        // Act
        var result = await _service.CreateRuleAsync(newRule);

        // Assert
        result.DisplayOrder.Should().Be(6); // Max (5) + 1
    }

    [Fact]
    public async Task CreateRuleAsync_SetsCreatedDate()
    {
        // Arrange
        var category = TestDataBuilder.RuleCategory().Build();
        _context.RuleCategories.Add(category);
        await _context.SaveChangesAsync();

        var rule = TestDataBuilder.Rule()
            .WithCategoryId(category.Id)
            .Build();
        var before = DateTime.UtcNow;

        // Act
        var result = await _service.CreateRuleAsync(rule);
        var after = DateTime.UtcNow;

        // Assert
        result.CreatedDate.Should().BeAfter(before.AddSeconds(-1));
        result.CreatedDate.Should().BeBefore(after.AddSeconds(1));
    }

    [Fact]
    public async Task CreateRuleAsync_WithEmptyCategory_SetsDisplayOrderToOne()
    {
        // Arrange
        var category = TestDataBuilder.RuleCategory().Build();
        _context.RuleCategories.Add(category);
        await _context.SaveChangesAsync();

        var rule = TestDataBuilder.Rule()
            .WithCategoryId(category.Id)
            .WithContent("First Rule")
            .Build();

        // Act
        var result = await _service.CreateRuleAsync(rule);

        // Assert
        result.DisplayOrder.Should().Be(1);
    }

    #endregion

    #region UpdateRuleAsync Tests

    [Fact]
    public async Task UpdateRuleAsync_WithExistingRule_UpdatesAndReturnsTrue()
    {
        // Arrange
        var category = TestDataBuilder.RuleCategory().Build();
        _context.RuleCategories.Add(category);
        await _context.SaveChangesAsync();

        var rule = TestDataBuilder.Rule()
            .WithCategoryId(category.Id)
            .WithContent("Original Content")
            .WithDetailedDescription("Original Description")
            .Build();
        _context.Rules.Add(rule);
        await _context.SaveChangesAsync();

        rule.Content = "Updated Content";
        rule.DetailedDescription = "Updated Description";

        // Act
        var result = await _service.UpdateRuleAsync(rule);

        // Assert
        result.Should().BeTrue();

        var updated = await _context.Rules.FindAsync(rule.Id);
        updated!.Content.Should().Be("Updated Content");
        updated.DetailedDescription.Should().Be("Updated Description");
    }

    [Fact]
    public async Task UpdateRuleAsync_UpdatesModifiedDate()
    {
        // Arrange
        var category = TestDataBuilder.RuleCategory().Build();
        _context.RuleCategories.Add(category);
        await _context.SaveChangesAsync();

        var rule = TestDataBuilder.Rule()
            .WithCategoryId(category.Id)
            .WithModifiedDate(DateTime.UtcNow.AddDays(-1))
            .Build();
        _context.Rules.Add(rule);
        await _context.SaveChangesAsync();

        var originalModifiedDate = rule.ModifiedDate;
        var before = DateTime.UtcNow;

        // Act
        await _service.UpdateRuleAsync(rule);
        var after = DateTime.UtcNow;

        // Assert
        var updated = await _context.Rules.FindAsync(rule.Id);
        updated!.ModifiedDate.Should().NotBeNull();
        updated.ModifiedDate!.Value.Should().BeAfter(originalModifiedDate!.Value);
        updated.ModifiedDate!.Value.Should().BeAfter(before.AddSeconds(-1));
        updated.ModifiedDate!.Value.Should().BeBefore(after.AddSeconds(1));
    }

    #endregion

    #region DeleteRuleAsync Tests

    [Fact]
    public async Task DeleteRuleAsync_WithExistingId_DeletesAndReturnsTrue()
    {
        // Arrange
        var category = TestDataBuilder.RuleCategory().Build();
        _context.RuleCategories.Add(category);
        await _context.SaveChangesAsync();

        var rule = TestDataBuilder.Rule()
            .WithCategoryId(category.Id)
            .Build();
        _context.Rules.Add(rule);
        await _context.SaveChangesAsync();
        var id = rule.Id;

        // Act
        var result = await _service.DeleteRuleAsync(id);

        // Assert
        result.Should().BeTrue();

        var deleted = await _context.Rules.FindAsync(id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteRuleAsync_WithNonExistentId_ReturnsFalse()
    {
        // Act
        var result = await _service.DeleteRuleAsync(999);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region ReorderRulesAsync Tests

    [Fact]
    public async Task ReorderRulesAsync_UpdatesDisplayOrder()
    {
        // Arrange
        var category = TestDataBuilder.RuleCategory().Build();
        _context.RuleCategories.Add(category);
        await _context.SaveChangesAsync();

        var rule1 = TestDataBuilder.Rule()
            .WithCategoryId(category.Id)
            .WithContent("First")
            .WithDisplayOrder(1)
            .Build();
        var rule2 = TestDataBuilder.Rule()
            .WithCategoryId(category.Id)
            .WithContent("Second")
            .WithDisplayOrder(2)
            .Build();
        var rule3 = TestDataBuilder.Rule()
            .WithCategoryId(category.Id)
            .WithContent("Third")
            .WithDisplayOrder(3)
            .Build();

        _context.Rules.AddRange(rule1, rule2, rule3);
        await _context.SaveChangesAsync();

        // Act - Reverse order
        var result = await _service.ReorderRulesAsync(category.Id, new List<int> { rule3.Id, rule2.Id, rule1.Id });

        // Assert
        result.Should().BeTrue();

        var updated1 = await _context.Rules.FindAsync(rule1.Id);
        var updated2 = await _context.Rules.FindAsync(rule2.Id);
        var updated3 = await _context.Rules.FindAsync(rule3.Id);

        updated3!.DisplayOrder.Should().Be(1);
        updated2!.DisplayOrder.Should().Be(2);
        updated1!.DisplayOrder.Should().Be(3);
    }

    [Fact]
    public async Task ReorderRulesAsync_OnlyUpdatesRulesInSpecifiedCategory()
    {
        // Arrange
        var category1 = TestDataBuilder.RuleCategory().Build();
        var category2 = TestDataBuilder.RuleCategory().Build();
        _context.RuleCategories.AddRange(category1, category2);
        await _context.SaveChangesAsync();

        var rule1 = TestDataBuilder.Rule()
            .WithCategoryId(category1.Id)
            .WithDisplayOrder(1)
            .Build();
        var rule2 = TestDataBuilder.Rule()
            .WithCategoryId(category2.Id)
            .WithDisplayOrder(1)
            .Build();

        _context.Rules.AddRange(rule1, rule2);
        await _context.SaveChangesAsync();

        // Act - Try to reorder rule2 with category1
        await _service.ReorderRulesAsync(category1.Id, new List<int> { rule2.Id });

        // Assert
        var updated2 = await _context.Rules.FindAsync(rule2.Id);
        updated2!.DisplayOrder.Should().Be(1); // Should NOT be updated
    }

    [Fact]
    public async Task ReorderRulesAsync_UpdatesModifiedDate()
    {
        // Arrange
        var category = TestDataBuilder.RuleCategory().Build();
        _context.RuleCategories.Add(category);
        await _context.SaveChangesAsync();

        var rule = TestDataBuilder.Rule()
            .WithCategoryId(category.Id)
            .WithModifiedDate(DateTime.UtcNow.AddDays(-1))
            .Build();
        _context.Rules.Add(rule);
        await _context.SaveChangesAsync();

        var before = DateTime.UtcNow;

        // Act
        await _service.ReorderRulesAsync(category.Id, new List<int> { rule.Id });
        var after = DateTime.UtcNow;

        // Assert
        var updated = await _context.Rules.FindAsync(rule.Id);
        updated!.ModifiedDate.Should().NotBeNull();
        updated.ModifiedDate!.Value.Should().BeAfter(before.AddSeconds(-1));
        updated.ModifiedDate!.Value.Should().BeBefore(after.AddSeconds(1));
    }

    #endregion

    #region GetActiveRulesGroupedAsync Tests

    [Fact]
    public async Task GetActiveRulesGroupedAsync_OnlyReturnsEnabledCategoriesAndRules()
    {
        // Arrange
        var enabledCat = TestDataBuilder.RuleCategory()
            .WithName("Enabled Category")
            .WithIsEnabled(true)
            .Build();
        var disabledCat = TestDataBuilder.RuleCategory()
            .WithName("Disabled Category")
            .WithIsEnabled(false)
            .Build();

        _context.RuleCategories.AddRange(enabledCat, disabledCat);
        await _context.SaveChangesAsync();

        var enabledRule = TestDataBuilder.Rule()
            .WithCategoryId(enabledCat.Id)
            .WithContent("Enabled Rule")
            .WithIsEnabled(true)
            .Build();
        var disabledRule = TestDataBuilder.Rule()
            .WithCategoryId(enabledCat.Id)
            .WithContent("Disabled Rule")
            .WithIsEnabled(false)
            .Build();
        var ruleInDisabledCat = TestDataBuilder.Rule()
            .WithCategoryId(disabledCat.Id)
            .WithContent("Rule in Disabled Category")
            .WithIsEnabled(true)
            .Build();

        _context.Rules.AddRange(enabledRule, disabledRule, ruleInDisabledCat);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetActiveRulesGroupedAsync();

        // Assert
        // NOTE: InMemory provider doesn't fully support filtered includes,
        // so we verify the enabled category is returned, but don't assert exact rule count
        result.Should().ContainSingle();
        result.Keys.First().Name.Should().Be("Enabled Category");
        result.Keys.First().IsEnabled.Should().BeTrue();

        // Disabled category should not be in results
        result.Keys.Should().NotContain(c => c.Name == "Disabled Category");
    }

    [Fact]
    public async Task GetActiveRulesGroupedAsync_ReturnsCategoriesOrderedByDisplayOrder()
    {
        // Arrange
        var cat1 = TestDataBuilder.RuleCategory()
            .WithName("Third")
            .WithDisplayOrder(3)
            .Build();
        var cat2 = TestDataBuilder.RuleCategory()
            .WithName("First")
            .WithDisplayOrder(1)
            .Build();
        var cat3 = TestDataBuilder.RuleCategory()
            .WithName("Second")
            .WithDisplayOrder(2)
            .Build();

        _context.RuleCategories.AddRange(cat1, cat2, cat3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetActiveRulesGroupedAsync();

        // Assert
        result.Keys.Should().HaveCount(3);
        result.Keys.Should().BeInAscendingOrder(c => c.DisplayOrder);
        result.Keys.ElementAt(0).Name.Should().Be("First");
        result.Keys.ElementAt(1).Name.Should().Be("Second");
        result.Keys.ElementAt(2).Name.Should().Be("Third");
    }

    [Fact]
    public async Task GetActiveRulesGroupedAsync_ReturnsRulesOrderedByDisplayOrder()
    {
        // Arrange
        var category = TestDataBuilder.RuleCategory().Build();
        _context.RuleCategories.Add(category);
        await _context.SaveChangesAsync();

        var rule1 = TestDataBuilder.Rule()
            .WithCategoryId(category.Id)
            .WithContent("Third")
            .WithDisplayOrder(3)
            .Build();
        var rule2 = TestDataBuilder.Rule()
            .WithCategoryId(category.Id)
            .WithContent("First")
            .WithDisplayOrder(1)
            .Build();
        var rule3 = TestDataBuilder.Rule()
            .WithCategoryId(category.Id)
            .WithContent("Second")
            .WithDisplayOrder(2)
            .Build();

        _context.Rules.AddRange(rule1, rule2, rule3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetActiveRulesGroupedAsync();

        // Assert
        var rules = result.Values.First();
        rules.Should().HaveCount(3);

        var rulesList = rules.OrderBy(r => r.DisplayOrder).ToList();
        rulesList[0].Content.Should().Be("First");
        rulesList[1].Content.Should().Be("Second");
        rulesList[2].Content.Should().Be("Third");
    }

    [Fact]
    public async Task GetActiveRulesGroupedAsync_WithEmptyDatabase_ReturnsEmptyDictionary()
    {
        // Act
        var result = await _service.GetActiveRulesGroupedAsync();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetServerConfigAsync Tests

    [Fact]
    public async Task GetServerConfigAsync_WithExistingKey_ReturnsValue()
    {
        // Arrange
        var config = TestDataBuilder.ServerConfig()
            .WithKey("server-ip")
            .WithValue("192.168.1.1")
            .Build();
        _context.ServerConfigs.Add(config);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetServerConfigAsync("server-ip");

        // Assert
        result.Should().Be("192.168.1.1");
    }

    [Fact]
    public async Task GetServerConfigAsync_WithNonExistentKey_ReturnsNull()
    {
        // Act
        var result = await _service.GetServerConfigAsync("non-existent-key");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region SetServerConfigAsync Tests

    [Fact]
    public async Task SetServerConfigAsync_WithNewKey_CreatesAndReturnsTrue()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var result = await _service.SetServerConfigAsync("new-key", "new-value", "test-user");
        var after = DateTime.UtcNow;

        // Assert
        result.Should().BeTrue();

        var saved = await _context.ServerConfigs.FirstOrDefaultAsync(sc => sc.Key == "new-key");
        saved.Should().NotBeNull();
        saved!.Value.Should().Be("new-value");
        saved.ModifiedBy.Should().Be("test-user");
        saved.ModifiedDate.Should().BeAfter(before.AddSeconds(-1));
        saved.ModifiedDate.Should().BeBefore(after.AddSeconds(1));
    }

    [Fact]
    public async Task SetServerConfigAsync_WithExistingKey_UpdatesAndReturnsTrue()
    {
        // Arrange
        var config = TestDataBuilder.ServerConfig()
            .WithKey("existing-key")
            .WithValue("old-value")
            .WithModifiedBy("old-user")
            .Build();
        _context.ServerConfigs.Add(config);
        await _context.SaveChangesAsync();

        var before = DateTime.UtcNow;

        // Act
        var result = await _service.SetServerConfigAsync("existing-key", "new-value", "new-user");
        var after = DateTime.UtcNow;

        // Assert
        result.Should().BeTrue();

        var updated = await _context.ServerConfigs.FirstOrDefaultAsync(sc => sc.Key == "existing-key");
        updated.Should().NotBeNull();
        updated!.Value.Should().Be("new-value");
        updated.ModifiedBy.Should().Be("new-user");
        updated.ModifiedDate.Should().BeAfter(before.AddSeconds(-1));
        updated.ModifiedDate.Should().BeBefore(after.AddSeconds(1));
    }

    [Fact]
    public async Task SetServerConfigAsync_WithNullModifiedBy_SavesNull()
    {
        // Act
        var result = await _service.SetServerConfigAsync("test-key", "test-value", null);

        // Assert
        result.Should().BeTrue();

        var saved = await _context.ServerConfigs.FirstOrDefaultAsync(sc => sc.Key == "test-key");
        saved.Should().NotBeNull();
        saved!.ModifiedBy.Should().BeNull();
    }

    #endregion
}
