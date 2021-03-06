using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Sentry.Protocol.Tests
{
    public class SdkVersionTests
    {
        [Fact]
        public void AddIntegration_DoesNotExcludeCurrentOne()
        {
            var sut = new SdkVersion();

            sut.AddIntegration("integration 1");
            sut.AddIntegration("integration 2");

            Assert.Equal(2, sut.Integrations.Count());
        }

        [Fact]
        public void SerializeObject_AllPropertiesSetToNonDefault_SerializesValidObject()
        {
            var sut = new SdkVersion
            {
                Name = "Sentry.Test.SDK",
                Version = "0.0.1-preview1",
            };
            sut.AddIntegration("integration 1");

            var actual = JsonSerializer.SerializeObject(sut);

            Assert.Equal("{\"integrations\":[\"integration 1\"],"
                        + "\"name\":\"Sentry.Test.SDK\","
                        + "\"version\":\"0.0.1-preview1\"}",
                    actual);
        }

        [Theory]
        [MemberData(nameof(TestCases))]
        public void SerializeObject_TestCase_SerializesAsExpected((SdkVersion sdkVersion, string serialized) @case)
        {
            var actual = JsonSerializer.SerializeObject(@case.sdkVersion);

            Assert.Equal(@case.serialized, actual);
        }

        public static IEnumerable<object[]> TestCases()
        {
            yield return new object[] { (new SdkVersion(), "{}") };
            yield return new object[] { (new SdkVersion { Name = "some name" }, "{\"name\":\"some name\"}") };
            yield return new object[] { (new SdkVersion { Version = "some version" }, "{\"version\":\"some version\"}") };
            yield return new object[] { (new SdkVersion { InternalIntegrations =
                new ConcurrentBag<string> { "integration 1", "integration 2" } }, "{\"integrations\":[\"integration 2\",\"integration 1\"]}") };
        }
    }
}
