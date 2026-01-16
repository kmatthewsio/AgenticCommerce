using AgenticCommerce.Core.Interfaces;
using AgenticCommerce.Core.Models;
using AgenticCommerce.Infrastructure.Agents;
using AgenticCommerce.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace AgenticCommerce.Tests.Agents;

public class AgentServiceTests : IDisposable
{
    private readonly Mock<IArcClient> _arcClientMock;
    private readonly Mock<ILogger<AgentService>> _loggerMock;
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly AgenticCommerceDbContext _dbContext;
    private readonly AgentService _service;

    private const string TestWalletAddress = "0x6255d8dd3f84ec460fc8b07db58ab06384a2f487";

    public AgentServiceTests()
    {
        _arcClientMock = new Mock<IArcClient>();
        _loggerMock = new Mock<ILogger<AgentService>>();
        _loggerFactoryMock = new Mock<ILoggerFactory>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _configurationMock = new Mock<IConfiguration>();

        _arcClientMock.Setup(x => x.GetAddress()).Returns(TestWalletAddress);
        _loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(Mock.Of<ILogger>());

        // Use in-memory database for testing
        var options = new DbContextOptionsBuilder<AgenticCommerceDbContext>()
            .UseInMemoryDatabase(databaseName: $"AgentTestDb_{Guid.NewGuid()}")
            .Options;

        _dbContext = new AgenticCommerceDbContext(options);

        // No AI key = simulation mode (avoids OpenAI dependency in tests)
        var aiOptions = Options.Create(new AIOptions
        {
            OpenAIApiKey = null,
            OpenAIModel = "gpt-4o"
        });

        _service = new AgentService(
            _arcClientMock.Object,
            _loggerMock.Object,
            aiOptions,
            _loggerFactoryMock.Object,
            _dbContext,
            _httpClientFactoryMock.Object,
            _configurationMock.Object);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    #region CreateAgentAsync Tests

    [Fact]
    public async Task CreateAgentAsync_ValidConfig_CreatesAgent()
    {
        // Arrange
        var config = new AgentConfig
        {
            Name = "Test Agent",
            Description = "A test agent",
            Budget = 100.0m,
            Capabilities = new List<string> { "research", "payments" }
        };

        // Act
        var result = await _service.CreateAgentAsync(config);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().StartWith("agent_");
        result.Name.Should().Be("Test Agent");
        result.Description.Should().Be("A test agent");
        result.Budget.Should().Be(100.0m);
        result.CurrentBalance.Should().Be(100.0m);
        result.Status.Should().Be(AgentStatus.Active);
        result.WalletAddress.Should().Be(TestWalletAddress);
    }

    [Fact]
    public async Task CreateAgentAsync_PersistsToDatabase()
    {
        // Arrange
        var config = new AgentConfig
        {
            Name = "Persisted Agent",
            Budget = 50.0m
        };

        // Act
        var result = await _service.CreateAgentAsync(config);

        // Assert
        var dbAgent = await _dbContext.Agents.FindAsync(result.Id);
        dbAgent.Should().NotBeNull();
        dbAgent!.Name.Should().Be("Persisted Agent");
        dbAgent.Budget.Should().Be(50.0m);
    }

    [Fact]
    public async Task CreateAgentAsync_GeneratesUniqueIds()
    {
        // Arrange
        var config = new AgentConfig { Name = "Agent", Budget = 10.0m };

        // Act
        var agent1 = await _service.CreateAgentAsync(config);
        var agent2 = await _service.CreateAgentAsync(config);

        // Assert
        agent1.Id.Should().NotBe(agent2.Id);
    }

    [Fact]
    public async Task CreateAgentAsync_SetsCreatedAt()
    {
        // Arrange
        var before = DateTime.UtcNow;
        var config = new AgentConfig { Name = "Agent", Budget = 10.0m };

        // Act
        var result = await _service.CreateAgentAsync(config);
        var after = DateTime.UtcNow;

        // Assert
        result.CreatedAt.Should().BeOnOrAfter(before);
        result.CreatedAt.Should().BeOnOrBefore(after);
    }

    #endregion

    #region GetAgentAsync Tests

    [Fact]
    public async Task GetAgentAsync_ExistingAgent_ReturnsAgent()
    {
        // Arrange
        var config = new AgentConfig { Name = "Findable Agent", Budget = 25.0m };
        var created = await _service.CreateAgentAsync(config);

        // Act
        var result = await _service.GetAgentAsync(created.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(created.Id);
        result.Name.Should().Be("Findable Agent");
    }

    [Fact]
    public async Task GetAgentAsync_NonExistentAgent_ReturnsNull()
    {
        // Act
        var result = await _service.GetAgentAsync("agent_nonexistent");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetAgentInfoAsync Tests

    [Fact]
    public async Task GetAgentInfoAsync_ExistingAgent_ReturnsInfo()
    {
        // Arrange
        var config = new AgentConfig
        {
            Name = "Info Agent",
            Description = "Test description",
            Budget = 75.0m,
            Capabilities = new List<string> { "research" }
        };
        var created = await _service.CreateAgentAsync(config);

        // Act
        var result = await _service.GetAgentInfoAsync(created.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(created.Id);
        result.Name.Should().Be("Info Agent");
        result.Description.Should().Be("Test description");
        result.Budget.Should().Be(75.0m);
        result.CurrentBalance.Should().Be(75.0m);
        result.Status.Should().Be("Active");
    }

    [Fact]
    public async Task GetAgentInfoAsync_NonExistentAgent_ReturnsNull()
    {
        // Act
        var result = await _service.GetAgentInfoAsync("agent_nonexistent");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region ListAgentsAsync Tests

    [Fact]
    public async Task ListAgentsAsync_NoAgents_ReturnsEmptyList()
    {
        // Act
        var result = await _service.ListAgentsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ListAgentsAsync_MultipleAgents_ReturnsAll()
    {
        // Arrange
        await _service.CreateAgentAsync(new AgentConfig { Name = "Agent 1", Budget = 10 });
        await _service.CreateAgentAsync(new AgentConfig { Name = "Agent 2", Budget = 20 });
        await _service.CreateAgentAsync(new AgentConfig { Name = "Agent 3", Budget = 30 });

        // Act
        var result = await _service.ListAgentsAsync();

        // Assert
        result.Should().HaveCount(3);
        result.Select(a => a.Name).Should().Contain("Agent 1", "Agent 2", "Agent 3");
    }

    #endregion

    #region DeleteAgentAsync Tests

    [Fact]
    public async Task DeleteAgentAsync_ExistingAgent_ReturnsTrue()
    {
        // Arrange
        var config = new AgentConfig { Name = "Deletable Agent", Budget = 10.0m };
        var created = await _service.CreateAgentAsync(config);

        // Act
        var result = await _service.DeleteAgentAsync(created.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAgentAsync_ExistingAgent_RemovesFromDatabase()
    {
        // Arrange
        var config = new AgentConfig { Name = "Deletable Agent", Budget = 10.0m };
        var created = await _service.CreateAgentAsync(config);

        // Act
        await _service.DeleteAgentAsync(created.Id);

        // Assert
        var dbAgent = await _dbContext.Agents.FindAsync(created.Id);
        dbAgent.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAgentAsync_NonExistentAgent_ReturnsFalse()
    {
        // Act
        var result = await _service.DeleteAgentAsync("agent_nonexistent");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region RunAgentAsync Tests

    [Fact]
    public async Task RunAgentAsync_NonExistentAgent_ReturnsError()
    {
        // Act
        var result = await _service.RunAgentAsync("agent_nonexistent", "do something");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task RunAgentAsync_ExistingAgent_SetsAgentId()
    {
        // Arrange
        var config = new AgentConfig { Name = "Runner Agent", Budget = 10.0m };
        var created = await _service.CreateAgentAsync(config);

        // Act
        var result = await _service.RunAgentAsync(created.Id, "test task");

        // Assert
        result.AgentId.Should().Be(created.Id);
    }

    #endregion

    #region MakePurchaseAsync Tests

    [Fact]
    public async Task MakePurchaseAsync_NonExistentAgent_ReturnsError()
    {
        // Arrange
        var request = new PurchaseRequest
        {
            RecipientAddress = "0xRecipient",
            Amount = 5.0m,
            Description = "Test purchase"
        };

        // Act
        var result = await _service.MakePurchaseAsync("agent_nonexistent", request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task MakePurchaseAsync_InsufficientBalance_ReturnsError()
    {
        // Arrange
        var config = new AgentConfig { Name = "Poor Agent", Budget = 5.0m };
        var created = await _service.CreateAgentAsync(config);

        var request = new PurchaseRequest
        {
            RecipientAddress = "0xRecipient",
            Amount = 100.0m, // More than budget
            Description = "Too expensive"
        };

        // Act
        var result = await _service.MakePurchaseAsync(created.Id, request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Insufficient");
    }

    #endregion
}
