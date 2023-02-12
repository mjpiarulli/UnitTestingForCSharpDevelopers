using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.ReturnsExtensions;
using Users.Api.Logging;
using Users.Api.Models;
using Users.Api.Repositories;
using Users.Api.Services;
using Xunit;

namespace Users.Api.Tests.Unit.Application;

public class UserServiceTests
{
    private readonly UserService _sut;
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ILoggerAdapter<UserService> _logger = Substitute.For<ILoggerAdapter<UserService>>();

    public UserServiceTests()
    {
        _sut = new UserService(_userRepository, _logger);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnEmptyList_WhenNoUsersExist()
    {
        // Arrange
        _userRepository.GetAllAsync().Returns(Enumerable.Empty<User>());

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnUsers_WhenSomeUsersExist()
    {
        // Arrange
        var nickChapsas = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Nick Chapsas"
        };
        var expectedUsers = new[]
        {
            nickChapsas
        };
        _userRepository.GetAllAsync().Returns(expectedUsers);

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        //result.Single().Should().BeEquivalentTo(nickChapsas);
        result.Should().BeEquivalentTo(expectedUsers);
    }

    [Fact]
    public async Task GetAllAsync_ShouldLogMessages_WhenInvoked()
    {
        // Arrange
        _userRepository.GetAllAsync().Returns(Enumerable.Empty<User>());

        // Act
        await _sut.GetAllAsync();

        // Assert
        _logger.Received(1).LogInformation(Arg.Is("Retrieving all users"));
        _logger.Received(1).LogInformation(Arg.Is("All users retrieved in {0}ms"), Arg.Any<long>());
    }

    [Fact]
    public async Task GetAllAsync_ShouldLogMessageAndException_WhenExceptionIsThrown()
    {
        // Arrange
        var sqliteException = new SqliteException("Something went wrong", 500);
        _userRepository.GetAllAsync()
            .Throws(sqliteException);

        // Act
        var requestAction = async () => await _sut.GetAllAsync();

        // Assert
        await requestAction.Should()
            .ThrowAsync<SqliteException>().WithMessage("Something went wrong");
        _logger.Received(1).LogError(Arg.Is(sqliteException), Arg.Is("Something went wrong while retrieving all users"));
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnUser_WhenUserExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedUser = new User
        {
            Id = userId,
            FullName = "Michael Piarulli"
        };
        _userRepository.GetByIdAsync(userId).Returns(expectedUser);

        // Act
        var result = await _sut.GetByIdAsync(userId);

        // Assert
        result.Should().BeEquivalentTo(expectedUser);
        
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userRepository.GetByIdAsync(userId).ReturnsNull();

        // Act
        var result = await _sut.GetByIdAsync(userId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldLogMessages_WhenInvoked()
    {
        // Arrange
        var id = Arg.Any<Guid>();
        var expectedUser = new User
        {
            Id = id,
            FullName = "Michael Piarulli"
        };
        _userRepository.GetByIdAsync(id).Returns(expectedUser);

        // Act
        await _sut.GetByIdAsync(id);

        // Assert
        _logger.Received(1).LogInformation(Arg.Is("Retrieving user with id: {0}"), Arg.Is(id));
        _logger.Received(1).LogInformation(Arg.Is("User with id {0} retrieved in {1}ms"), Arg.Is(id), Arg.Any<long>());
    }

    [Fact]
    public async Task GetByIdAsync_ShouldLogMessageAndException_WhenExceptionIsThrown()
    {
        // Arrange
        var userId = Guid.NewGuid();
        const string exceptionMessage = "This is a testing error";
        var exception = new SqliteException(exceptionMessage, 500);
        _userRepository.GetByIdAsync(userId).Throws(exception);

        // Act
        var action= async () => await _sut.GetByIdAsync(userId);

        // Assert
        await action.Should().ThrowAsync<SqliteException>().WithMessage(exceptionMessage);
        _logger.Received(1).LogInformation(Arg.Is("Retrieving user with id: {0}"), Arg.Is(userId));
        _logger.Received(1).LogError(exception, "Something went wrong while retrieving user with id {0}", Arg.Is(userId));
        _logger.Received(1).LogInformation(Arg.Is("User with id {0} retrieved in {1}ms"), Arg.Is(userId), Arg.Any<long>());
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateUser_WhenCreateDetailsAreValid()
    {
        // Arrange
        var newUser = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Michael Piarulli"
        };
        _userRepository.CreateAsync(newUser).Returns(true);

        // Act
        var result = await _sut.CreateAsync(newUser);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_ShouldLogMessages_WhenCreatingUser()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Michael Piarulli"
        };       
        _userRepository.CreateAsync(user).Returns(true);

        // Act
        await _sut.CreateAsync(user);

        // Assert
        _logger.Received(1).LogInformation(Arg.Is("Creating user with id {0} and name: {1}"), Arg.Is(user.Id), Arg.Is(user.FullName));
        _logger.Received(1).LogInformation(Arg.Is("User with id {0} created in {1}ms"), Arg.Is(user.Id), Arg.Any<long>());
    }

    [Fact]
    public async Task CreateAsync_ShouldLogMessageAndException_WhenExceptionIsThrown()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Michael Piarulli"
        };
        const string exceptionMessage = "This is a test error";
        var exception = new SqliteException(exceptionMessage, 500);
        _userRepository.CreateAsync(user).Throws(exception);

        // Act
        var action = async() => await _sut.CreateAsync(user);

        // Assert
        await action.Should().ThrowAsync<SqliteException>().WithMessage(exceptionMessage);
        _logger.Received(1).LogInformation(Arg.Is("Creating user with id {0} and name: {1}"), Arg.Is(user.Id), Arg.Is(user.FullName));
        _logger.Received(1).LogError(exception, Arg.Is("Something went wrong while creating a user"));
        _logger.Received(1).LogInformation(Arg.Is("User with id {0} created in {1}ms"), Arg.Is(user.Id), Arg.Any<long>());
    }

