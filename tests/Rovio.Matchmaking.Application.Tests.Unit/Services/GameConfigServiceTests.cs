
namespace Rovio.Matchmaking.Application.Tests.Unit.Services;

public sealed class GameConfigServiceTests
{
    private readonly IGameConfigRepository _repository = Substitute.For<IGameConfigRepository>();
    private readonly IGameConfigProjector _projector = Substitute.For<IGameConfigProjector>();
    private readonly FixedTimeProvider _timeProvider = new();
    private readonly GameConfigService _sut;

    public GameConfigServiceTests()
    {
        _sut = new GameConfigService(_repository, _projector, _timeProvider);
    }

    [Fact]
    public async Task GetAsync_WhenConfigExists_ShouldReturnDto()
    {
        // Arrange
        var config = GameMatchConfigFactory.Create();
        _repository.GetAsync(GameIdFactory.Create(), Arg.Any<CancellationToken>()).Returns(config);

        // Act
        var result = await _sut.GetAsync(GameIdFactory.Create());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.GameId.Should().Be(AngryBirds2GameId);
        result.Value.MinPlayers.Should().Be(DefaultMinPlayers);
        result.Value.MaxPlayers.Should().Be(DefaultMaxPlayers);
    }

    [Fact]
    public async Task GetAsync_WhenConfigDoesNotExist_ShouldFail()
    {
        // Arrange
        _repository.GetAsync(GameIdFactory.Create(), Arg.Any<CancellationToken>())
            .Returns((GameMatchConfig?)null);

        // Act
        var result = await _sut.GetAsync(GameIdFactory.Create());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.GameNotFound);
        result.Error.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task ListGameIdsAsync_WhenConfigsExist_ShouldReturnOrderedIds()
    {
        // Arrange
        var first = GameMatchConfigFactory.Create(gameId: AlternateGameId);
        var second = GameMatchConfigFactory.Create(gameId: AngryBirds2GameId);
        _repository.ListAsync(Arg.Any<CancellationToken>()).Returns([first, second]);

        // Act
        var result = await _sut.ListGameIdsAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Equal(AngryBirds2GameId, AlternateGameId);
    }

    [Fact]
    public async Task ListGameIdsAsync_WhenRepositoryThrows_ShouldFail()
    {
        // Arrange
        _repository.ListAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException());

