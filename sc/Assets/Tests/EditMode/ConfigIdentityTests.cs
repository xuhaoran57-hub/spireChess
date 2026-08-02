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
            Assert.That(configs.Identity.ContentVersion, Is.EqualTo("5.6.0"));
            Assert.That(configs.Identity.RulesVersion, Is.EqualTo("8B.1"));
            Assert.That(
                configs.Identity.ConfigHash,
                Is.EqualTo("a45ece87784a34f9fff49d28f2655c879217086669a5371d9a28f3f3f08f1b51"));
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