    [Fact]
    public async Task DeleteByIdAsync_ShouldDeleteUser_WhenUserExists()
    {
        // Arrange
        var id = Arg.Any<Guid>();
        _userRepository.DeleteByIdAsync(id).Returns(true);

        // Act
        var result = await _sut.DeleteByIdAsync(id);

        // Assert
        result.Should().Be(true);
    }

    [Fact]
    public async Task DeleteByIdAsync_ShouldNotDeleteUser_WhenUserDoesNotExist()
    {
        // Arrange
        var id = Arg.Any<Guid>();
        _userRepository.DeleteByIdAsync(id).Returns(false);

        // Act
        var result = await _sut.DeleteByIdAsync(id);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteByIdAsync_ShouldLogMessages_WhenInvoked()
    {
        // Arrange
        var id = Arg.Any<Guid>();
        _userRepository.DeleteByIdAsync(id).Returns(true);

        // Act
        await _sut.DeleteByIdAsync(id);

        // Assert
        _logger.Received(1).LogInformation(Arg.Is("Deleting user with id: {0}"), Arg.Is(id));
        _logger.Received(1).LogInformation(Arg.Is("User with id {0} deleted in {1}ms"), Arg.Is(id), Arg.Any<long>());
    }

    [Fact]
    public async Task DeleteByIdAsync_ShouldLogMessageAndException_WhenExceptionIsThrown()
    {
        // Arrange
        var id = Arg.Any<Guid>();
        const string exceptionMessage = "This is a test exception message";
        var exception = new SqliteException(exceptionMessage, 500);
        _userRepository.DeleteByIdAsync(id).Throws(exception);

        // Act
        var action = async () => await _sut.DeleteByIdAsync(id);

        // Assert
        await action.Should().ThrowAsync<SqliteException>().WithMessage(exceptionMessage);
        _logger.Received(1).LogInformation(Arg.Is("Deleting user with id: {0}"), Arg.Is(id));
        _logger.Received(1).LogError(exception, Arg.Is("Something went wrong while deleting user with id {0}"), Arg.Is(id));
        _logger.Received(1).LogInformation(Arg.Is("User with id {0} deleted in {1}ms"), Arg.Is(id), Arg.Any<long>());
    }
}
