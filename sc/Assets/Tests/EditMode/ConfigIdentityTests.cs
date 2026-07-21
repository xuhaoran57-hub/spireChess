using NUnit.Framework;
using SpireChess.Config;
using SpireChess.Utils;

namespace SpireChess.Tests.EditMode
{
    public sealed class ConfigIdentityTests
    {
        [Test]
        public void LoadFromResources_ExposesFrozenFullConfigIdentity()
        {
            var configs = new ConfigService(new NewtonsoftJsonSerializer());
            var validation = configs.LoadFromResources();

            Assert.That(validation.IsValid, Is.True, string.Join("\n", validation.Errors));
            Assert.That(configs.Identity, Is.Not.Null);
            Assert.That(configs.Identity.ContentVersion, Is.EqualTo("5.5.0"));
            Assert.That(configs.Identity.RulesVersion, Is.EqualTo("8B.1"));
            Assert.That(
                configs.Identity.ConfigHash,
                Is.EqualTo("818596be90de4e2ddf6c4b7f9ba0b6e1fee994fcc31ec9893652e02f49ef4311"));
        }

        [Test]
        public void CanonicalJsonHash_IgnoresObjectPropertyOrder()
        {
            Assert.That(
                CanonicalJson.ComputeSha256("{\"b\":2,\"a\":1}"),
                Is.EqualTo(CanonicalJson.ComputeSha256("{\"a\":1,\"b\":2}")));
        }
    }
}
