using Madtorio.Services;
using Microsoft.AspNetCore.Hosting;

namespace Madtorio.Tests.Unit.Services;

public class ChunkedFileUploadServiceTests : IDisposable
{
    private readonly ChunkedFileUploadService _service;
    private readonly Mock<IWebHostEnvironment> _mockEnvironment;
    private readonly Mock<ILogger<ChunkedFileUploadService>> _mockLogger;
    private readonly string _testRoot;

    public ChunkedFileUploadServiceTests()
    {
        _mockEnvironment = new Mock<IWebHostEnvironment>();
        _mockLogger = new Mock<ILogger<ChunkedFileUploadService>>();

        // Create a unique temp directory for each test
        _testRoot = Path.Combine(Path.GetTempPath(), "madtorio_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testRoot);

        _mockEnvironment.Setup(e => e.ContentRootPath).Returns(_testRoot);
        _service = new ChunkedFileUploadService(_mockEnvironment.Object, _mockLogger.Object);
    }

    public void Dispose()
    {
        // Clean up test directory
        if (Directory.Exists(_testRoot))
        {
            try
            {
                Directory.Delete(_testRoot, true);
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }

    #region InitiateUploadAsync Tests

    [Fact]
    public async Task InitiateUploadAsync_WithValidInput_ReturnsUploadId()
    {
        // Arrange
        var fileName = "test.zip";
        var fileSize = 1024L * 1024 * 50; // 50 MB
        var uploadedBy = "test-user";

        // Act
        var uploadId = await _service.InitiateUploadAsync(fileName, fileSize, uploadedBy);

        // Assert
        uploadId.Should().NotBeNullOrEmpty();
        Guid.TryParse(uploadId, out _).Should().BeTrue();
    }

    [Fact]
    public async Task InitiateUploadAsync_CreatesUploadSession()
    {
        // Arrange
        var fileName = "test.zip";
        var fileSize = 1024L * 1024 * 50; // 50 MB
        var uploadedBy = "test-user";

        // Act
        var uploadId = await _service.InitiateUploadAsync(fileName, fileSize, uploadedBy);

        // Assert
        var session = await _service.GetUploadSessionAsync(uploadId);
        session.Should().NotBeNull();
        session!.UploadId.Should().Be(uploadId);
        session.FileName.Should().Be(fileName);
        session.FileSize.Should().Be(fileSize);
        session.UploadedBy.Should().Be(uploadedBy);
        session.UploadedChunks.Should().BeEmpty();
    }

    [Fact]
    public async Task InitiateUploadAsync_CalculatesTotalChunksCorrectly()
    {
        // Arrange
        var fileSize = 10485760L * 2 + 5000000; // 2 full chunks + partial chunk = 3 total chunks
        var fileName = "test.zip";

        // Act
        var uploadId = await _service.InitiateUploadAsync(fileName, fileSize, "user");

        // Assert
        var session = await _service.GetUploadSessionAsync(uploadId);
        session!.TotalChunks.Should().Be(3);
    }

    [Fact]
    public async Task InitiateUploadAsync_CreatesTempDirectory()
    {
        // Arrange
        var fileName = "test.zip";
        var fileSize = 1024L * 1024;

        // Act
        var uploadId = await _service.InitiateUploadAsync(fileName, fileSize, "user");

        // Assert
        var tempPath = Path.Combine(_testRoot, "AppData", "uploads", "temp", uploadId);
        Directory.Exists(tempPath).Should().BeTrue();
    }

    [Fact]
    public async Task InitiateUploadAsync_WithFileTooLarge_ThrowsException()
    {
        // Arrange
        var fileName = "huge.zip";
        var fileSize = 600L * 1024 * 1024; // 600 MB (over 500 MB limit)

        // Act & Assert
        await FluentActions.Invoking(async () =>
                await _service.InitiateUploadAsync(fileName, fileSize, "user"))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*exceeds*500*");
    }

    [Fact]
    public async Task InitiateUploadAsync_SetsCreatedAtTimestamp()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var uploadId = await _service.InitiateUploadAsync("test.zip", 1024, "user");
        var after = DateTime.UtcNow;

        // Assert
        var session = await _service.GetUploadSessionAsync(uploadId);
        session!.CreatedAt.Should().BeAfter(before.AddSeconds(-1));
        session.CreatedAt.Should().BeBefore(after.AddSeconds(1));
    }

    #endregion

    #region UploadChunkAsync Tests

    [Fact]
    public async Task UploadChunkAsync_WithValidChunk_SavesChunkFile()
    {
        // Arrange
        var uploadId = await _service.InitiateUploadAsync("test.zip", 1024 * 1024, "user");
        var chunkData = new byte[1024];
        new Random().NextBytes(chunkData);

        // Act
        var (success, error) = await _service.UploadChunkAsync(uploadId, 0, chunkData);

        // Assert
        success.Should().BeTrue();
        error.Should().BeNull();

        var chunkPath = Path.Combine(_testRoot, "AppData", "uploads", "temp", uploadId, "chunk_0000.tmp");
        File.Exists(chunkPath).Should().BeTrue();
        var savedData = await File.ReadAllBytesAsync(chunkPath);
        savedData.Should().Equal(chunkData);
    }

    [Fact]
    public async Task UploadChunkAsync_UpdatesUploadedChunksList()
    {
        // Arrange
        var uploadId = await _service.InitiateUploadAsync("test.zip", 1024 * 1024, "user");
        var chunkData = new byte[1024];

        // Act
        await _service.UploadChunkAsync(uploadId, 0, chunkData);
        await _service.UploadChunkAsync(uploadId, 2, chunkData);

        // Assert
        var session = await _service.GetUploadSessionAsync(uploadId);
        session!.UploadedChunks.Should().Contain(new[] { 0, 2 });
        session.UploadedChunks.Should().HaveCount(2);
    }

    [Fact]
    public async Task UploadChunkAsync_WithNonExistentSession_ReturnsFalse()
    {
        // Arrange
        var chunkData = new byte[1024];

        // Act
        var (success, error) = await _service.UploadChunkAsync("non-existent-id", 0, chunkData);

        // Assert
        success.Should().BeFalse();
        error.Should().Contain("session not found");
    }

    [Fact]
    public async Task UploadChunkAsync_WithMultipleChunks_SavesAllChunks()
    {
        // Arrange
        var uploadId = await _service.InitiateUploadAsync("test.zip", 30 * 1024 * 1024, "user");
        var chunk1 = new byte[1024];
        var chunk2 = new byte[2048];
        var chunk3 = new byte[512];

        new Random().NextBytes(chunk1);
        new Random().NextBytes(chunk2);
        new Random().NextBytes(chunk3);

        // Act
        await _service.UploadChunkAsync(uploadId, 0, chunk1);
        await _service.UploadChunkAsync(uploadId, 1, chunk2);
        await _service.UploadChunkAsync(uploadId, 2, chunk3);

        // Assert
        var session = await _service.GetUploadSessionAsync(uploadId);
        session!.UploadedChunks.Should().HaveCount(3);
        session.UploadedChunks.Should().Contain(new[] { 0, 1, 2 });

        var chunk1Path = Path.Combine(_testRoot, "AppData", "uploads", "temp", uploadId, "chunk_0000.tmp");
        var chunk2Path = Path.Combine(_testRoot, "AppData", "uploads", "temp", uploadId, "chunk_0001.tmp");
        var chunk3Path = Path.Combine(_testRoot, "AppData", "uploads", "temp", uploadId, "chunk_0002.tmp");

        File.Exists(chunk1Path).Should().BeTrue();
        File.Exists(chunk2Path).Should().BeTrue();
        File.Exists(chunk3Path).Should().BeTrue();
    }

    #endregion

    #region FinalizeUploadAsync Tests

    [Fact]
    public async Task FinalizeUploadAsync_WithAllChunks_AssemblesFileSuccessfully()
    {
        // Arrange
        // Note: Service expects 1 chunk for files < 10MB (ChunkSize = 10485760)
        var fileSize = 3000L; // 3000 bytes = 1 chunk
        var uploadId = await _service.InitiateUploadAsync("test.zip", fileSize, "user");

        // Verify it calculated 1 chunk
        var session = await _service.GetUploadSessionAsync(uploadId);
        session!.TotalChunks.Should().Be(1);

        // Upload the single chunk
        var chunk = new byte[3000];
        for (int i = 0; i < 3000; i++)
        {
            chunk[i] = (byte)(i % 256);
        }

        await _service.UploadChunkAsync(uploadId, 0, chunk);

        // Act
        var (success, filePath, error) = await _service.FinalizeUploadAsync(uploadId);

        // Assert
        success.Should().BeTrue();
        error.Should().BeNull();
        filePath.Should().NotBeNullOrEmpty();

        // Verify final file exists and has correct content
        var finalFilePath = Path.Combine(_testRoot, "AppData", "uploads", "saves", filePath!);
        File.Exists(finalFilePath).Should().BeTrue();

        var finalContent = await File.ReadAllBytesAsync(finalFilePath);
        finalContent.Should().HaveCount(3000);
        finalContent.Should().Equal(chunk);
    }

    [Fact]
    public async Task FinalizeUploadAsync_RemovesTempDirectory()
    {
        // Arrange
        var uploadId = await _service.InitiateUploadAsync("test.zip", 1000, "user");
        var chunk = new byte[1000];
        await _service.UploadChunkAsync(uploadId, 0, chunk);

        var tempPath = Path.Combine(_testRoot, "AppData", "uploads", "temp", uploadId);
        tempPath.Should().NotBeNull();
        Directory.Exists(tempPath).Should().BeTrue();

        // Act
        await _service.FinalizeUploadAsync(uploadId);

        // Assert
        Directory.Exists(tempPath).Should().BeFalse();
    }

    [Fact]
    public async Task FinalizeUploadAsync_RemovesUploadSession()
    {
        // Arrange
        var uploadId = await _service.InitiateUploadAsync("test.zip", 1000, "user");
        var chunk = new byte[1000];
        await _service.UploadChunkAsync(uploadId, 0, chunk);

        // Act
        await _service.FinalizeUploadAsync(uploadId);

        // Assert
        var session = await _service.GetUploadSessionAsync(uploadId);
        session.Should().BeNull();
    }

    [Fact]
    public async Task FinalizeUploadAsync_WithMissingChunks_ReturnsFalse()
    {
        // Arrange
        var uploadId = await _service.InitiateUploadAsync("test.zip", 30 * 1024 * 1024, "user");
        var chunk = new byte[1000];
        await _service.UploadChunkAsync(uploadId, 0, chunk);
        // Missing chunks 1 and 2

        // Act
        var (success, filePath, error) = await _service.FinalizeUploadAsync(uploadId);

        // Assert
        success.Should().BeFalse();
        filePath.Should().BeNull();
        error.Should().Contain("Missing");
        error.Should().Contain("chunks");
    }

    [Fact]
    public async Task FinalizeUploadAsync_WithNonExistentSession_ReturnsFalse()
    {
        // Act
        var (success, filePath, error) = await _service.FinalizeUploadAsync("non-existent-id");

        // Assert
        success.Should().BeFalse();
        filePath.Should().BeNull();
        error.Should().Contain("session not found");
    }

    [Fact]
    public async Task FinalizeUploadAsync_ReturnsJustFileName()
    {
        // Arrange
        var uploadId = await _service.InitiateUploadAsync("test.zip", 1000, "user");
        var chunk = new byte[1000];
        await _service.UploadChunkAsync(uploadId, 0, chunk);

        // Act
        var (success, filePath, error) = await _service.FinalizeUploadAsync(uploadId);

        // Assert
        success.Should().BeTrue();
        filePath.Should().NotBeNullOrEmpty();
        filePath.Should().EndWith(".zip");
        filePath.Should().NotContain(Path.DirectorySeparatorChar.ToString());
        filePath.Should().NotContain(Path.AltDirectorySeparatorChar.ToString());
    }

    [Fact]
    public async Task FinalizeUploadAsync_GeneratesUniqueFileName()
    {
        // Arrange
        var uploadId1 = await _service.InitiateUploadAsync("test.zip", 1000, "user");
        var uploadId2 = await _service.InitiateUploadAsync("test.zip", 1000, "user");

        var chunk = new byte[1000];
        await _service.UploadChunkAsync(uploadId1, 0, chunk);
        await _service.UploadChunkAsync(uploadId2, 0, chunk);

        // Act
        var (_, filePath1, _) = await _service.FinalizeUploadAsync(uploadId1);
        var (_, filePath2, _) = await _service.FinalizeUploadAsync(uploadId2);

        // Assert
        filePath1.Should().NotBe(filePath2);
    }

    [Fact]
    public async Task FinalizeUploadAsync_PreservesFileExtension()
    {
        // Arrange
        var uploadId = await _service.InitiateUploadAsync("myfile.customext", 1000, "user");
        var chunk = new byte[1000];
        await _service.UploadChunkAsync(uploadId, 0, chunk);

        // Act
        var (success, filePath, _) = await _service.FinalizeUploadAsync(uploadId);

        // Assert
        success.Should().BeTrue();
        filePath.Should().EndWith(".customext");
    }

    #endregion

    #region CancelUploadAsync Tests

    [Fact]
    public async Task CancelUploadAsync_RemovesTempDirectory()
    {
        // Arrange
        var uploadId = await _service.InitiateUploadAsync("test.zip", 1024, "user");
        var tempPath = Path.Combine(_testRoot, "AppData", "uploads", "temp", uploadId);
        Directory.Exists(tempPath).Should().BeTrue();

        // Act
        var result = await _service.CancelUploadAsync(uploadId);

        // Assert
        result.Should().BeTrue();
        Directory.Exists(tempPath).Should().BeFalse();
    }

    [Fact]
    public async Task CancelUploadAsync_RemovesUploadSession()
    {
        // Arrange
        var uploadId = await _service.InitiateUploadAsync("test.zip", 1024, "user");

        // Act
        var result = await _service.CancelUploadAsync(uploadId);

        // Assert
        result.Should().BeTrue();
        var session = await _service.GetUploadSessionAsync(uploadId);
        session.Should().BeNull();
    }

    [Fact]
    public async Task CancelUploadAsync_WithNonExistentSession_ReturnsFalse()
    {
        // Act
        var result = await _service.CancelUploadAsync("non-existent-id");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CancelUploadAsync_WithUploadedChunks_DeletesAllFiles()
    {
        // Arrange
        var uploadId = await _service.InitiateUploadAsync("test.zip", 3000, "user");
        var chunk = new byte[1000];

        await _service.UploadChunkAsync(uploadId, 0, chunk);
        await _service.UploadChunkAsync(uploadId, 1, chunk);

        var tempPath = Path.Combine(_testRoot, "AppData", "uploads", "temp", uploadId);
        Directory.GetFiles(tempPath).Should().HaveCount(2);

        // Act
        await _service.CancelUploadAsync(uploadId);

        // Assert
        Directory.Exists(tempPath).Should().BeFalse();
    }

    #endregion

    #region GetUploadSessionAsync Tests

    [Fact]
    public async Task GetUploadSessionAsync_WithExistingSession_ReturnsSession()
    {
        // Arrange
        var uploadId = await _service.InitiateUploadAsync("test.zip", 1024, "test-user");

        // Act
        var session = await _service.GetUploadSessionAsync(uploadId);

        // Assert
        session.Should().NotBeNull();
        session!.UploadId.Should().Be(uploadId);
        session.FileName.Should().Be("test.zip");
        session.FileSize.Should().Be(1024);
        session.UploadedBy.Should().Be("test-user");
    }

    [Fact]
    public async Task GetUploadSessionAsync_WithNonExistentSession_ReturnsNull()
    {
        // Act
        var session = await _service.GetUploadSessionAsync("non-existent-id");

        // Assert
        session.Should().BeNull();
    }

    [Fact]
    public async Task GetUploadSessionAsync_ReflectsUploadedChunks()
    {
        // Arrange
        var uploadId = await _service.InitiateUploadAsync("test.zip", 3000, "user");
        var chunk = new byte[1000];

        await _service.UploadChunkAsync(uploadId, 0, chunk);
        await _service.UploadChunkAsync(uploadId, 2, chunk);

        // Act
        var session = await _service.GetUploadSessionAsync(uploadId);

        // Assert
        session!.UploadedChunks.Should().Contain(new[] { 0, 2 });
        session.UploadedChunks.Should().NotContain(1);
    }

    #endregion

    #region Edge Cases and Integration Tests

    [Fact]
    public async Task FullUploadWorkflow_WithSingleChunk_Succeeds()
    {
        // Arrange
        var fileName = "small.zip";
        var fileSize = 5000L;
        var uploadedBy = "test-user";
        var fileContent = new byte[5000];
        new Random().NextBytes(fileContent);

        // Act - Initiate
        var uploadId = await _service.InitiateUploadAsync(fileName, fileSize, uploadedBy);

        // Act - Upload
        var (uploadSuccess, uploadError) = await _service.UploadChunkAsync(uploadId, 0, fileContent);

        // Act - Finalize
        var (finalizeSuccess, filePath, finalizeError) = await _service.FinalizeUploadAsync(uploadId);

        // Assert
        uploadSuccess.Should().BeTrue();
        uploadError.Should().BeNull();
        finalizeSuccess.Should().BeTrue();
        finalizeError.Should().BeNull();
        filePath.Should().NotBeNullOrEmpty();

        var finalFilePath = Path.Combine(_testRoot, "AppData", "uploads", "saves", filePath!);
        var finalContent = await File.ReadAllBytesAsync(finalFilePath);
        finalContent.Should().Equal(fileContent);
    }

    [Fact]
    public async Task FullUploadWorkflow_WithMultipleChunks_Succeeds()
    {
        // Arrange - Use realistic large file that requires multiple chunks
        // ChunkSize = 10485760 (10MB), so use 25MB file = 3 chunks
        var fileName = "multipart.zip";
        var chunkSize = 10485760; // 10MB
        var chunk1 = new byte[chunkSize];
        var chunk2 = new byte[chunkSize];
        var chunk3 = new byte[chunkSize / 2]; // Last chunk is smaller

        for (int i = 0; i < chunk1.Length; i++) chunk1[i] = (byte)(i % 256);
        for (int i = 0; i < chunk2.Length; i++) chunk2[i] = (byte)((i + 100) % 256);
        for (int i = 0; i < chunk3.Length; i++) chunk3[i] = (byte)((i + 200) % 256);

        var totalSize = chunk1.Length + chunk2.Length + chunk3.Length;

        // Act - Full workflow
        var uploadId = await _service.InitiateUploadAsync(fileName, totalSize, "user");

        // Verify chunks calculated correctly
        var session = await _service.GetUploadSessionAsync(uploadId);
        session!.TotalChunks.Should().Be(3);

        await _service.UploadChunkAsync(uploadId, 0, chunk1);
        await _service.UploadChunkAsync(uploadId, 1, chunk2);
        await _service.UploadChunkAsync(uploadId, 2, chunk3);

        var (success, filePath, error) = await _service.FinalizeUploadAsync(uploadId);

        // Assert
        success.Should().BeTrue();
        error.Should().BeNull();

        var finalFilePath = Path.Combine(_testRoot, "AppData", "uploads", "saves", filePath!);
        var finalContent = await File.ReadAllBytesAsync(finalFilePath);

        finalContent.Should().HaveCount((int)totalSize);
        finalContent.Take(chunkSize).Should().Equal(chunk1);
        finalContent.Skip(chunkSize).Take(chunkSize).Should().Equal(chunk2);
        finalContent.Skip(chunkSize * 2).Take(chunkSize / 2).Should().Equal(chunk3);
    }

    [Fact]
    public async Task ConcurrentUploads_DoNotInterfere()
    {
        // Arrange
        var upload1Id = await _service.InitiateUploadAsync("file1.zip", 1000, "user1");
        var upload2Id = await _service.InitiateUploadAsync("file2.zip", 2000, "user2");

        var chunk1 = new byte[1000];
        var chunk2 = new byte[2000];
        new Random(1).NextBytes(chunk1);
        new Random(2).NextBytes(chunk2);

        // Act
        await _service.UploadChunkAsync(upload1Id, 0, chunk1);
        await _service.UploadChunkAsync(upload2Id, 0, chunk2);

        var (success1, filePath1, _) = await _service.FinalizeUploadAsync(upload1Id);
        var (success2, filePath2, _) = await _service.FinalizeUploadAsync(upload2Id);

        // Assert
        success1.Should().BeTrue();
        success2.Should().BeTrue();
        filePath1.Should().NotBe(filePath2);

        var file1Path = Path.Combine(_testRoot, "AppData", "uploads", "saves", filePath1!);
        var file2Path = Path.Combine(_testRoot, "AppData", "uploads", "saves", filePath2!);

        var file1Content = await File.ReadAllBytesAsync(file1Path);
        var file2Content = await File.ReadAllBytesAsync(file2Path);

        file1Content.Should().Equal(chunk1);
        file2Content.Should().Equal(chunk2);
    }

    #endregion
}
