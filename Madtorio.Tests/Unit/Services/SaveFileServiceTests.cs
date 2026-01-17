using Madtorio.Data;
using Madtorio.Data.Models;
using Madtorio.Services;
using Madtorio.Tests.Helpers;

namespace Madtorio.Tests.Unit.Services;

public class SaveFileServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly SaveFileService _service;
    private readonly Mock<ILogger<SaveFileService>> _mockLogger;

    public SaveFileServiceTests()
    {
        _context = InMemoryDbContextFactory.Create();
        _mockLogger = new Mock<ILogger<SaveFileService>>();
        _service = new SaveFileService(_context, _mockLogger.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region GetAllSavesAsync Tests

    [Fact]
    public async Task GetAllSavesAsync_WithMultipleSaves_ReturnsOrderedByModifiedDateDescending()
    {
        // Arrange
        var older = TestDataBuilder.SaveFile()
            .WithFileName("older.zip")
            .WithModifiedDate(DateTime.UtcNow.AddDays(-2))
            .Build();

        var newer = TestDataBuilder.SaveFile()
            .WithFileName("newer.zip")
            .WithModifiedDate(DateTime.UtcNow.AddDays(-1))
            .Build();

        var newest = TestDataBuilder.SaveFile()
            .WithFileName("newest.zip")
            .WithModifiedDate(DateTime.UtcNow)
            .Build();

        _context.SaveFiles.AddRange(older, newer, newest);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAllSavesAsync();

        // Assert
        result.Should().HaveCount(3);
        result.Should().BeInDescendingOrder(s => s.ModifiedDate);
        result[0].FileName.Should().Be("newest.zip");
        result[1].FileName.Should().Be("newer.zip");
        result[2].FileName.Should().Be("older.zip");
    }

    [Fact]
    public async Task GetAllSavesAsync_WithEmptyDatabase_ReturnsEmptyList()
    {
        // Act
        var result = await _service.GetAllSavesAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllSavesAsync_WithSingleSave_ReturnsSingleItem()
    {
        // Arrange
        var saveFile = TestDataBuilder.SaveFile().Build();
        _context.SaveFiles.Add(saveFile);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAllSavesAsync();

        // Assert
        result.Should().ContainSingle();
        result[0].FileName.Should().Be(saveFile.FileName);
    }

    #endregion

    #region GetSaveByIdAsync Tests

    [Fact]
    public async Task GetSaveByIdAsync_WithExistingId_ReturnsSaveFile()
    {
        // Arrange
        var saveFile = TestDataBuilder.SaveFile()
            .WithFileName("test.zip")
            .Build();
        _context.SaveFiles.Add(saveFile);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetSaveByIdAsync(saveFile.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(saveFile.Id);
        result.FileName.Should().Be("test.zip");
    }

    [Fact]
    public async Task GetSaveByIdAsync_WithNonExistentId_ReturnsNull()
    {
        // Act
        var result = await _service.GetSaveByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region CreateSaveAsync Tests

    [Fact]
    public async Task CreateSaveAsync_WithValidSaveFile_CreatesAndReturns()
    {
        // Arrange
        var saveFile = TestDataBuilder.SaveFile()
            .WithFileName("new-save.zip")
            .Build();

        // Act
        var result = await _service.CreateSaveAsync(saveFile);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.FileName.Should().Be("new-save.zip");

        var saved = await _context.SaveFiles.FindAsync(result.Id);
        saved.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateSaveAsync_WithoutUploadDate_SetsUploadDateToNow()
    {
        // Arrange
        var saveFile = TestDataBuilder.SaveFile()
            .WithUploadDate(default)
            .Build();
        var before = DateTime.UtcNow;

        // Act
        var result = await _service.CreateSaveAsync(saveFile);
        var after = DateTime.UtcNow;

        // Assert
        result.UploadDate.Should().BeAfter(before.AddSeconds(-1));
        result.UploadDate.Should().BeBefore(after.AddSeconds(1));
    }

    [Fact]
    public async Task CreateSaveAsync_SetsModifiedDate()
    {
        // Arrange
        var saveFile = TestDataBuilder.SaveFile().Build();
        var before = DateTime.UtcNow;

        // Act
        var result = await _service.CreateSaveAsync(saveFile);
        var after = DateTime.UtcNow;

        // Assert
        result.ModifiedDate.Should().BeAfter(before.AddSeconds(-1));
        result.ModifiedDate.Should().BeBefore(after.AddSeconds(1));
    }

    #endregion

    #region UpdateSaveAsync Tests

    [Fact]
    public async Task UpdateSaveAsync_WithExistingSave_UpdatesAndReturnsTrue()
    {
        // Arrange
        var saveFile = TestDataBuilder.SaveFile()
            .WithFileName("original.zip")
            .WithDescription("Original description")
            .Build();
        _context.SaveFiles.Add(saveFile);
        await _context.SaveChangesAsync();

        saveFile.FileName = "updated.zip";
        saveFile.Description = "Updated description";

        // Act
        var result = await _service.UpdateSaveAsync(saveFile);

        // Assert
        result.Should().BeTrue();

        var updated = await _context.SaveFiles.FindAsync(saveFile.Id);
        updated!.FileName.Should().Be("updated.zip");
        updated.Description.Should().Be("Updated description");
    }

    [Fact]
    public async Task UpdateSaveAsync_UpdatesModifiedDate()
    {
        // Arrange
        var saveFile = TestDataBuilder.SaveFile()
            .WithModifiedDate(DateTime.UtcNow.AddDays(-1))
            .Build();
        _context.SaveFiles.Add(saveFile);
        await _context.SaveChangesAsync();

        var originalModifiedDate = saveFile.ModifiedDate;
        var before = DateTime.UtcNow;

        // Act
        await _service.UpdateSaveAsync(saveFile);
        var after = DateTime.UtcNow;

        // Assert
        var updated = await _context.SaveFiles.FindAsync(saveFile.Id);
        updated!.ModifiedDate.Should().NotBeNull();
        updated.ModifiedDate!.Value.Should().BeAfter(originalModifiedDate!.Value);
        updated.ModifiedDate!.Value.Should().BeAfter(before.AddSeconds(-1));
        updated.ModifiedDate!.Value.Should().BeBefore(after.AddSeconds(1));
    }

    [Fact]
    public async Task UpdateSaveAsync_WithNonExistentSave_ReturnsFalse()
    {
        // Arrange
        var saveFile = TestDataBuilder.SaveFile()
            .WithId(999)
            .Build();

        // Act
        var result = await _service.UpdateSaveAsync(saveFile);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region DeleteSaveAsync Tests

    [Fact]
    public async Task DeleteSaveAsync_WithExistingId_DeletesAndReturnsTrue()
    {
        // Arrange
        var saveFile = TestDataBuilder.SaveFile().Build();
        _context.SaveFiles.Add(saveFile);
        await _context.SaveChangesAsync();
        var id = saveFile.Id;

        // Act
        var result = await _service.DeleteSaveAsync(id);

        // Assert
        result.Should().BeTrue();

        var deleted = await _context.SaveFiles.FindAsync(id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteSaveAsync_WithNonExistentId_ReturnsFalse()
    {
        // Act
        var result = await _service.DeleteSaveAsync(999);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetStatisticsAsync Tests

    [Fact]
    public async Task GetStatisticsAsync_WithMultipleSaves_ReturnsCorrectStatistics()
    {
        // Arrange
        var save1 = TestDataBuilder.SaveFile()
            .WithFileSize(1024 * 1024) // 1 MB
            .Build();
        var save2 = TestDataBuilder.SaveFile()
            .WithFileSize(2 * 1024 * 1024) // 2 MB
            .Build();
        var save3 = TestDataBuilder.SaveFile()
            .WithFileSize(3 * 1024 * 1024) // 3 MB
            .Build();

        _context.SaveFiles.AddRange(save1, save2, save3);
        await _context.SaveChangesAsync();

        // Act
        var (totalFiles, totalSize) = await _service.GetStatisticsAsync();

        // Assert
        totalFiles.Should().Be(3);
        totalSize.Should().Be(6 * 1024 * 1024); // 6 MB total
    }

    [Fact]
    public async Task GetStatisticsAsync_WithEmptyDatabase_ReturnsZeros()
    {
        // Act
        var (totalFiles, totalSize) = await _service.GetStatisticsAsync();

        // Assert
        totalFiles.Should().Be(0);
        totalSize.Should().Be(0);
    }

    [Fact]
    public async Task GetStatisticsAsync_WithSingleFile_ReturnsCorrectCount()
    {
        // Arrange
        var saveFile = TestDataBuilder.SaveFile()
            .WithFileSize(5 * 1024 * 1024) // 5 MB
            .Build();
        _context.SaveFiles.Add(saveFile);
        await _context.SaveChangesAsync();

        // Act
        var (totalFiles, totalSize) = await _service.GetStatisticsAsync();

        // Assert
        totalFiles.Should().Be(1);
        totalSize.Should().Be(5 * 1024 * 1024);
    }

    #endregion
}
