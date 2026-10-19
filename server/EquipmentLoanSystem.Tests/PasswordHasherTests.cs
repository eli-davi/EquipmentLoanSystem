using EquipmentLoanSystem.Data;
using EquipmentLoanSystem.Service.Services;

namespace EquipmentLoanSystem.Tests
{
    public class PasswordHasherTests
    {
        private readonly PasswordHasher _hasher = new();

        [Fact]
        public void Hash_ThenVerify_Succeeds_AndWrongPasswordFails()
        {
            var hash = _hasher.Hash("secret123");

            Assert.True(_hasher.Verify("secret123", hash));
            Assert.False(_hasher.Verify("wrong", hash));
        }

        [Fact]
        public void SeededDemoUsers_HavePassword123456()
        {
            foreach (var member in SeedData.Members)
                Assert.True(_hasher.Verify("123456", member.PasswordHash), member.Email);
        }
    }
}
