using Xunit;

namespace Mailer.Core.Tests
{
    public class StringExtensionsTests
    {
        [Theory,
            InlineData("valid@email.com", true),
            InlineData("a.b.c@e.f.g.com", true),
            InlineData("e.f.g.com", false),
            InlineData("abc@efg@test.com", false),
            InlineData("", false)]
        public void IsValidEmailMethod_ReturnsTrueForValidEmailAddress(string input, bool expectedResult)
        {
            Assert.Equal(expectedResult, input.IsValidEmail());
        }
    }
}
