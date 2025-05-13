using Monzowler.Shared.Utilities;

namespace Monzowler.Unittest.Utilities
{
    public class SanitizerTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("#anchor")]
        public void SanitizeUrl_ShouldReturnNull_ForEmptyOrAnchorHref(string href)
        {
            // Act
            var result = Sanitizer.SanitizeUrl(href, "https://example.com");

            // Assert
            Assert.Null(result);
        }

        [Theory]
        [InlineData("mailto:test@example.com")]
        [InlineData("ftp://example.com/file.txt")]
        [InlineData("javascript:alert('x')")]
        public void SanitizeUrl_ShouldReturnNull_ForNonHttpSchemes(string href)
        {
            // Act
            var result = Sanitizer.SanitizeUrl(href, "https://example.com");

            // Assert
            Assert.Null(result);
        }

        [Theory]
        [InlineData("/file.pdf")]
        [InlineData("/file.docx")]
        [InlineData("/file.zip")]
        [InlineData("/image.jpg")]
        [InlineData("/video.mp4")]
        public void SanitizeUrl_ShouldReturnNull_ForExcludedExtensions(string href)
        {
            // Act
            var result = Sanitizer.SanitizeUrl(href, "https://example.com");

            // Assert
            Assert.Null(result);
        }

        [Theory]
        [InlineData("/valid-path", "https://example.com/valid-path")]
        [InlineData("valid-path/", "https://example.com/valid-path")]
        [InlineData("https://example.com/page", "https://example.com/page")]
        public void SanitizeUrl_ShouldReturnSanitizedUrl_WhenValid(string href, string expected)
        {
            // Act
            var result = Sanitizer.SanitizeUrl(href, "https://example.com");

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void SanitizeUrl_ShouldReturnNull_WhenUriCreationFails()
        {
            // Invalid base URL
            var result = Sanitizer.SanitizeUrl("/valid-path", "not-a-valid-url");

            Assert.Null(result);
        }
    }
}
