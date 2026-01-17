using Microsoft.EntityFrameworkCore;
using Madtorio.Data;
using Madtorio.Data.Models;

namespace Madtorio.Services;

public class RulesService : IRulesService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RulesService> _logger;

    public RulesService(ApplicationDbContext context, ILogger<RulesService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<RuleCategory>> GetAllCategoriesAsync(bool includeDisabled = false)
    {
        try
        {
            var query = _context.RuleCategories.Include(c => c.Rules).AsQueryable();

            if (!includeDisabled)
            {
                query = query.Where(c => c.IsEnabled);
            }

            return await query.OrderBy(c => c.DisplayOrder).ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving rule categories");
            return new List<RuleCategory>();
        }
    }

    public async Task<RuleCategory?> GetCategoryByIdAsync(int id)
    {
        try
        {
            return await _context.RuleCategories
                .Include(c => c.Rules)
                .FirstOrDefaultAsync(c => c.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving category with ID: {Id}", id);
            return null;
        }
    }

    public async Task<RuleCategory> CreateCategoryAsync(RuleCategory category)
    {
        try
        {
            // Set display order to max + 1
            var maxOrder = await _context.RuleCategories.MaxAsync(c => (int?)c.DisplayOrder) ?? 0;
            category.DisplayOrder = maxOrder + 1;
            category.CreatedDate = DateTime.UtcNow;

            _context.RuleCategories.Add(category);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Rule category created: {Name}", category.Name);
            return category;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating category: {Name}", category.Name);
            throw;
        }
    }

    public async Task<bool> UpdateCategoryAsync(RuleCategory category)
    {
        try
        {
            category.ModifiedDate = DateTime.UtcNow;
            _context.RuleCategories.Update(category);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Category updated: {Id}", category.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating category: {Id}", category.Id);
            return false;
        }
    }

    public async Task<bool> DeleteCategoryAsync(int id)
    {
        try
        {
            var category = await _context.RuleCategories.FindAsync(id);
            if (category == null)
            {
                _logger.LogWarning("Category not found for deletion: {Id}", id);
                return false;
            }

            _context.RuleCategories.Remove(category);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Category deleted: {Id}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting category: {Id}", id);
            return false;
        }
    }

    public async Task<bool> ReorderCategoriesAsync(List<int> categoryIds)
    {
        try
        {
            for (int i = 0; i < categoryIds.Count; i++)
            {
                var category = await _context.RuleCategories.FindAsync(categoryIds[i]);
                if (category != null)
                {
                    category.DisplayOrder = i + 1;
                    category.ModifiedDate = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Categories reordered");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reordering categories");
            return false;
        }
    }

    public async Task<List<Rule>> GetRulesByCategoryAsync(int categoryId, bool includeDisabled = false)
    {
        try
        {
            var query = _context.Rules.Where(r => r.CategoryId == categoryId);

            if (!includeDisabled)
            {
                query = query.Where(r => r.IsEnabled);
            }

            return await query.OrderBy(r => r.DisplayOrder).ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving rules for category: {CategoryId}", categoryId);
            return new List<Rule>();
        }
    }

    public async Task<Rule?> GetRuleByIdAsync(int id)
    {
        try
        {
            return await _context.Rules.FindAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving rule with ID: {Id}", id);
            return null;
        }
    }

    public async Task<Rule> CreateRuleAsync(Rule rule)
    {
        try
        {
            // Set display order to max + 1 within category
            var maxOrder = await _context.Rules
                .Where(r => r.CategoryId == rule.CategoryId)
                .MaxAsync(r => (int?)r.DisplayOrder) ?? 0;
            rule.DisplayOrder = maxOrder + 1;
            rule.CreatedDate = DateTime.UtcNow;

            _context.Rules.Add(rule);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Rule created in category {CategoryId}", rule.CategoryId);
            return rule;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating rule");
            throw;
        }
    }

    public async Task<bool> UpdateRuleAsync(Rule rule)
    {
        try
        {
            rule.ModifiedDate = DateTime.UtcNow;
            _context.Rules.Update(rule);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Rule updated: {Id}", rule.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating rule: {Id}", rule.Id);
            return false;
        }
    }

    public async Task<bool> DeleteRuleAsync(int id)
    {
        try
        {
            var rule = await _context.Rules.FindAsync(id);
            if (rule == null)
            {
                _logger.LogWarning("Rule not found for deletion: {Id}", id);
                return false;
            }

            _context.Rules.Remove(rule);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Rule deleted: {Id}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting rule: {Id}", id);
            return false;
        }
    }

    public async Task<bool> ReorderRulesAsync(int categoryId, List<int> ruleIds)
    {
        try
        {
            for (int i = 0; i < ruleIds.Count; i++)
            {
                var rule = await _context.Rules.FindAsync(ruleIds[i]);
                if (rule != null && rule.CategoryId == categoryId)
                {
                    rule.DisplayOrder = i + 1;
                    rule.ModifiedDate = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Rules reordered for category: {CategoryId}", categoryId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reordering rules for category: {CategoryId}", categoryId);
            return false;
        }
    }

    public async Task<Dictionary<RuleCategory, List<Rule>>> GetActiveRulesGroupedAsync()
    {
        try
        {
            var categories = await _context.RuleCategories
                .Include(c => c.Rules.Where(r => r.IsEnabled).OrderBy(r => r.DisplayOrder))
                .Where(c => c.IsEnabled)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

            return categories.ToDictionary(c => c, c => c.Rules.ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active rules grouped by category");
            return new Dictionary<RuleCategory, List<Rule>>();
        }
    }

    public async Task<string?> GetServerConfigAsync(string key)
    {
        try
        {
            var config = await _context.ServerConfigs
                .FirstOrDefaultAsync(sc => sc.Key == key);
            return config?.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving server config: {Key}", key);
            return null;
        }
    }

    public async Task<bool> SetServerConfigAsync(string key, string value, string? modifiedBy = null)
    {
        try
        {
            var config = await _context.ServerConfigs
                .FirstOrDefaultAsync(sc => sc.Key == key);

            if (config == null)
            {
                config = new ServerConfig
                {
                    Key = key,
                    Value = value,
                    ModifiedBy = modifiedBy,
                    ModifiedDate = DateTime.UtcNow
                };
                _context.ServerConfigs.Add(config);
            }
            else
            {
                config.Value = value;
                config.ModifiedBy = modifiedBy;
                config.ModifiedDate = DateTime.UtcNow;
                _context.ServerConfigs.Update(config);
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Server config updated: {Key}", key);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting server config: {Key}", key);
            return false;
        }
    }
}