        // Act
        var result = await _sut.ListGameIdsAsync();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.PostgresUnavailable);
        result.Error.ErrorType.Should().Be(ErrorType.Unavailable);
    }

    [Fact]
    public async Task UpsertAsync_WhenValidRequestProvided_ShouldPersistAndPublish()
    {
        // Arrange
        var request = UpsertGameConfigRequestFactory.CreateWithLatencyPolicy();
        _repository.GetAsync(GameIdFactory.Create(), Arg.Any<CancellationToken>())
            .Returns((GameMatchConfig?)null);

        // Act
        var result = await _sut.UpsertAsync(GameIdFactory.Create(), request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.GameId.Should().Be(AngryBirds2GameId);
        result.Value.MinPlayers.Should().Be(DefaultMinPlayers);
        result.Value.MaxPlayers.Should().Be(DefaultMaxPlayers);
        await _repository.Received(1).UpsertAsync(Arg.Any<GameMatchConfig>(), Arg.Any<CancellationToken>());
        await _projector.Received(1).PublishAsync(Arg.Any<GameMatchConfig>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpsertAsync_WhenExistingConfigFound_ShouldPreserveCreatedAt()
    {
        // Arrange
        var existing = GameMatchConfigFactory.Create(createdAt: OlderEnqueueAt, updatedAt: OlderEnqueueAt);
        var request = UpsertGameConfigRequestFactory.Create();
        _repository.GetAsync(GameIdFactory.Create(), Arg.Any<CancellationToken>()).Returns(existing);

        // Act
        var result = await _sut.UpsertAsync(GameIdFactory.Create(), request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CreatedAt.Should().Be(OlderEnqueueAt);
        result.Value.UpdatedAt.Should().Be(DefaultNow);
    }

    [Fact]
    public async Task UpsertAsync_WhenLatencyPolicyIsInvalid_ShouldFail()
    {
        // Arrange
        var request = UpsertGameConfigRequestFactory.CreateWithInvalidLatencyPolicy();
        _repository.GetAsync(GameIdFactory.Create(), Arg.Any<CancellationToken>())
            .Returns((GameMatchConfig?)null);

        // Act
        var result = await _sut.UpsertAsync(GameIdFactory.Create(), request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.InvalidLatencyPolicy);
    }

    [Fact]
    public async Task UpsertAsync_WhenMatchSizeIsInvalid_ShouldFail()
    {
        // Arrange
        var request = UpsertGameConfigRequestFactory.CreateWithInvalidMatchSize();
        _repository.GetAsync(GameIdFactory.Create(), Arg.Any<CancellationToken>())
            .Returns((GameMatchConfig?)null);

        // Act
        var result = await _sut.UpsertAsync(GameIdFactory.Create(), request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.InvalidMatchSize);
    }

    [Fact]
    public async Task UpsertAsync_WhenRepositoryGetThrows_ShouldFail()
    {
        // Arrange
        var request = UpsertGameConfigRequestFactory.Create();
        _repository.GetAsync(GameIdFactory.Create(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException());

        // Act
        var result = await _sut.UpsertAsync(GameIdFactory.Create(), request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.PostgresUnavailable);
    }

    [Fact]
    public async Task UpsertAsync_WhenRepositoryUpsertThrows_ShouldFail()
    {
        // Arrange
        var request = UpsertGameConfigRequestFactory.Create();
        _repository.GetAsync(GameIdFactory.Create(), Arg.Any<CancellationToken>())
            .Returns((GameMatchConfig?)null);
        _repository.UpsertAsync(Arg.Any<GameMatchConfig>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException());

        // Act
        var result = await _sut.UpsertAsync(GameIdFactory.Create(), request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.PostgresUnavailable);
    }

    [Fact]
    public async Task UpsertAsync_WhenProjectionThrows_ShouldFail()
    {
        // Arrange
        var request = UpsertGameConfigRequestFactory.Create();
        _repository.GetAsync(GameIdFactory.Create(), Arg.Any<CancellationToken>())
            .Returns((GameMatchConfig?)null);
        _projector.PublishAsync(Arg.Any<GameMatchConfig>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException());

        // Act
        var result = await _sut.UpsertAsync(GameIdFactory.Create(), request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.ConfigProjectionFailed);
        result.Error.ErrorType.Should().Be(ErrorType.Unavailable);
    }

    [Fact]
    public async Task EnsureSeededAndProjectedAsync_WhenConfigsExist_ShouldProjectAll()
    {
        // Arrange
        var configs = new List<GameMatchConfig> { GameMatchConfigFactory.Create() };
        _repository.ListAsync(Arg.Any<CancellationToken>()).Returns(configs);

        // Act
        var result = await _sut.EnsureSeededAndProjectedAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _repository.DidNotReceive()
            .UpsertAsync(Arg.Any<GameMatchConfig>(), Arg.Any<CancellationToken>());
        await _projector.Received(1).ProjectAllAsync(configs, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnsureSeededAndProjectedAsync_WhenNoConfigsExist_ShouldSeedAndProject()
    {
        // Arrange
        var seeded = new List<GameMatchConfig> { GameMatchConfigFactory.CreateAngryBirds2Defaults() };
        _repository.ListAsync(Arg.Any<CancellationToken>())
            .Returns(
                Array.Empty<GameMatchConfig>(),
                seeded);

        // Act
        var result = await _sut.EnsureSeededAndProjectedAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1)
            .UpsertAsync(Arg.Any<GameMatchConfig>(), Arg.Any<CancellationToken>());
        await _projector.Received(1).ProjectAllAsync(seeded, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnsureSeededAndProjectedAsync_WhenRepositoryThrows_ShouldFail()
    {
        // Arrange
        _repository.ListAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException());

        // Act
        var result = await _sut.EnsureSeededAndProjectedAsync();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.ConfigBootstrapFailed);
    }
}
