using System;
using System.IO;
using System.Collections.Generic;
using Xunit;
using ArduinoCore;
using ArduinoCore.Data;
using ArduinoCore.Models;
using Microsoft.Data.Sqlite;

namespace ArduinoTests
{
    /// <summary>
    /// Тесты для проверки бизнес-логики приложения Arduino Learning Hub.
    /// </summary>
    public class ArduinoManagerTests : IDisposable
    {
        private readonly DatabaseService _dbService;
        private readonly ArduinoManager _manager;
        private readonly string _testDbPath;

        public ArduinoManagerTests()
        {
            
            _testDbPath = Path.Combine(Path.GetTempPath(), $"arduino_test_{Guid.NewGuid()}.sqlite");



            InitializeTestDatabase(_testDbPath);

            _dbService = new DatabaseService(_testDbPath);
            _manager = new ArduinoManager(_dbService);
        }

        /// <summary>
        /// Создает пустую структуру БД для тестов без наполнения данными SeedData.
        /// </summary>
        private void InitializeTestDatabase(string path)
        {
            using var conn = new SqliteConnection($"Data Source={path}");
            conn.Open();

            string sql = @"
                CREATE TABLE IF NOT EXISTS Topics (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT, 
                    Title TEXT, Description TEXT, Content TEXT, OrderIndex INTEGER
                );
                CREATE TABLE IF NOT EXISTS Components (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT, 
                    Name TEXT UNIQUE, Category TEXT, Description TEXT, 
                    PinCount INTEGER, VoltageMin REAL, VoltageMax REAL
                );
                CREATE TABLE IF NOT EXISTS ComponentExamples (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT, 
                    ComponentId INTEGER, CodeSnippet TEXT, SchemaDescription TEXT,
                    FOREIGN KEY(ComponentId) REFERENCES Components(Id) ON DELETE CASCADE
                );
            ";

            using var cmd = new SqliteCommand(sql, conn);
            cmd.ExecuteNonQuery();


            InsertTestComponent(conn, "TestResistor", "Passive", "Test Resistor", 2, 0, 5);
            InsertTestComponent(conn, "TestLED", "Actuator", "Test LED", 2, 2, 3);
            InsertTestTopic(conn, "Test Topic 1", "Content 1");
        }

        private void InsertTestComponent(SqliteConnection conn, string name, string cat, string desc, int pins, double vMin, double vMax)
        {
            using var cmd = new SqliteCommand("INSERT INTO Components (Name, Category, Description, PinCount, VoltageMin, VoltageMax) VALUES (@N, @C, @D, @P, @VMi, @VMa)", conn);
            cmd.Parameters.AddWithValue("@N", name);
            cmd.Parameters.AddWithValue("@C", cat);
            cmd.Parameters.AddWithValue("@D", desc);
            cmd.Parameters.AddWithValue("@P", pins);
            cmd.Parameters.AddWithValue("@VMi", vMin);
            cmd.Parameters.AddWithValue("@VMa", vMax);
            cmd.ExecuteNonQuery();
        }

        private void InsertTestTopic(SqliteConnection conn, string title, string content)
        {
            using var cmd = new SqliteCommand("INSERT INTO Topics (Title, Content, OrderIndex) VALUES (@T, @C, 1)", conn);
            cmd.Parameters.AddWithValue("@T", title);
            cmd.Parameters.AddWithValue("@C", content);
            cmd.ExecuteNonQuery();
        }

        #region Тесты поиска компонентов

        [Fact]
        public void FindComponent_ExistingComponent_ReturnsCorrectData()
        {
            // Act
            var component = _manager.FindComponent("TestResistor");

            // Assert
            Assert.NotNull(component);
            Assert.Equal("TestResistor", component.Name);
            Assert.Equal("Passive", component.Category);
            Assert.Equal(2, component.PinCount);
        }

        [Fact]
        public void FindComponent_CaseInsensitive_FindsComponent()
        {
            // Act
            var component = _manager.FindComponent("testled");

            // Assert
            Assert.NotNull(component);
            Assert.Equal("TestLED", component.Name);
        }

        [Fact]
        public void FindComponent_NonExistentComponent_ReturnsNull()
        {
            // Act
            var component = _manager.FindComponent("NonExistentChip");

            // Assert
            Assert.Null(component);
        }

        [Fact]
        public void FindComponent_EmptyString_ThrowsArgumentException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => _manager.FindComponent(""));
            Assert.Contains("пустым", ex.Message);
        }

        [Fact]
        public void FindComponent_WhitespaceString_ThrowsArgumentException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => _manager.FindComponent("   "));
            Assert.Contains("пустым", ex.Message);
        }

        #endregion

        #region Тесты получения тем

        [Fact]
        public void GetAllTopics_ReturnsListOfTopics()
        {
            // Act
            var topics = _manager.GetAllTopics();

            // Assert
            Assert.NotNull(topics);
            Assert.Single(topics);
            Assert.Equal("Test Topic 1", topics[0].Title);
        }

        #endregion

        #region Тесты бизнес-расчетов

        [Fact]
        public void CalculateResistor_ValidInputs_ReturnsCorrectOhms()
        {
            // Arrange:
            double source = 5.0;
            double led = 2.0;
            double current = 20.0;

            // Act
            double result = _manager.CalculateResistor(source, led, current);

            // Assert
            Assert.Equal(150.0, result, precision: 1);
        }

        [Fact]
        public void CalculateResistor_ZeroCurrent_ThrowsException()
        {

            Assert.Throws<ArgumentException>(() => _manager.CalculateResistor(5, 2, 0));
        }

        [Fact]
        public void CalculateResistor_NegativeCurrent_ThrowsException()
        {

            Assert.Throws<ArgumentException>(() => _manager.CalculateResistor(5, 2, -10));
        }

        [Fact]
        public void CalculateResistor_SourceVoltageLessThanLed_ThrowsException()
        {

            Assert.Throws<ArgumentException>(() => _manager.CalculateResistor(2, 3, 10));
        }

        [Fact]
        public void CalculateResistor_EqualVoltages_ThrowsException()
        {

            Assert.Throws<ArgumentException>(() => _manager.CalculateResistor(3, 3, 10));
        }

        #endregion

        public void Dispose()
        {
            _dbService?.Dispose();

            if (File.Exists(_testDbPath))
            {
                try
                {
                    File.Delete(_testDbPath);
                    File.Delete($"{_testDbPath}-wal");
                    File.Delete($"{_testDbPath}-shm");
                }
                catch {}
            }
        }
    }
}