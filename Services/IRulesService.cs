using Madtorio.Data.Models;

namespace Madtorio.Services;

public interface IRulesService
{
    // Category operations
    Task<List<RuleCategory>> GetAllCategoriesAsync(bool includeDisabled = false);
    Task<RuleCategory?> GetCategoryByIdAsync(int id);
    Task<RuleCategory> CreateCategoryAsync(RuleCategory category);
    Task<bool> UpdateCategoryAsync(RuleCategory category);
    Task<bool> DeleteCategoryAsync(int id);
    Task<bool> ReorderCategoriesAsync(List<int> categoryIds);

    // Rule operations
    Task<List<Rule>> GetRulesByCategoryAsync(int categoryId, bool includeDisabled = false);
    Task<Rule?> GetRuleByIdAsync(int id);
    Task<Rule> CreateRuleAsync(Rule rule);
    Task<bool> UpdateRuleAsync(Rule rule);
    Task<bool> DeleteRuleAsync(int id);
    Task<bool> ReorderRulesAsync(int categoryId, List<int> ruleIds);

    // Display operations (for public pages)
    Task<Dictionary<RuleCategory, List<Rule>>> GetActiveRulesGroupedAsync();

    // Server config operations
    Task<string?> GetServerConfigAsync(string key);
    Task<bool> SetServerConfigAsync(string key, string value, string? modifiedBy = null);
}
