using NetArchTest.Rules;
using NUnit.Framework;

namespace NetSdrClientAppTests
{
    [TestFixture]
    public class ArchitectureTests
    {
        private const string NetSdrClientAppNamespace = "NetSdrClientApp";

        [Test]
        public void Messages_ShouldNotDependOn_Networking()
        {
            // Arrange
            var assembly = typeof(NetSdrClientApp.NetSdrClient).Assembly;

            // Act
            var result = Types.InAssembly(assembly)
                .That()
                .ResideInNamespace("NetSdrClientApp.Messages")
                .ShouldNot()
                .HaveDependencyOn("NetSdrClientApp.Networking")
                .GetResult();

            // Assert
            Assert.IsTrue(result.IsSuccessful, 
                "Messages namespace should not depend on Networking namespace. " +
                $"Failing types: {string.Join(", ", result.FailingTypeNames ?? new List<string>())}");
        }

        [Test]
        public void Networking_ShouldNotDependOn_Messages()
        {
            // Arrange
            var assembly = typeof(NetSdrClientApp.NetSdrClient).Assembly;

            // Act
            var result = Types.InAssembly(assembly)
                .That()
                .ResideInNamespace("NetSdrClientApp.Networking")
                .ShouldNot()
                .HaveDependencyOn("NetSdrClientApp.Messages")
                .GetResult();

            // Assert
            Assert.IsTrue(result.IsSuccessful,
                "Networking namespace should not depend on Messages namespace. " +
                $"Failing types: {string.Join(", ", result.FailingTypeNames ?? new List<string>())}");
        }

        [Test]
        public void Interfaces_ShouldStartWithI()
        {
            // Arrange
            var assembly = typeof(NetSdrClientApp.NetSdrClient).Assembly;

            // Act
            var result = Types.InAssembly(assembly)
                .That()
                .AreInterfaces()
                .Should()
                .HaveNameStartingWith("I")
                .GetResult();

            // Assert
            Assert.IsTrue(result.IsSuccessful,
                "All interfaces should start with 'I'. " +
                $"Failing types: {string.Join(", ", result.FailingTypeNames ?? new List<string>())}");
        }

        [Test]
        public void Classes_InNetworking_ShouldNotBePublic()
        {
            // Arrange
            var assembly = typeof(NetSdrClientApp.NetSdrClient).Assembly;

            // Act
            var result = Types.InAssembly(assembly)
                .That()
                .ResideInNamespace("NetSdrClientApp.Networking")
                .And()
                .AreClasses()
                .And()
                .DoNotHaveNameEndingWith("Wrapper") // Wrapper класи можуть бути public
                .ShouldNot()
                .BePublic()
                .GetResult();

            // Assert
            Assert.IsTrue(result.IsSuccessful,
                "Implementation classes in Networking (except Wrappers) should not be public. " +
                $"Failing types: {string.Join(", ", result.FailingTypeNames ?? new List<string>())}");
        }

        [Test]
        public void NetSdrClient_ShouldBePublic()
        {
            // Arrange
            var assembly = typeof(NetSdrClientApp.NetSdrClient).Assembly;

            // Act
            var result = Types.InAssembly(assembly)
                .That()
                .HaveNameMatching("NetSdrClient")
                .And()
                .DoNotResideInNamespace("Coverlet.Core.Instrumentation") // Ignore Coverlet technical classes
                .Should()
                .BePublic()
                .GetResult();

            // Assert
            Assert.IsTrue(result.IsSuccessful,
                "NetSdrClient should be public as it's the main API class. " +
                $"Failing types: {string.Join(", ", result.FailingTypeNames ?? new List<string>())}");
        }
    }
}