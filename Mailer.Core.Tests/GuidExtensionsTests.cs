using System;
using Xunit;

namespace Mailer.Core.Tests
{
    public class GuidExtensionsTests
    {
        [Theory,
            InlineData("0f8fad5b-d9cb-469f-a165-70867728950e", false),
            InlineData("7c9e6679-7425-40de-944b-e07fc1f90ae7", false),
            InlineData("00000000-0000-0000-0000-000000000000", true)]
        public void IsEmptyMethod_ReturnsTrueForEmptyGuid(string id, bool expectedResult)
        {
            Assert.Equal(expectedResult, new Guid(id).IsEmpty());
        }
    }
}
